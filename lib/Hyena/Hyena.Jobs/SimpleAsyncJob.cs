//
// SimpleAsyncJob.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Hyena.Jobs
{
    public abstract class SimpleAsyncJob : Job
    {
        private Thread thread;

        public SimpleAsyncJob ()
        {
        }

        public SimpleAsyncJob (string name, PriorityHints hints, params Resource [] resources)
            : base (name, hints, resources)
        {
        }

        protected override void RunJob ()
        {
            if (thread == null) {
                thread = new Thread (InnerStart);
                thread.Name = String.Format ("Hyena.Jobs.JobRunner ({0})", Title);
                thread.Priority = this.Has (PriorityHints.SpeedSensitive) ? ThreadPriority.Normal : ThreadPriority.Lowest;
                thread.Start ();
            }
        }

        protected void AbortThread ()
        {
            if (thread != null) {
                thread.Abort ();
            }
        }

        private void InnerStart ()
        {
            try {
                Run ();
            } catch (ThreadAbortException) {
            } catch (Exception e) {
                Log.Exception (e);
            }
        }

        protected abstract void Run ();
    }
}
