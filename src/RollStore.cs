/*
 * RollStore.cs
 *
 * Author(s)
 *	Ettore Perazzoli <ettore@perazzoli.org>
 *	Bengt Thuree
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using Mono.Data.SqliteClient;
using System.Collections;
using System.IO;
using System;
using Banshee.Database;
using FSpot.Utils;
using FSpot;

public class RollStore : DbStore<Roll>
{
	public RollStore (QueuedSqliteDatabase database, bool is_new) : base (database, false)
	{
		if (!is_new && Database.TableExists("rolls"))
			return;

		Database.ExecuteNonQuery (
			"CREATE TABLE rolls (\n" +
			"	id	INTEGER PRIMARY KEY NOT NULL, \n" +
			"       time	INTEGER NOT NULL\n" +
			")");
	}

	public Roll Create (DateTime time_in_utc)
	{
		long unix_time = DbUtils.UnixTimeFromDateTime (time_in_utc);
		uint id = (uint) Database.Execute (new DbCommand ("INSERT INTO rolls (time) VALUES (:time)", "time", unix_time));

		Roll roll = new Roll (id, unix_time);
		AddToCache (roll);

		return roll;
	}

	public Roll Create ()
	{
		return Create (System.DateTime.UtcNow);
	}

	public override Roll Get (uint id)
	{
		Roll roll = LookupInCache (id) as Roll;
		if (roll != null)
			return roll;

		SqliteDataReader reader = Database.Query(new DbCommand ("SELECT time FROM rolls WHERE id = :id", "id", id));

		if (reader.Read ()) {
			roll = new Roll (id, Convert.ToUInt32 (reader ["time"]));
			AddToCache (roll);
		}

		return roll;
	}

	public override void Remove (Roll item)
	{
		RemoveFromCache (item);
		Database.ExecuteNonQuery (new DbCommand ("DELETE FROM rolls WHERE id = :id", "id", item.Id));
	}

	public override void Commit (Roll item)
	{
		// Nothing to do here, since all the properties of a roll are immutable.
	}

	public uint PhotosInRoll (Roll roll)
	{
		uint number_of_photos = 0;
		using (SqliteDataReader reader = Database.Query (new DbCommand ("SELECT count(*) AS count FROM photos WHERE roll_id = :id", "id", roll.Id))) {
			if (reader.Read ())
				number_of_photos = Convert.ToUInt32 (reader ["count"]);
               
			reader.Close ();
		}
                return number_of_photos;
	}

	public Roll [] GetRolls ()
	{
		return GetRolls (-1);
	}

	public Roll [] GetRolls (int limit)
	{
		ArrayList list = new ArrayList ();

		string query = "SELECT DISTINCT rolls.id AS roll_id, rolls.time AS roll_time FROM rolls, photos WHERE photos.roll_id = rolls.id ORDER BY rolls.time DESC";
		if (limit >= 0)
			query += " LIMIT " + limit;

		using (SqliteDataReader reader = Database.Query(query)) {
			while (reader.Read ()) {
				uint id = Convert.ToUInt32 (reader["roll_id"]);

				Roll roll = LookupInCache (id) as Roll;
				if (roll == null) {
					roll = new Roll (id, Convert.ToUInt32 (reader["roll_time"]));
					AddToCache (roll);
				}
				list.Add (roll);
			}
			reader.Close ();
		}
		return (Roll []) list.ToArray (typeof (Roll));
	}
}
