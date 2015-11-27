//
// Sample.cs
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
using Hyena.Data.Sqlite;

namespace Hyena.Metrics
{
    public class Sample
    {
        [DatabaseColumn (Constraints = DatabaseColumnConstraints.PrimaryKey)]
        protected long Id { get; set; }

        [DatabaseColumn]
        public string MetricName { get; protected set; }

        [DatabaseColumn]
        public DateTime Stamp { get; protected set; }

        [DatabaseColumn]
        public string Value { get; protected set; }

        // For SqliteModelProvider's use
        public Sample () {}

        public Sample (Metric metric, object value)
        {
            MetricName = metric.Name;
            Stamp = DateTime.Now;
            SetValue (value);
        }

        protected void SetValue (object value)
        {
            if (value == null) {
                Value = "";
            } else if (value is DateTime) {
                Value = Hyena.DateTimeUtil.ToInvariantString ((DateTime) value);
            } else {
                Value = value.ToString ();
            }
        }
    }
}