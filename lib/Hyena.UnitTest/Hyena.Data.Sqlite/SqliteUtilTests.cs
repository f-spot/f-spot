//
// SqliteUtilTests.cs
//
// Author:
//   John Millikin <jmillikin@gmail.com>
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NUnit.Framework;

namespace Hyena.Data.Sqlite.Tests
{
	[TestFixture]
	public class CollationKeyTests
	{
		protected void CollationKeyTest (object before, object after)
		{
			Assert.AreEqual (after, (new CollationKeyFunction ()).Invoke (new object[] { before }));
		}

		[Test]
		public void TestNull ()
		{
			CollationKeyTest (null, null);
			CollationKeyTest (System.DBNull.Value, null);
		}

		[Test]
		public void TestKey ()
		{
			// See Hyena.StringUtil.Tests for full tests. This just checks that
			// the collation function is actually being used.
			CollationKeyTest ("", new byte[] { 1, 1, 1, 1, 0 });
			CollationKeyTest ("\u0104", new byte[] { 14, 2, 1, 27, 1, 1, 1, 0 });
		}
	}

	[TestFixture]
	public class SearchKeyTests
	{
		protected void SearchKeyTest (object before, object after)
		{
			Assert.AreEqual (after, new SearchKeyFunction ().Invoke (new object[] { before }));
		}

		[Test]
		public void TestNull ()
		{
			SearchKeyTest (null, null);
			SearchKeyTest (System.DBNull.Value, null);
		}

		[Test]
		public void TestKey ()
		{
			// See Hyena.StringUtil.Tests for full tests. This just checks that
			// the search key function is actually being used.
			SearchKeyTest ("", "");
			SearchKeyTest ("\u0104", "a");
		}
	}
}
