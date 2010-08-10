/*
 * RetroactiveRoll.cs
 *
 * Author(s)
 * 	Andy Wingo  <wingo@pobox.com>
 *
 * This is free software. See COPYING for details
 */


using FSpot;
using FSpot.Core;
using FSpot.Extensions;
using System;
using Hyena;

using Hyena.Data.Sqlite;

namespace FSpot.Tools.RetroactiveRoll
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
				HyenaSqliteCommand cmd = new HyenaSqliteCommand ("UPDATE photos SET roll_id = ? " +
							       "WHERE id = ? ", roll.Id, p.Id);
				App.Instance.Database.Database.Execute (cmd);
				p.RollId = roll.Id;
			}
			Log.Debug ("RetroactiveRoll done: " + photos.Length + " photos in roll " + roll.Id);
		}
	}
}
