//
// SqliteCommandTests.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Database.Tests
{
	[TestClass]
	public class SqliteCommandTests
	{
		string dbfile;
		HyenaSqliteConnection connection;

		[TestInitialize]
		public void Setup ()
		{
			dbfile = Path.Combine (Path.GetTempPath (), "sqlite-command-" + Guid.NewGuid ().ToString ("N") + ".db");
			connection = new HyenaSqliteConnection (dbfile);
		}

		[TestCleanup]
		public void TearDown ()
		{
			connection?.Dispose ();
			if (dbfile != null && File.Exists (dbfile)) {
				File.Delete (dbfile);
			}
		}

		[TestMethod]
		public void WrongParameterCountThrows ()
		{
			Assert.ThrowsExactly<ArgumentException> (() =>
				connection.Execute ("INSERT INTO t (a, b, c) VALUES (?, ?, ?)", "a", 32));
		}

		[TestMethod]
		public void StringWithQuotesRoundTrips ()
		{
			connection.Execute ("CREATE TABLE t (s TEXT)");
			const string value = "It's complicated, \"but\" ''why not''?";
			connection.Execute ("INSERT INTO t (s) VALUES (?)", value);
			Assert.AreEqual (value, connection.Query<string> ("SELECT s FROM t"));
		}

		[TestMethod]
		public void NullRoundTrips ()
		{
			connection.Execute ("CREATE TABLE t (s TEXT, n INTEGER)");
			connection.Execute ("INSERT INTO t (s, n) VALUES (?, ?)", null, 22);
			Assert.IsNull (connection.Query<string> ("SELECT s FROM t"));
			Assert.AreEqual (22, connection.Query<int> ("SELECT n FROM t"));
		}

		[TestMethod]
		public void BoolIsStoredAsInteger ()
		{
			connection.Execute ("CREATE TABLE t (b INTEGER)");
			connection.Execute ("INSERT INTO t (b) VALUES (?)", true);
			Assert.AreEqual ("integer", connection.Query<string> ("SELECT typeof(b) FROM t"));
			Assert.AreEqual (1, connection.Query<int> ("SELECT b FROM t"));

			connection.Execute ("DELETE FROM t");
			connection.Execute ("INSERT INTO t (b) VALUES (?)", false);
			Assert.AreEqual (0, connection.Query<int> ("SELECT b FROM t"));
		}

		[TestMethod]
		public void DoubleIsCultureInvariant ()
		{
			connection.Execute ("CREATE TABLE t (n REAL)");
			connection.Execute ("INSERT INTO t (n) VALUES (?)", 32.2);
			Assert.AreEqual (32.2, connection.Query<double> ("SELECT n FROM t"), 0.0001);
		}

		[TestMethod]
		public void ByteArrayRoundTripsAsBlob ()
		{
			connection.Execute ("CREATE TABLE t (b BLOB)");
			var value = new byte[] { 0x10, 0x20, 0x30 };
			connection.Execute ("INSERT INTO t (b) VALUES (?)", value);
			Assert.AreEqual ("blob", connection.Query<string> ("SELECT typeof(b) FROM t"));
			using (var reader = connection.Query ("SELECT b FROM t")) {
				Assert.IsTrue (reader.Read ());
				Assert.AreSequenceEqual (value, reader.Get<byte[]> (0));
			}
		}

		[TestMethod]
		public void NestedBeginTransactionThrowsWhenAlreadyInTransaction ()
		{
			connection.BeginTransaction ();
			Assert.IsTrue (connection.InTransaction);
			Assert.ThrowsExactly<Exception> (() => connection.BeginTransaction ());
			connection.RollbackTransaction ();
			Assert.IsFalse (connection.InTransaction);
		}
	}
}
