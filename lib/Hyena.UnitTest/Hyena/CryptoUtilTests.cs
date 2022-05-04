//
// CryptoUtilTests.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using NUnit.Framework;

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
