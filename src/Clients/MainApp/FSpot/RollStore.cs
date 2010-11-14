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

using System.Collections;
using System.IO;
using System;

using FSpot.Core;
using FSpot.Database;
using FSpot.Utils;
using FSpot;

using Hyena;
using Hyena.Data.Sqlite;

namespace FSpot {
public class RollStore : DbStore<Roll>
{
	public RollStore (FSpotDatabaseConnection database, bool is_new) : base (database, false)
	{
		if (!is_new && Database.TableExists("rolls"))
			return;

		Database.Execute (
			"CREATE TABLE rolls (\n" +
			"	id	INTEGER PRIMARY KEY NOT NULL, \n" +
			"       time	INTEGER NOT NULL\n" +
			")");
	}

	public Roll Create (DateTime time_in_utc)
	{
		long unix_time = DateTimeUtil.FromDateTime (time_in_utc);
		uint id = (uint) Database.Execute (new HyenaSqliteCommand ("INSERT INTO rolls (time) VALUES (?)", unix_time));

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

		IDataReader reader = Database.Query(new HyenaSqliteCommand ("SELECT time FROM rolls WHERE id = ?", id));

		if (reader.Read ()) {
			roll = new Roll (id, Convert.ToUInt32 (reader ["time"]));
			AddToCache (roll);
		}

        reader.Dispose();

		return roll;
	}

	public override void Remove (Roll item)
	{
		RemoveFromCache (item);
		Database.Execute (new HyenaSqliteCommand ("DELETE FROM rolls WHERE id = ?", item.Id));
	}

	public override void Commit (Roll item)
	{
		// Nothing to do here, since all the properties of a roll are immutable.
	}

	public uint PhotosInRoll (Roll roll)
	{
		uint number_of_photos = 0;
		using (IDataReader reader = Database.Query (new HyenaSqliteCommand ("SELECT count(*) AS count FROM photos WHERE roll_id = ?", roll.Id))) {
			if (reader.Read ())
				number_of_photos = Convert.ToUInt32 (reader ["count"]);

			reader.Dispose ();
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

		using (IDataReader reader = Database.Query(query)) {
			while (reader.Read ()) {
				uint id = Convert.ToUInt32 (reader["roll_id"]);

				Roll roll = LookupInCache (id) as Roll;
				if (roll == null) {
					roll = new Roll (id, Convert.ToUInt32 (reader["roll_time"]));
					AddToCache (roll);
				}
				list.Add (roll);
			}
			reader.Dispose ();
		}
		return (Roll []) list.ToArray (typeof (Roll));
	}
}
}