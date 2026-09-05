//
// SqliteUtilTests.cs
//
// Author:
//   John Millikin <jmillikin@gmail.com>
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyena.Data.Sqlite.Tests
{
	[TestClass]
	public class CollationKeyTests
	{
		protected void CollationKeyTest (object before, object after)
		{
			var actual = (new CollationKeyFunction ()).Invoke (new object[] { before });
			if (after is byte[] expectedBytes && actual is byte[] actualBytes)
				Assert.AreSequenceEqual (expectedBytes, actualBytes);
			else
				Assert.AreEqual (after, actual);
		}

		[TestMethod]
		public void TestNull ()
		{
			CollationKeyTest (null, null);
			CollationKeyTest (System.DBNull.Value, null);
		}

		[TestMethod]
		public void TestKey ()
		{
			// See Hyena.StringUtil.Tests for full tests. This just checks that
			// the collation function is actually being used.
			CollationKeyTest ("", Hyena.StringUtil.SortKey (""));
			CollationKeyTest ("\u0104", Hyena.StringUtil.SortKey ("\u0104"));
		}
	}

	[TestClass]
	public class SearchKeyTests
	{
		protected void SearchKeyTest (object before, object after)
		{
			Assert.AreEqual (after, new SearchKeyFunction ().Invoke (new object[] { before }));
		}

		[TestMethod]
		public void TestNull ()
		{
			SearchKeyTest (null, null);
			SearchKeyTest (System.DBNull.Value, null);
		}

		[TestMethod]
		public void TestKey ()
		{
			// See Hyena.StringUtil.Tests for full tests. This just checks that
			// the search key function is actually being used.
			SearchKeyTest ("", "");
			SearchKeyTest ("\u0104", "a");
		}
	}
}
