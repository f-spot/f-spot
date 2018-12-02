// GtkSynchronizationContext.cs
//
// Author:
//      Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (c) 2018 Stephen Shaw
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

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

				try
				{
					proxy.d(proxy.state);
				}
				catch (Exception e)
				{
					GLib.ExceptionManager.RaiseUnhandledException(e, false);
				}
				finally
				{
					proxy.resetEvent?.Set();
				}
				return false;
			}
		}

		const int defaultPriority = 0;

		[DllImport("libglib-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint g_timeout_add_full(int priority, uint interval, GSourceFuncInternal d, IntPtr data, GLib.DestroyNotify notify);

		static void AddTimeout(TimeoutProxy proxy)
		{
			var gch = GCHandle.Alloc(proxy);

			g_timeout_add_full(defaultPriority, 0, TimeoutProxy.SourceHandler, (IntPtr)gch, GLib.DestroyHelper.NotifyHandler);
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

			using (var ob = new ManualResetEventSlim(false))
			{
				var proxy = new TimeoutProxy(d, state, ob);

				AddTimeout(proxy);
				ob.Wait();
			}
		}

		public override SynchronizationContext CreateCopy()
		{
			return new GtkSynchronizationContext();
		}
	}
}
