//
// RetroactiveRoll.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot;
using FSpot.Core;
using FSpot.Database;
using FSpot.Extensions;

using Hyena;

using Hyena.Data.Sqlite;

namespace FSpot.Tools.RetroactiveRoll
{
	public class RetroactiveRoll: ICommand
	{
		public void Run (object o, EventArgs e)
		{
			var photos = App.Instance.Organizer.SelectedPhotos ();

			if (photos.Count == 0) {
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
			Log.Debug ("RetroactiveRoll done: " + photos.Count + " photos in roll " + roll.Id);
		}
	}
}
