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
using System.Linq;

using FSpot.Database;
using FSpot.Extensions;
using FSpot.Models;

namespace FSpot.Tools.RetroactiveRoll
{
	public class RetroactiveRoll : ICommand
	{
		public void Run (object o, EventArgs e)
		{
			var photos = App.Instance.Organizer.SelectedPhotos ();

			if (photos.Length == 0) {
				Logger.Log.Debug ("no photos selected, returning");
				return;
			}

			DateTime import_time = photos[0].UtcTime;
			foreach (Photo p in photos)
				if (p.UtcTime > import_time)
					import_time = p.UtcTime;

			RollStore rolls = App.Instance.Database.Rolls;
			Roll roll = rolls.Create (import_time);

			var context = new FSpotContext ();
			foreach (Photo p in photos) {
				var photo = context.Photos.First (x => x.Id == p.Id);
				photo.RollId = roll.Id;
				context.Photos.Update (photo);
				p.RollId = roll.Id;
			}
			context.SaveChanges ();
			Logger.Log.Debug ($"RetroactiveRoll done: {photos.Length} photos in roll {roll.Id}");
		}
	}
}
