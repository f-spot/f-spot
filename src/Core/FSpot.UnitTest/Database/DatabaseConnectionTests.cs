//
// DatabaseConnectionTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Database.Tests
{
	[TestClass]
	public class DatabaseConnectionTests
	{
		string dbfile;
		DatabaseConnection connection;

		[TestInitialize]
		public void Setup ()
		{
			dbfile = Path.Combine (Path.GetTempPath (), "database-connection-" + Guid.NewGuid ().ToString ("N") + ".db");
			connection = new DatabaseConnection (dbfile);
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
		public void DateTimeIsStoredAsUnixTimeInteger ()
		{
			connection.Execute ("CREATE TABLE t (d INTEGER)");
			var dt = new DateTime (2000, 1, 2, 3, 4, 5);

			connection.Execute ("INSERT INTO t (d) VALUES (?)", dt);

			Assert.AreEqual ("integer", connection.Query<string> ("SELECT typeof(d) FROM t"));
			Assert.AreEqual (DateTimeUtil.ToTimeT (dt), connection.Query<long> ("SELECT d FROM t"));
			Assert.AreEqual (dt, connection.Query<DateTime> ("SELECT d FROM t"));
		}

		[TestMethod]
		public void SearchKeyFunctionIsRegisteredOnConnection ()
		{
			Assert.AreEqual ("eee", connection.Query<string> ("SELECT HYENA_SEARCH_KEY('Éee')"));
			Assert.AreEqual ("a", connection.Query<string> ("SELECT HYENA_SEARCH_KEY(?)", "\u0104"));
			Assert.IsNull (connection.Query<string> ("SELECT HYENA_SEARCH_KEY(?)", null));
		}

		[TestMethod]
		public void CollationKeyFunctionIsRegisteredOnConnection ()
		{
			using (var reader = connection.Query ("SELECT HYENA_COLLATION_KEY(?)", "\u0104")) {
				Assert.IsTrue (reader.Read ());
				Assert.AreSequenceEqual (StringUtil.SortKey ("\u0104"), reader.Get<byte[]> (0));
			}

			using (var reader = connection.Query ("SELECT HYENA_COLLATION_KEY(?)", null)) {
				Assert.IsTrue (reader.Read ());
				Assert.IsNull (reader.Get<byte[]> (0));
			}
		}

		[TestMethod]
		public void Md5FunctionIsRegisteredOnConnection ()
		{
			Assert.AreEqual ("ae2b1fca515949e5d54fb22b8ed95575", connection.Query<string> ("SELECT HYENA_MD5(?, ?)", 1, "testing"));
			Assert.AreEqual ("ae2b1fca515949e5d54fb22b8ed95575", connection.Query<string> ("SELECT HYENA_MD5(?, ?, ?)", 2, "test", "ing"));
			Assert.IsNull (connection.Query<string> ("SELECT HYENA_MD5(?, ?)", 1, null));
		}

		[TestMethod]
		public void ExecuteAndQueryRoundTripRows ()
		{
			connection.Execute ("CREATE TABLE Users (ID INTEGER PRIMARY KEY, Name TEXT)");
			Assert.AreEqual (1L, connection.Execute ("INSERT INTO Users (Name) VALUES (?)", "Gabriel"));
			Assert.AreEqual (2L, connection.Execute ("INSERT INTO Users (Name) VALUES (?)", "Aaron"));
			Assert.AreEqual (2, connection.Query<int> ("SELECT COUNT(*) FROM Users"));

			using (var reader = connection.Query ("SELECT ID, Name FROM Users ORDER BY NAME")) {
				Assert.IsTrue (reader.Read ());
				Assert.AreEqual ("Aaron", reader["Name"]);
				Assert.AreEqual (2L, Convert.ToInt64 (reader["ID"]));
				Assert.IsTrue (reader.Read ());
				Assert.AreEqual ("Gabriel", reader.Get<string> (1));
				Assert.IsFalse (reader.Read ());
			}
		}

		[TestMethod]
		public void DisposeReleasesDatabaseFile ()
		{
			connection.Execute ("CREATE TABLE t (id INTEGER)");
			connection.Dispose ();
			connection = null;

			File.Delete (dbfile);
			Assert.IsFalse (File.Exists (dbfile));
		}
	}
}
