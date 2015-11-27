//
// SqliteTests.cs
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

using NUnit.Framework;
using Hyena.Data.Sqlite;

namespace Hyena.Data.Sqlite
{
    [TestFixture]
    public class SqliteTests
    {
        Connection con;
        Statement select_literal;
        string dbfile = "hyena-data-sqlite-test.db";

        [SetUp]
        public void Setup ()
        {
            con = new Connection (dbfile);
            select_literal = con.CreateStatement ("SELECT ?");
        }

        [TearDown]
        public void TearDown ()
        {
            select_literal.Dispose ();
            Assert.AreEqual (0, con.Statements.Count);
            con.Dispose ();
            System.IO.File.Delete (dbfile);
        }

        [Test]
        public void TestBindWhileReading ()
        {
            using (var stmt = con.CreateStatement ("SELECT ? as version")) {
                stmt.Bind (7);
                var reader = stmt.Query ();
                Assert.IsTrue (reader.Read ());
                Assert.AreEqual (7, reader[0]);

                try {
                    stmt.Bind (6);
                    Assert.Fail ("Shouldn't be able to bind while a reader is active");
                } catch {}

                Assert.AreEqual (7, reader[0]);
                reader.Dispose ();

                stmt.Bind (6);
                Assert.AreEqual (6, stmt.Query<int> ());
            }
        }

        [Test]
        public void TestQueryWhileReading ()
        {
            using (var stmt = con.CreateStatement ("SELECT ? as version")) {
                stmt.Bind (7);
                var reader = stmt.Query ();
                Assert.IsTrue (reader.Read ());
                Assert.AreEqual (7, reader[0]);

                try {
                    stmt.Execute ();
                    Assert.Fail ("Shouldn't be able to query while a reader is active");
                } catch {}

                try {
                    stmt.Query ();
                    Assert.Fail ("Shouldn't be able to query while a reader is active");
                } catch {}

                try {
                    stmt.Query<int> ();
                    Assert.Fail ("Shouldn't be able to query while a reader is active");
                } catch {}

                Assert.AreEqual (7, reader[0]);
                reader.Dispose ();

                stmt.Bind (6);
                Assert.AreEqual (6, stmt.Query<int> ());
            }
        }

        [Test]
        public void Test ()
        {
            using (var stmt = con.CreateStatement ("SELECT 'foobar' as version")) {
                Assert.AreEqual ("foobar", stmt.Query<string> ());

                try {
                    stmt.Bind ();
                    Assert.Fail ("should not be able to bind parameterless statement");
                } catch {}
            }

            using (var stmt = con.CreateStatement ("SELECT 2 + 5 as res")) {
                using (var reader = stmt.Query ()) {
                    Assert.IsTrue (reader.Read ());
                    Assert.AreEqual (7, reader.Get<int> (0));
                    Assert.AreEqual (7, reader[0]);
                    Assert.AreEqual (7, reader["res"]);
                }
            }
        }

        [Test]
        public void TestBinding ()
        {
            using (var stmt = con.CreateStatement ("SELECT ? as version")) {
                try {
                    stmt.First ();
                    Assert.Fail ("unbound statement should have thrown an exception");
                } catch {}

                try {
                    stmt.Bind (1, 2);
                    Assert.Fail ("bound statement with the wrong number of parameters");
                } catch {}

                try {
                    stmt.Bind ();
                    Assert.Fail ("bound statement with the wrong number of parameters");
                } catch {}

                stmt.Bind (21);
                Assert.AreEqual (21, stmt.Query<int> ());

                stmt.Bind ("ffoooo");
                using (var reader = stmt.First ()) {
                    Assert.AreEqual ("ffoooo", reader[0]);
                    Assert.AreEqual ("ffoooo", reader["version"]);
                }
            }

            using (var stmt = con.CreateStatement ("SELECT ? as a, ? as b, ?")) {
                stmt.Bind (1, "two", 3.3);

                using (var reader = stmt.Query ()) {
                    Assert.IsTrue (reader.Read ());
                    Assert.AreEqual (1, reader.Get<int> (0));
                    Assert.AreEqual ("two", reader["b"]);
                    Assert.AreEqual ("two", reader.Get<string> ("b"));
                    Assert.AreEqual ("two", reader.Get<string> (1));
                    Assert.AreEqual (3.3, reader[2]);
                }
            }
        }

        [Test]
        public void CreateTable ()
        {
            CreateUsers (con);

            using (var stmt = con.CreateStatement ("SELECT COUNT(*) FROM Users")) {
                Assert.AreEqual (2, stmt.Query<int> ());
            }

            using (var reader = con.Query ("SELECT ID, Name FROM Users ORDER BY NAME")) {
                Assert.IsTrue (reader.Read ());
                Assert.AreEqual ("Aaron", reader["Name"]);
                Assert.AreEqual ("Aaron", reader[1]);
                Assert.AreEqual (2, reader["ID"]);
                Assert.AreEqual (2, reader[0]);

                Assert.IsTrue (reader.Read ());
                Assert.AreEqual ("Gabriel", reader["Name"]);
                Assert.AreEqual ("Gabriel", reader[1]);
                Assert.AreEqual (1, reader["ID"]);
                Assert.AreEqual (1, reader[0]);

                Assert.IsFalse (reader.Read ());
            }
        }

        private void CreateUsers (Connection con)
        {
            using (var stmt = con.CreateStatement ("DROP TABLE IF EXISTS Users")) {
                stmt.Execute ();
            }

            using (var stmt = con.CreateStatement ("CREATE TABLE Users (ID INTEGER PRIMARY KEY, Name TEXT)")) {
                stmt.Execute ();
                try {
                    stmt.Execute ();
                    Assert.Fail ("Shouldn't be able to create table; already exists");
                } catch {}
            }

            using (var stmt = con.CreateStatement ("INSERT INTO Users (Name) VALUES (?)")) {
                stmt.Bind ("Gabriel").Execute ();
                stmt.Bind ("Aaron").Execute ();
            }
        }

        [Test]
        public void CheckInterleavedAccess ()
        {
            CreateUsers (con);

            var q1 = con.Query ("SELECT ID, Name FROM Users ORDER BY NAME ASC");
            var q2 = con.Query ("SELECT ID, Name FROM Users ORDER BY ID ASC");

            Assert.IsTrue (q1.Read ());
            Assert.IsTrue (q2.Read ());
            Assert.AreEqual ("Aaron", q1["Name"]);
            Assert.AreEqual ("Gabriel", q2["Name"]);

            con.Execute ("INSERT INTO Users (Name) VALUES ('Zeus')");
            Assert.AreEqual (3, con.Query<int> ("SELECT COUNT(*) FROM Users"));

            Assert.IsTrue (q2.Read ());
            Assert.AreEqual ("Aaron", q2[1]);
            Assert.IsTrue (q1.Read ());
            Assert.AreEqual ("Gabriel", q1[1]);

            // The new value is already passed when sorting by Name ASC
            // But it had ID=3, so it's available to the second query
            Assert.IsFalse (q1.Read ());
            Assert.IsTrue (q2.Read ());
            Assert.AreEqual ("Zeus", q2[1]);

            // Insert a value, see that q2 can see it, then delete it and try to
            // get the now-deleted value from q2
            con.Execute ("INSERT INTO Users (Name) VALUES ('Apollo')");
            Assert.AreEqual (4, con.Query<int> ("SELECT COUNT(*) FROM Users"));
            Assert.IsTrue (q2.Read ());

            con.Execute ("DELETE FROM Users WHERE Name='Apollo'");
            Assert.AreEqual (3, con.Query<int> ("SELECT COUNT(*) FROM Users"));
            Assert.AreEqual ("Apollo", q2[1]);
            Assert.IsFalse (q2.Read ());

            try {
                Console.WriteLine (q1[1]);
                Assert.Fail ("Should have thrown");
            } catch {}

            q1.Dispose ();
            q2.Dispose ();
        }

        [Test]
        public void ConnectionDisposesStatements ()
        {
            var stmt = con.CreateStatement ("SELECT 1");
            Assert.IsFalse (stmt.IsDisposed);
            con.Dispose ();
            Assert.IsTrue (stmt.IsDisposed);
        }

        [Test]
        public void MultipleCommands ()
        {
            try {
                using (var stmt = con.CreateStatement ("CREATE TABLE Lusers (ID INTEGER PRIMARY KEY, Name TEXT); INSERT INTO Lusers (Name) VALUES ('Foo')")) {
                    stmt.Execute ();
                }
                Assert.Fail ("Mutliple commands aren't supported in this sqlite binding");
            } catch {}
        }

        [Test]
        public void Query ()
        {
            using (var q = con.Query ("SELECT 7")) {
                int rows = 0;
                while (q.Read ()) {
                    Assert.AreEqual (7, q[0]);
                    rows++;
                }
                Assert.AreEqual (1, rows);
            }
        }

        [Test]
        public void QueryScalar ()
        {
            Assert.AreEqual (7, con.Query<int> ("SELECT 7"));
        }

        [Test]
        public void Execute ()
        {
            try {
                con.Query<int> ("SELECT COUNT(*) FROM Users");
                Assert.Fail ("Should have thrown an exception");
            } catch {}
            con.Execute ("CREATE TABLE Users (ID INTEGER PRIMARY KEY, Name TEXT)");
            Assert.AreEqual (0, con.Query<int> ("SELECT COUNT(*) FROM Users"));
        }

        [Test]
        public void Md5Function ()
        {
            using (var stmt = con.CreateStatement ("SELECT HYENA_MD5(?, ?)")) {
                Assert.AreEqual ("ae2b1fca515949e5d54fb22b8ed95575", stmt.Bind (1, "testing").Query<string> ());
                Assert.AreEqual (null, stmt.Bind (1, null).Query<string> ());
            }

            using (var stmt = con.CreateStatement ("SELECT HYENA_MD5(?, ?, ?)")) {
                Assert.AreEqual ("ae2b1fca515949e5d54fb22b8ed95575", stmt.Bind (2, "test", "ing").Query<string> ());
                Assert.AreEqual (null, stmt.Bind (2, null, null).Query<string> ());
            }

            using (var stmt = con.CreateStatement ("SELECT HYENA_MD5(?, ?, ?, ?)")) {
                Assert.AreEqual (null, stmt.Bind (3, null, "", null).Query<string> ());

                try {
                    con.RemoveFunction<Md5Function> ();
                    Assert.Fail ("Removed function while statement active");
                } catch (SqliteException e) {
                    Assert.AreEqual (5, e.ErrorCode);
                }
            }

            try {
                using (var stmt = con.CreateStatement ("SELECT HYENA_MD5(?, ?, ?, ?)")) {
                    Assert.AreEqual ("ae2b1fca515949e5d54fb22b8ed95575", stmt.Query<string> ());
                    Assert.Fail ("Function HYENA_MD5 should no longer exist");
                }
            } catch {}

        }

        [Test]
        public void SearchKeyFunction ()
        {
            using (var stmt = con.CreateStatement ("SELECT HYENA_SEARCH_KEY(?)")) {
                Assert.AreEqual (null,  stmt.Bind (null).Query<string> ());
                Assert.AreEqual ("eee", stmt.Bind ("Éee").Query<string> ());
                Assert.AreEqual ("a",   stmt.Bind ("\u0104").Query<string> ());
            }

            con.Execute ("SELECT HYENA_SEARCH_KEY('foo')");
        }

        [Test]
        public void CollationKeyFunction ()
        {
            using (var stmt = con.CreateStatement ("SELECT HYENA_COLLATION_KEY(?)")) {
                Assert.AreEqual (new byte[] {14, 2, 1, 27, 1, 1, 1, 0}, stmt.Bind ("\u0104").Query<byte[]> ());
            }
        }

        [Test]
        public void DataTypes ()
        {
            AssertGetNull<int> (0);
            AssertRoundTrip<int> (0);
            AssertRoundTrip<int> (1);
            AssertRoundTrip<int> (-1);
            AssertRoundTrip<int> (42);
            AssertRoundTrip<int> (int.MaxValue);
            AssertRoundTrip<int> (int.MinValue);

            AssertGetNull<uint> (0);
            AssertRoundTrip<uint> (0);
            AssertRoundTrip<uint> (1);
            AssertRoundTrip<uint> (42);
            AssertRoundTrip<uint> (uint.MaxValue);
            AssertRoundTrip<uint> (uint.MinValue);

            AssertGetNull<long> (0);
            AssertRoundTrip<long> (0);
            AssertRoundTrip<long> (1);
            AssertRoundTrip<long> (-1);
            AssertRoundTrip<long> (42);
            AssertRoundTrip<long> (long.MaxValue);
            AssertRoundTrip<long> (long.MinValue);

            AssertGetNull<ulong> (0);
            AssertRoundTrip<ulong> (0);
            AssertRoundTrip<ulong> (1);
            AssertRoundTrip<ulong> (42);
            AssertRoundTrip<ulong> (ulong.MaxValue);
            AssertRoundTrip<ulong> (ulong.MinValue);

            AssertGetNull<float> (0f);
            AssertRoundTrip<float> (0f);
            AssertRoundTrip<float> (1f);
            AssertRoundTrip<float> (-1f);
            AssertRoundTrip<float> (42.222f);
            AssertRoundTrip<float> (float.MaxValue);
            AssertRoundTrip<float> (float.MinValue);

            AssertGetNull<double> (0);
            AssertRoundTrip<double> (0);
            AssertRoundTrip<double> (1);
            AssertRoundTrip<double> (-1);
            AssertRoundTrip<double> (42.222);
            AssertRoundTrip<double> (double.MaxValue);
            AssertRoundTrip<double> (double.MinValue);

            AssertGetNull<string> (null);
            AssertRoundTrip<string> ("a");
            AssertRoundTrip<string> ("üb€r;&#co¯ol!~`\n\r\t");

            AssertGetNull<byte[]> (null);
            AssertRoundTrip<byte[]> (new byte [] { 0 });
            AssertRoundTrip<byte[]> (new byte [] { 0, 1});
            AssertRoundTrip<byte[]> (System.Text.Encoding.UTF8.GetBytes ("üb€r;&#co¯ol!~`\n\r\t"));

            var ignore_ms = new Func<DateTime, DateTime, bool> ((a, b) => (a - b).TotalSeconds < 1);
            AssertGetNull<DateTime> (DateTime.MinValue);
            AssertRoundTrip<DateTime> (new DateTime (1970, 1, 1).ToLocalTime ());
            AssertRoundTrip<DateTime> (DateTime.Now, ignore_ms);
            AssertRoundTrip<DateTime> (DateTime.MinValue);
            // FIXME
            //AssertRoundTrip<DateTime> (DateTime.MaxValue);
            Assert.AreEqual (new DateTime (1970, 1, 1).ToLocalTime (), con.Query<DateTime> ("SELECT 0"));

            AssertGetNull<TimeSpan> (TimeSpan.MinValue);
            AssertRoundTrip<TimeSpan> (TimeSpan.MinValue);
            AssertRoundTrip<TimeSpan> (TimeSpan.FromSeconds (0));
            AssertRoundTrip<TimeSpan> (TimeSpan.FromSeconds (0.001));
            AssertRoundTrip<TimeSpan> (TimeSpan.FromSeconds (0.002));
            AssertRoundTrip<TimeSpan> (TimeSpan.FromSeconds (0.503));
            AssertRoundTrip<TimeSpan> (TimeSpan.FromSeconds (1.01));
            AssertRoundTrip<TimeSpan> (TimeSpan.FromHours (999.00193));
            // FIXME
            //AssertRoundTrip<TimeSpan> (TimeSpan.MaxValue);
        }

        private void AssertRoundTrip<T> (T val)
        {
            AssertRoundTrip (val, null);
        }

        private void AssertRoundTrip<T> (T val, Func<T, T, bool> func)
        {
            var o = select_literal.Bind (val).Query<T> ();
            if (func == null) {
                Assert.AreEqual (val, o);
            } else {
                Assert.IsTrue (func (val, o));
            }
        }

        private void AssertGetNull<T> (T val)
        {
            Assert.AreEqual (val, select_literal.Bind (null).Query<T> ());
        }
    }
}

#endif
