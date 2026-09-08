//
// GLibSynchronizationContext.cs
//
// Author:
//      Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (c) 2018 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

// Adapted from MonoDevelop's DispatchService:
// https://github.com/mono/monodevelop/blob/master/main/src/core/MonoDevelop.Ide/MonoDevelop.Ide/DispatchService.cs#L48

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace FSpot
{
	// Marshals continuations onto the GLib main loop, which is what makes await resume on
	// the UI thread. Nothing here touches Gtk: it is a GLib context, and it would work in
	// any process running a GLib main loop.
	class GLibSynchronizationContext : SynchronizationContext
	{
		// Called directly rather than through GLib.Idle.Add, for three reasons that all
		// matter on a path taken by every await resumption. GLib.Idle.Add locks a shared
		// static dictionary per callback; it needs the glibsharpglue-2 native library,
		// which ships as a .so even on macOS; and it queues at G_PRIORITY_DEFAULT_IDLE
		// (200), behind GTK's own redraws (120), whereas G_PRIORITY_DEFAULT is 0.
		const int GPriorityDefault = 0;

		[DllImport ("libglib-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint g_timeout_add_full (int priority, uint interval, GSourceFuncInternal d, IntPtr data, GLib.DestroyNotify notify);

		[UnmanagedFunctionPointer (CallingConvention.Cdecl)]
		internal delegate bool GSourceFuncInternal (IntPtr ptr);

		// The thread the context is created on, which Main.cs does before starting the
		// main loop. Held here rather than read from ThreadAssist so the context does not
		// depend on the mechanism it replaces.
		readonly Thread mainThread = Thread.CurrentThread;

		sealed class Callback
		{
			readonly SendOrPostCallback callback;
			readonly object state;
			readonly ManualResetEventSlim completed;

			internal Callback (SendOrPostCallback callback, object state, ManualResetEventSlim completed)
			{
				this.callback = callback;
				this.state = state;
				this.completed = completed;
			}

			internal static readonly GSourceFuncInternal SourceHandler = Invoke;

			static bool Invoke (IntPtr data)
			{
				var self = (Callback)((GCHandle)data).Target;

				try {
					self.callback (self.state);
				} catch (Exception e) {
					GLib.ExceptionManager.RaiseUnhandledException (e, false);
				} finally {
					self.completed?.Set ();
				}

				return false;   // one shot; GLib removes the source
			}
		}

		public override void Post (SendOrPostCallback d, object state)
		{
			if (d == null) {
				ReportNullCallback ();
				return;
			}

			Enqueue (d, state, null);
		}

		public override void Send (SendOrPostCallback d, object state)
		{
			// Checked before the main-thread shortcut rather than inside Enqueue, which
			// the shortcut skips: a null callback threw here and was tolerated from
			// every other thread.
			if (d == null) {
				ReportNullCallback ();
				return;
			}

			if (Thread.CurrentThread == mainThread) {
				d (state);
				return;
			}

			using var completed = new ManualResetEventSlim (false);
			Enqueue (d, state, completed);
			completed.Wait ();
		}

		// The thread above is the whole of this context's state and it never changes, so
		// a copy can be this instance -- and should be. Mono's
		// SynchronizationContextAwaitTaskContinuation decides whether to run a
		// continuation inline with a reference comparison, m_syncContext ==
		// SynchronizationContext.Current, so handing out a distinct copy sends every
		// main-thread await resumption through the GLib loop for nothing. Re-deriving the
		// thread in a copy would be worse still: ExecutionContext.Capture () copies the
		// context on whatever thread it runs on, so a copy taken on a worker would take
		// that worker for the main thread and run Send inline, off the loop.
		public override SynchronizationContext CreateCopy () => this;

		// A null continuation has nothing to run, and an awaiter waiting on it continues
		// either way, so log and carry on rather than taking the process down.
		static void ReportNullCallback ()
		{
			Logger.Log.Error ("Unexpected null delegate sent to the synchronization context, at {StackTrace}",
				Environment.StackTrace);
		}

		void Enqueue (SendOrPostCallback d, object state, ManualResetEventSlim completed)
		{
			var handle = GCHandle.Alloc (new Callback (d, state, completed));
			try {
				g_timeout_add_full (GPriorityDefault, 0, Callback.SourceHandler, (IntPtr)handle, GLib.DestroyHelper.NotifyHandler);
			} catch {
				// No source was created, so no destroy notify will ever fire to free this.
				handle.Free ();
				throw;
			}
		}
	}
}
