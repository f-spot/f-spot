// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Utils.Tests
{
	[TestClass]
	public class HashUtilsTests
	{
		const string TestingMd5 = "ae2b1fca515949e5d54fb22b8ed95575";

		[TestMethod]
		[DataRow ("testing", TestingMd5)]
		[DataRow (null, "")]
		[DataRow ("", "")]
		public void Md5Encode_MatchesTheKnownAsciiDigest (string text, string expected)
		{
			Assert.AreEqual (expected, HashUtils.Md5Encode (text));
		}

		[TestMethod]
		public void Md5EncodeStream_HashesTheRemainingBytes ()
		{
			using (var stream = new MemoryStream (Encoding.ASCII.GetBytes ("testing")))
				Assert.AreEqual (TestingMd5, HashUtils.Md5EncodeStream (stream));
		}
	}
}
