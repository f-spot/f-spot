//
// SqliteCommandTests.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
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
using System.Reflection;
using NUnit.Framework;
using Hyena.Data.Sqlite;

namespace Hyena.Data.Sqlite.Tests
{
    [TestFixture]
    public class SqliteCommandTests
    {
        [Test]
        public void IdentifiesParameters ()
        {
            HyenaSqliteCommand cmd = null;
            try {
                cmd = new HyenaSqliteCommand ("select foo from bar where baz = ?, bbz = ?, this = ?",
                    "a", 32);
                Assert.Fail ("Should not have been able to pass 2 values to ApplyValues without exception");
            } catch {}

            try {
                cmd = new HyenaSqliteCommand ("select foo from bar where baz = ?, bbz = ?, this = ?",
                    "a", 32, "22");
            } catch {
                Assert.Fail ("Should have been able to pass 3 values to ApplyValues without exception");
            }

            Assert.AreEqual ("select foo from bar where baz = 'a', bbz = 32, this = '22'", GetGeneratedSql (cmd));
        }

        [Test]
        public void Constructor ()
        {
            HyenaSqliteCommand cmd = new HyenaSqliteCommand ("select foo from bar where baz = ?, bbz = ?, this = ?", "a", 32, "22");
            Assert.AreEqual ("select foo from bar where baz = 'a', bbz = 32, this = '22'", GetGeneratedSql (cmd));
        }

        [Test]
        public void CultureInvariant ()
        {
            HyenaSqliteCommand cmd = new HyenaSqliteCommand ("select foo from bar where baz = ?", 32.2);
            Assert.AreEqual ("select foo from bar where baz = 32.2", GetGeneratedSql (cmd));
        }

        [Test]
        public void ParameterSerialization ()
        {
            HyenaSqliteCommand cmd = new HyenaSqliteCommand ("select foo from bar where baz = ?");

            Assert.AreEqual ("select foo from bar where baz = NULL", GetGeneratedSql (cmd, null));
            Assert.AreEqual ("select foo from bar where baz = 'It''s complicated, \"but\" ''''why not''''?'", GetGeneratedSql (cmd, "It's complicated, \"but\" ''why not''?"));
            Assert.AreEqual ("select foo from bar where baz = 0", GetGeneratedSql (cmd, new DateTime (1970, 1, 1).ToLocalTime ()));
            Assert.AreEqual ("select foo from bar where baz = 931309200", GetGeneratedSql (cmd, new DateTime (1999, 7, 7).ToLocalTime ()));
            Assert.AreEqual ("select foo from bar where baz = 555.55", GetGeneratedSql (cmd, 555.55f));
            Assert.AreEqual ("select foo from bar where baz = 555.55", GetGeneratedSql (cmd, 555.55));
            Assert.AreEqual ("select foo from bar where baz = 555", GetGeneratedSql (cmd, 555));
            Assert.AreEqual ("select foo from bar where baz = 1", GetGeneratedSql (cmd, true));
            Assert.AreEqual ("select foo from bar where baz = 0", GetGeneratedSql (cmd, false));

            HyenaSqliteCommand cmd2 = new HyenaSqliteCommand ("select foo from bar where baz = ?, bar = ?, boo = ?");
            Assert.AreEqual ("select foo from bar where baz = NULL, bar = NULL, boo = 22", GetGeneratedSql (cmd2, null, null, 22));

            HyenaSqliteCommand cmd3 = new HyenaSqliteCommand ("select foo from bar where id in (?) and foo not in (?)");
            Assert.AreEqual ("select foo from bar where id in (1,2,4) and foo not in ('foo','baz')",
                    GetGeneratedSql (cmd3, new int [] {1, 2, 4}, new string [] {"foo", "baz"}));
        }

        static PropertyInfo tf = typeof(HyenaSqliteCommand).GetProperty ("CurrentSqlText", BindingFlags.Instance | BindingFlags.NonPublic);
        private static string GetGeneratedSql (HyenaSqliteCommand cmd, params object [] p)
        {
            return tf.GetValue ((new HyenaSqliteCommand (cmd.Text, p)), null) as string;
        }

        private static string GetGeneratedSql (HyenaSqliteCommand cmd)
        {
            return tf.GetValue (cmd, null) as string;
        }
    }

    [TestFixture]
    public class ObjectToSqlTests
    {
        protected void AssertToSql (object o, object expected)
        {
            Assert.AreEqual (expected, HyenaSqliteCommand.SqlifyObject (o));
        }

        [Test]
        public void TestNull ()
        {
            AssertToSql (null, "NULL");
        }

        [Test]
        public void TestBool ()
        {
            AssertToSql (false, "0");
            AssertToSql (true, "1");
        }

        [Test]
        public void TestString ()
        {
            AssertToSql ("", "''");
            AssertToSql ("test", "'test'");
            AssertToSql ("te'st", "'te''st'");
        }

        [Test]
        public void TestByteArray ()
        {
            // BLOB notation
            AssertToSql (new byte[] {}, "X''");
            AssertToSql (new byte[] {0x10, 0x20, 0x30}, "X'102030'");
        }

        [Test]
        public void TestOtherArray ()
        {
            AssertToSql (new object[] {}, "");
            AssertToSql (new object[] {"a"}, "'a'");
            AssertToSql (new object[] {"a", "b"}, "'a','b'");
        }

        [Test]
        public void TestDateTime ()
        {
            // Returned using local time, not UTC
            AssertToSql (new DateTime (2000, 1, 2), 946792800);

            // Disregards milliseconds
            AssertToSql (new DateTime (2000, 1, 2, 10, 9, 8, 7), 946829348);
        }
    }
}

#endif
