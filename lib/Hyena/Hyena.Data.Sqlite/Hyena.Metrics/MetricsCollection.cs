//
// MetricsCollection.cs
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
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using Hyena;
using Hyena.Json;

namespace Hyena.Metrics
{
    public sealed class MetricsCollection : List<Metric>, IDisposable
    {
        public static readonly int FormatVersion = 1;

        public string AnonymousUserId { get; private set; }
        public ISampleStore Store { get; private set; }

        public MetricsCollection (string uniqueUserId, ISampleStore store)
        {
            AnonymousUserId = uniqueUserId;
            Store = store;
        }

        public Metric Add (string name)
        {
            return Add (new Metric (name, Store));
        }

        public Metric Add (string name, object value)
        {
            return Add (new Metric (name, Store, value));
        }

        public Metric Add (string name, Func<object> sampleFunc)
        {
            return Add (new Metric (name, Store, sampleFunc));
        }

        public new Metric Add (Metric metric)
        {
            base.Add (metric);
            return metric;
        }

        public void Dispose ()
        {
            foreach (var m in this) {
                m.Dispose ();
            }
            Clear ();
        }

        public string ToJsonString ()
        {
            var report = new Dictionary<string, object> ();

            report["ID"] = AnonymousUserId;
            report["Now"] = DateTimeUtil.ToInvariantString (DateTime.Now);
            report["FormatVersion"] = FormatVersion;

            var metrics = new Dictionary<string, object> ();
            foreach (var metric in this.OrderBy (m => m.Name)) {
                metrics[metric.Name] = Store.GetFor (metric).Select (s => new object [] { DateTimeUtil.ToInvariantString (s.Stamp), s.Value ?? "" });
            }
            report["Metrics"] = metrics;

            return new Serializer (report).Serialize ();
        }

        public void AddDefaults ()
        {
            Add ("Env/OS Platform",          PlatformDetection.SystemName);
            Add ("Env/OS Version",           System.Environment.OSVersion);
            Add ("Env/Processor Count",      System.Environment.ProcessorCount);
            Add ("Env/.NET Runtime Version", System.Environment.Version);
            Add ("Env/Debugging",            ApplicationContext.Debugging);
            Add ("Env/CultureInfo",          System.Globalization.CultureInfo.CurrentCulture.Name);

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies ()) {
                var name = asm.GetName ();
                Add (String.Format ("Assemblies/{0}", name.Name), name.Version);
            }
        }
    }
}