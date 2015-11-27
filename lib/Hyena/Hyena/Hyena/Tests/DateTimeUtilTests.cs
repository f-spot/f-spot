//
// DateTimeUtilTests.cs
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

#if ENABLE_TESTS

using System;
using NUnit.Framework;
using Hyena;

namespace Hyena.Tests
{
    [TestFixture]
    public class DateTimeUtilTests
    {
        [Test]
        public void InvariantString ()
        {
            // Tests are run in Chicago timezone, UTC -6 in the winter, -5 in the summer
            TestInv ("2010-02-18 02:41:00.000 -06:00", new DateTime (2010, 2, 18, 2, 41, 0, 0));
            TestInv ("2010-02-18 02:41:50.123 -06:00", new DateTime (2010, 2, 18, 2, 41, 50, 123));
            TestInv ("2010-10-18 02:01:00.000 -05:00", new DateTime (2010, 10, 18, 2, 1, 0, 0));
        }

        private void TestInv (string inv_string, DateTime dt)
        {
            // Make sure we can generate the given string from the DateTime
            Assert.AreEqual (inv_string, DateTimeUtil.ToInvariantString (dt));

            // And vice versa
            DateTime parsed_dt;
            if (DateTimeUtil.TryParseInvariant (inv_string, out parsed_dt))
                Assert.AreEqual (dt, parsed_dt);
            else
                Assert.Fail (String.Format ("TryParseInvariant failed on {0}", inv_string));
        }

        [Test]
        public void FromToSymmetry ()
        {
            // ToTimeT only has precision to the second; so strip off the remainding ticks
            DateTime now = DateTime.Now;
            now = now.Subtract (TimeSpan.FromTicks (now.Ticks % TimeSpan.TicksPerSecond));

            long time_t = DateTimeUtil.ToTimeT (now);
            DateTime now_t = DateTimeUtil.FromTimeT (time_t);

            Assert.AreEqual (DateTimeKind.Local, now.Kind);
            Assert.AreEqual (DateTimeKind.Local, now_t.Kind);
            Assert.AreEqual (now, now_t);
        }
    }
}

#endif
