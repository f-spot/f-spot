//
// RollStore.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Ettore Perazzoli <ettore@src.gnome.org>
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2003-2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
// Copyright (C) 2003 Ettore Perazzoli
// Copyright (C) 2007-2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Core;

using Hyena;
using Hyena.Data.Sqlite;

namespace FSpot.Database
{
	public class RollStore : DbStore<Roll>
	{
		public RollStore (IDb db, bool isNew) : base (db, false)
		{
			if (!isNew && Database.TableExists ("rolls"))
				return;

			Database.Execute (
			"CREATE TABLE rolls (\n" +
			"	id	INTEGER PRIMARY KEY NOT NULL, \n" +
			"       time	INTEGER NOT NULL\n" +
			")");
		}

		public Roll Create (DateTime timeInUtc)
		{
			long unix_time = DateTimeUtil.FromDateTime (timeInUtc);
			uint id = (uint)Database.Execute (new HyenaSqliteCommand ("INSERT INTO rolls (time) VALUES (?)", unix_time));

			Roll roll = new Roll (id, unix_time);
			AddToCache (roll);

			return roll;
		}

		public Roll Create ()
		{
			return Create (DateTime.UtcNow);
		}

		public override Roll Get (uint id)
		{
			var roll = LookupInCache (id);
			if (roll != null)
				return roll;

			Hyena.Data.Sqlite.IDataReader reader = Database.Query (new HyenaSqliteCommand ("SELECT time FROM rolls WHERE id = ?", id));

			if (reader.Read ()) {
				roll = new Roll (id, Convert.ToUInt32 (reader["time"]));
				AddToCache (roll);
			}

			reader.Dispose ();

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
					number_of_photos = Convert.ToUInt32 (reader["count"]);

				reader.Dispose ();
			}
			return number_of_photos;
		}

		public Roll[] GetRolls ()
		{
			return GetRolls (-1);
		}

		public Roll[] GetRolls (int limit)
		{
			var rolls = new List<Roll> ();

			string query = "SELECT DISTINCT rolls.id AS roll_id, rolls.time AS roll_time FROM rolls, photos WHERE photos.roll_id = rolls.id ORDER BY rolls.time DESC";
			if (limit >= 0)
				query += " LIMIT " + limit;

			using (IDataReader reader = Database.Query (query)) {
				while (reader.Read ()) {
					uint id = Convert.ToUInt32 (reader["roll_id"]);

					Roll roll = LookupInCache (id);
					if (roll == null) {
						roll = new Roll (id, Convert.ToUInt32 (reader["roll_time"]));
						AddToCache (roll);
					}
					rolls.Add (roll);
				}
				reader.Dispose ();
			}
			return rolls.ToArray ();
		}
	}
}
