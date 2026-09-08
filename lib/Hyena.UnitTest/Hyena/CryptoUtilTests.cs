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

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyena.Tests
{
	[TestClass]
	public class CryptoUtilTests
	{
		[TestMethod]
		public void Md5Encode ()
		{
			Assert.AreEqual ("ae2b1fca515949e5d54fb22b8ed95575", CryptoUtil.Md5Encode ("testing"));
			Assert.AreEqual ("", CryptoUtil.Md5Encode (null));
			Assert.AreEqual ("", CryptoUtil.Md5Encode (""));
		}

		[TestMethod]
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
	}
}
