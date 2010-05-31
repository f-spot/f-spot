/*
 * RetroactiveRoll.cs
 *
 * Author(s)
 * 	Andy Wingo  <wingo@pobox.com>
 *
 * This is free software. See COPYING for details
 */


using FSpot;
using FSpot.Extensions;
using Mono.Unix;
using System;
using Mono.Data.SqliteClient;
using Banshee.Database;
using Hyena;

namespace RetroactiveRoll
{
	public class RetroactiveRoll: ICommand
	{
		public void Run (object o, EventArgs e)
		{
			Photo[] photos = App.Instance.Organizer.SelectedPhotos ();

			if (photos.Length == 0) {
				Log.Debug ("no photos selected, returning");
				return;
			}

			DateTime import_time = photos[0].Time;
			foreach (Photo p in photos)
				if (p.Time > import_time)
					import_time = p.Time;

			RollStore rolls = App.Instance.Database.Rolls;
			Roll roll = rolls.Create(import_time);
			foreach (Photo p in photos) {
				DbCommand cmd = new DbCommand ("UPDATE photos SET roll_id = :roll_id " +
							       "WHERE id = :id ",
							       "roll_id", roll.Id,
							       "id", p.Id);
				App.Instance.Database.Database.ExecuteNonQuery (cmd);
				p.RollId = roll.Id;
			}
			Log.Debug ("RetroactiveRoll done: " + photos.Length + " photos in roll " + roll.Id);
		}
	}
}
