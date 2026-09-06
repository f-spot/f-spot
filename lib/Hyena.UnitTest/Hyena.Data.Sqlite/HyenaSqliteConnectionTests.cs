//
// HyenaSqliteConnectionTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyena.Data.Sqlite.Tests
{
	[TestClass]
	public class HyenaSqliteConnectionTests
	{
		string dbfile;
		HyenaSqliteConnection connection;

		[TestInitialize]
		public void Setup ()
		{
			dbfile = Path.Combine (Path.GetTempPath (), "hyena-sqlite-connection-" + Guid.NewGuid ().ToString ("N") + ".db");
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
