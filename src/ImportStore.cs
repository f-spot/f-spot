using Mono.Data.SqliteClient;
using System.Collections;
using System.IO;
using System;
using Banshee.Database;


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

	public ImportStore (QueuedSqliteDatabase database, bool is_new)
		: base (database, false)
	{
		if (! is_new)
			return;
		
		Database.ExecuteNonQuery (
			"CREATE TABLE imports (                            " +
			"	id          INTEGER PRIMARY KEY NOT NULL,  " +
			"       time        INTEGER			   " +
			")");

	}

	public Import Create (DateTime time_in_utc)
	{
		long unix_time = DbUtils.UnixTimeFromDateTime (time_in_utc);

		uint id = (uint)Database.Execute (new DbCommand ("INSERT INTO import (time) VALUES (:time)", "time", unix_time));

		Import import = new Import (id, unix_time);
		AddToCache (import);

		return import;
	}

	public override DbItem Get (uint id)
	{
		Import import = LookupInCache (id) as Import;
		if (import != null)
			return import;

		
		SqliteDataReader reader = Database.Query(new DbCommand ("SELECT time FROM imports WHERE id = :id", "id", id));

		if (reader.Read ()) {
			import = new Import (id, Convert.ToUInt32 (reader [0]));
			AddToCache (import);
		}

		return import;
	}

	public override void Remove (DbItem item)
	{
		RemoveFromCache (item);

		Database.ExecuteNonQuery (new DbCommand ("DELETE FROM imports WHERE id = :id", "id", item.Id));
	}

	public override void Commit (DbItem item)
	{
		// Nothing to do here, since all the properties of an import are immutable.
	}

	// FIXME: Maybe this should be an abstract method in the base class?
	public ArrayList GetAll ()
	{
		ArrayList list = new ArrayList ();

		SqliteDataReader reader = Database.Query("SELECT id, time FROM imports"); 

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

		list.Sort (new ImportComparerByDate ());
		return list;
	}
}
