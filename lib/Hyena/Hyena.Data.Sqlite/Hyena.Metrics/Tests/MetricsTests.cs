//
// MetricsTests.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2010 Novell, Inc.
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

#if ENABLE_TESTS

using System;
using System.Linq;
using System.IO;
using NUnit.Framework;

using Hyena;
using Hyena.Json;
using Hyena.Metrics;

namespace Hyena.Tests
{
    [TestFixture]
    public class MetricsTests
    {
        [Test]
        public void MetricsCollection ()
        {
            string id = "myuniqueid";
            var metrics = new MetricsCollection (id, new MemorySampleStore ());
            Assert.AreEqual ("myuniqueid", metrics.AnonymousUserId);

            metrics.AddDefaults ();
            Assert.IsTrue (metrics.Count > 0);

            string metrics_str = metrics.ToJsonString ();
            Assert.IsTrue (metrics_str.Contains ("\"ID\":\"myuniqueid\""));

            // tests/Makefile.am runs the tests with Locale=it_IT
            Assert.IsTrue (metrics_str.Contains ("it-IT"));

            // Make sure DateTime samples are saved as invariant strings
            var now = DateTime.Now;
            var time_metric = metrics.Add ("Foo", now);
            Assert.AreEqual (Hyena.DateTimeUtil.ToInvariantString (now), metrics.Store.GetFor (time_metric).First ().Value);

            // Make sure we can read the JSON back in
            var ds = new Json.Deserializer ();
            ds.SetInput (metrics.ToJsonString ());
            var json_obj = ds.Deserialize () as JsonObject;
            Assert.AreEqual (id, json_obj["ID"]);
            Assert.IsTrue (json_obj["Metrics"] is JsonObject);
        }
    }
}

#endif
