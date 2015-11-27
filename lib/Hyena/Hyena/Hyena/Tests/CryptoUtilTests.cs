//
// CryptoUtilTests.cs
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
using System.IO;
using NUnit.Framework;
using Hyena;

namespace Hyena.Tests
{
    [TestFixture]
    public class CryptoUtilTests
    {
        [Test]
        public void Md5Encode ()
        {
            Assert.AreEqual ("ae2b1fca515949e5d54fb22b8ed95575", CryptoUtil.Md5Encode ("testing"));
            Assert.AreEqual ("", CryptoUtil.Md5Encode (null));
            Assert.AreEqual ("", CryptoUtil.Md5Encode (""));
        }

        [Test]
        public void IsMd5Encoded ()
        {
            Assert.IsTrue (CryptoUtil.IsMd5Encoded ("ae2b1fca515949e5d54fb22b8ed95575"));
            Assert.IsFalse (CryptoUtil.IsMd5Encoded ("abc233"));
            Assert.IsFalse (CryptoUtil.IsMd5Encoded ("lebowski"));
            Assert.IsFalse (CryptoUtil.IsMd5Encoded ("ae2b1fca515949e5g54fb22b8ed95575"));
            Assert.IsFalse (CryptoUtil.IsMd5Encoded (null));
            Assert.IsFalse (CryptoUtil.IsMd5Encoded (""));
        }

        [Test]
        public void Md5EncodeStream ()
        {
            var file = Path.GetTempFileName ();
            var tw = new StreamWriter (file);
            tw.Write ("testing");
            tw.Close ();

            var stream = new FileStream (file, FileMode.Open);
            Assert.AreEqual ("ae2b1fca515949e5d54fb22b8ed95575", CryptoUtil.Md5EncodeStream (stream));
            stream.Close ();

            File.Delete (file);
        }

        /*[Test]
        public void Md5Performance ()
        {
            int max = 10000;
            using (new Timer (String.Format ("Computed {0} MD5 hashes", max))) {
                for (int i = 0; i < max; i++) {
                    CryptoUtil.Md5Encode ("LkaJSd Flkjdf234234lkj3WlkejewrVlkdf @343434 dsfjk 3497u34 l 2008 lkjdf");
                }
            }
        }*/
    }
}

#endif
