//
// ChoreographerTests.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using NUnit.Framework;

using Hyena;
using Hyena.Gui.Theatrics;

namespace Hyena.Gui.Theatrics.Tests
{
    [TestFixture]
    public class ChoreographerTests
    {
        private void _TestComposeRange (int [] values, Easing easing)
        {
            for (double i = 0, n = 100, p = 0, j = 0; i <= n; i += 5, p = i / n, j++) {
                int value = Choreographer.PixelCompose (p, (int)n, easing);
                Assert.AreEqual (values[(int)j], value);
            }
        }

        [Test]
        public void QuadraticInCompose ()
        {
            _TestComposeRange (new int [] {
                0, 0, 1, 2, 4, 6, 9, 12, 16, 20, 25, 30, 36, 42, 49, 56, 64, 72, 81, 90, 100
            }, Easing.QuadraticIn);
        }

        [Test]
        public void QuadraticOutCompose ()
        {
            _TestComposeRange (new int [] {
                0, 10, 19, 28, 36, 44, 51, 58, 64, 70, 75, 80, 84, 88, 91, 94, 96, 98, 99, 100, 100
            }, Easing.QuadraticOut);
        }

        [Test]
        public void QuadraticInOutCompose ()
        {
            _TestComposeRange (new int [] {
                0, 1, 2, 4, 8, 12, 18, 24, 32, 40, 50, 60, 68, 76, 82, 88, 92, 96, 98, 100, 100
            }, Easing.QuadraticInOut);
        }

        [Test]
        public void ExponentialInCompose ()
        {
            _TestComposeRange (new int [] {
                0, 0, 0, 0, 0, 1, 1, 1, 2, 2, 3, 4, 6, 9, 12, 18, 25, 35, 50, 71, 100
            }, Easing.ExponentialIn);
        }

        [Test]
        public void ExponentialOutCompose ()
        {
            _TestComposeRange (new int [] {
                0, 29, 50, 65, 75, 82, 88, 91, 94, 96, 97, 98, 98, 99, 99, 99, 100, 100, 100, 100, 100
            }, Easing.ExponentialOut);
        }

        [Test]
        public void ExponentialInOutCompose ()
        {
            _TestComposeRange (new int [] {
                0, 0, 0, 0, 1, 2, 3, 6, 13, 25, 50, 75, 88, 94, 97, 98, 99, 100, 100, 100, 100
            }, Easing.ExponentialInOut);
        }

        [Test]
        public void LinearCompose ()
        {
            _TestComposeRange (new int [] {
                0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100
            }, Easing.Linear);
        }

        [Test]
        public void SineCompose ()
        {
            _TestComposeRange (new int [] {
                0, 16, 31, 45, 59, 71, 81, 89, 95, 99, 100, 99, 95, 89, 81, 71, 59, 45, 31, 16, 0
            }, Easing.Sine);
        }
    }
}

#endif
