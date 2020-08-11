// GtkSynchronizationContext.cs
//
// Author:
//      Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (c) 2018 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

// Pulled from here: https://github.com/mono/monodevelop/blob/master/main/src/core/MonoDevelop.Ide/MonoDevelop.Ide/DispatchService.cs#L48

using System;
using System.Runtime.InteropServices;
using System.Threading;

using Hyena;

namespace FSpot
{
	class GtkSynchronizationContext : SynchronizationContext
	{
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate bool GSourceFuncInternal(IntPtr ptr);

		internal class TimeoutProxy
		{
			internal SendOrPostCallback d;
			object state;
			ManualResetEventSlim resetEvent;

			public TimeoutProxy(SendOrPostCallback d, object state) : this(d, state, null)
			{
			}

			public TimeoutProxy(SendOrPostCallback d, object state, ManualResetEventSlim lockObject)
			{
				this.d = d;
				this.state = state;
				this.resetEvent = lockObject;
			}

			internal static readonly GSourceFuncInternal SourceHandler = HandlerInternal;

			static bool HandlerInternal(IntPtr data)
			{
				var proxy = (TimeoutProxy)((GCHandle)data).Target;

				try {
					proxy.d(proxy.state);
				} catch (Exception e) {
					GLib.ExceptionManager.RaiseUnhandledException(e, false);
				} finally {
					proxy.resetEvent?.Set();
				}
				return false;
			}
		}

		const int defaultPriority = 0;

		[DllImport("libglib-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint g_timeout_add_full(int priority, uint interval, GSourceFuncInternal d, IntPtr data, GLib.DestroyNotify notify);

		class ExceptionWithStackTraceWithoutThrowing : Exception
		{
			public ExceptionWithStackTraceWithoutThrowing (string message) : base (message)
			{
				StackTrace = Environment.StackTrace;
			}

			public override string StackTrace { get; }
		}

		static void AddTimeout(TimeoutProxy proxy)
		{
			if (proxy.d == null) {
				// Create an exception without throwing it, as throwing is expensive and these exceptions can be
				// hit a lot of times.

				const string exceptionMessage = "Unexpected null delegate sent to synchronization context";
				Hyena.Log.Exception (exceptionMessage,
					new ExceptionWithStackTraceWithoutThrowing (exceptionMessage));

				// Return here without queueing the UI operation. Async calls which await on the given callback
				// will continue immediately, but at least we won't crash.
				// Having a null continuation won't do anything anyway.
				return;
			}

			var gch = GCHandle.Alloc (proxy);

			g_timeout_add_full (defaultPriority, 0, TimeoutProxy.SourceHandler, (IntPtr)gch, GLib.DestroyHelper.NotifyHandler);
		}

		public override void Post(SendOrPostCallback d, object state)
		{
			var proxy = new TimeoutProxy(d, state);
			AddTimeout(proxy);
		}

		public override void Send(SendOrPostCallback d, object state)
		{
			if (ThreadAssist.InMainThread)
			{
				d(state);
				return;
			}

			using var ob = new ManualResetEventSlim(false);
			var proxy = new TimeoutProxy(d, state, ob);

			AddTimeout(proxy);
			ob.Wait();
		}

		public override SynchronizationContext CreateCopy()
		{
			return new GtkSynchronizationContext();
		}
	}
}
