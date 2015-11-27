//
// Metric.cs
//
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//
// Copyright (c) 2010 Novell, Inc.
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

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Hyena.Metrics
{
    public sealed class Metric : IDisposable
    {
        public string Name { get; private set; }
        public bool CanTakeSample { get { return sample_func != null; } }

        private ISampleStore store;
        private Func<object> sample_func;

        internal Metric (string name, ISampleStore store)
        {
            Name = name;
            this.store = store;
        }

        internal Metric (string name, ISampleStore store, Func<object> sampleFunc) : this (name, store)
        {
            sample_func = sampleFunc;
        }

        internal Metric (string name, ISampleStore store, object value) : this (name, store)
        {
            PushSample (value);
        }

        public void Dispose ()
        {
        }

        public void PushSample (object sampleValue)
        {
            try {
                store.Add (new Sample (this, sampleValue));
            } catch (Exception e) {
                Log.Exception ("Error taking sample", e);
            }
        }

        public void TakeSample ()
        {
            if (sample_func == null) {
                Log.Warning ("sampleFunc is null.  Are you calling TakeSample on a non-event-driven metric?");
                return;
            }

            try {
                store.Add (new Sample (this, sample_func ()));
            } catch (Exception e) {
                Log.Exception ("Error taking sample", e);
            }
        }
    }
}
