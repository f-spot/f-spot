//
// ThreadAssist.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2005-2009 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

namespace Hyena
{
	public static class ThreadAssist
	{
		public static Thread MainThread { get; private set; }

		public static Action<InvokeHandler> ProxyToMainHandler { get; set; }

		public static void InitializeMainThread ()
		{
			MainThread = Thread.CurrentThread;
			try {
				MainThread.Name = "Main Thread";
			} catch {
				Log.Debug ($"Main thread set to {MainThread.Name}");
			}
		}

		public static bool InMainThread {
			get {
				if (MainThread == null) {
					throw new ApplicationException ("ThreadAssist.InitializeMainThread must be called first");
				}

				return MainThread.Equals (Thread.CurrentThread);
			}
		}

		public static void AssertNotInMainThread ()
		{
			if (ApplicationContext.Debugging && InMainThread) {
				Log.Warning ("In GUI thread, will probably block it", Environment.StackTrace);
			}
		}

		public static void AssertInMainThread ()
		{
			if (ApplicationContext.Debugging && !InMainThread) {
				Log.Warning ("Not in main thread!", Environment.StackTrace);
			}
		}

		public static void BlockingProxyToMain (InvokeHandler handler)
		{
			if (!InMainThread) {
				var reset_event = new ManualResetEvent (false);

				ProxyToMainHandler (delegate {
					try {
						handler ();
					} finally {
						reset_event.Set ();
					}
				});

				reset_event.WaitOne ();
			} else {
				handler ();
			}
		}

		public static void ProxyToMain (InvokeHandler handler)
		{
			if (!InMainThread) {
				ProxyToMainHandler (handler);
			} else {
				handler ();
			}
		}

		public static void SpawnFromMain (ThreadStart threadedMethod)
		{
			if (InMainThread) {
				Spawn (threadedMethod, true);
			} else {
				threadedMethod ();
			}
		}

		public static Thread Spawn (ThreadStart threadedMethod, bool autoStart)
		{
			var thread = new Thread (threadedMethod);
			thread.Name = $"Spawned: {threadedMethod}";
			thread.IsBackground = true;
			if (autoStart) {
				thread.Start ();
			}
			return thread;
		}

		public static Thread Spawn (ThreadStart threadedMethod)
		{
			return Spawn (threadedMethod, true);
		}
	}
}
