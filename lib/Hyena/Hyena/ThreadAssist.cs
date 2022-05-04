//
// ThreadAssist.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2005-2009 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

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
