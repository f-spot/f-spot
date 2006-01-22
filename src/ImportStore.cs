using Mono.Data.SqliteClient;
using System.Collections;
using System.IO;
using System;


public class ImportStore : DbStore {

	public class Import : DbItem {
		// The time is always in UTC.
		private DateTime time;
		public DateTime Time {
			get {
				return time;
			}
		}

		public Import (uint id, long unix_time)
			: base (id)
		{
			time = DbUtils.DateTimeFromUnixTime (unix_time);
		}
	}

	private class ImportComparerByDate : IComparer {
		public int Compare (Object a, Object b) {
			return DateTime.Compare ((a as Import).Time, (b as Import).Time);
		}
	}

	// Constructor

	public ImportStore (SqliteConnection connection, bool is_new)
		: base (connection, false)
	{
		if (! is_new)
			return;
		
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText =
			"CREATE TABLE imports (                            " +
			"	id          INTEGER PRIMARY KEY NOT NULL,  " +
			"       time        INTEGER			   " +
			")";

		command.ExecuteNonQuery ();
		command.Dispose ();
	}

	public Import Create (DateTime time_in_utc)
	{
		long unix_time = DbUtils.UnixTimeFromDateTime (time_in_utc);

		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText = String.Format ("INSERT INTO import (time) VALUES ({0})  ",
						     unix_time);
		command.ExecuteScalar ();
		command.Dispose ();

		uint id = (uint) Connection.LastInsertRowId;
		Import import = new Import (id, unix_time);
		AddToCache (import);

		return import;
	}

	public override DbItem Get (uint id)
	{
		Import import = LookupInCache (id) as Import;
		if (import != null)
			return import;

		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText = String.Format ("SELECT time FROM imports WHERE id = {0}", id);
		SqliteDataReader reader = command.ExecuteReader ();

		if (reader.Read ()) {
			import = new Import (id, Convert.ToUInt32 (reader [0]));
			AddToCache (import);
		}

		command.Dispose ();

		return import;
	}

	public override void Remove (DbItem item)
	{
		RemoveFromCache (item);

		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText = String.Format ("DELETE FROM imports WHERE id = {0}", item.Id);
		command.ExecuteNonQuery ();

		command.Dispose ();
	}

	public override void Commit (DbItem item)
	{
		// Nothing to do here, since all the properties of an import are immutable.
	}

	// FIXME: Maybe this should be an abstract method in the base class?
	public ArrayList GetAll ()
	{
		ArrayList list = new ArrayList ();

		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText = "SELECT id, time FROM imports";
		SqliteDataReader reader = command.ExecuteReader ();

		while (reader.Read ()) {
			// Note that we get both time and ID from the database, but we have to see
			// if the item is already in the cache first to make sure we return always
			// the same object for a given ID.
			
			uint id = Convert.ToUInt32 (reader[0]);

			Import import = LookupInCache (id) as Import;
			if (import == null) {
				import = new Import (id, Convert.ToUInt32 (reader[1]));
				AddToCache (import);
			}

			list.Add (import);
		}

		command.Dispose ();

		list.Sort (new ImportComparerByDate ());
		return list;
	}
}
