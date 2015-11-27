//
// SqliteUtilTests.cs
//
// Author:
//   John Millikin <jmillikin@gmail.com>
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

using NUnit.Framework;
using Hyena.Data.Sqlite;

namespace Hyena.Data.Sqlite.Tests
{
    [TestFixture]
    public class CollationKeyTests
    {
        protected void CollationKeyTest (object before, object after)
        {
            Assert.AreEqual (after, (new CollationKeyFunction ()).Invoke (new object[] {before}));
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
            CollationKeyTest ("", new byte[] {1, 1, 1, 1, 0});
            CollationKeyTest ("\u0104", new byte[] {14, 2, 1, 27, 1, 1, 1, 0});
        }
    }

    [TestFixture]
    public class SearchKeyTests
    {
        protected void SearchKeyTest (object before, object after)
        {
            Assert.AreEqual (after, (new SearchKeyFunction ()).Invoke (new object[] {before}));
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

#endif

