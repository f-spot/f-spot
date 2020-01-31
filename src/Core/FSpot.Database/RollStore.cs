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
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;

using FSpot.Models;

namespace FSpot.Database
{
	public class RollStore : DbStore<Roll>
	{
		FSpotContext Context;

		public RollStore (IDb db) : base (db)
		{
			Context = new FSpotContext ();
		}

		public Roll Create () => Create (DateTime.UtcNow);

		public Roll Create (DateTime timeInUtc)
		{
			var roll = new Roll { UtcTime = timeInUtc };
			Context.Add (roll);
			Context.SaveChanges ();

			return roll;
		}

		public override Roll Get (Guid id)
		{
			return Context.Rolls.Find (id);
		}

		public override void Remove (Roll item)
		{
			Context.Remove (item);
			Context.SaveChanges ();
		}

		public override void Commit (Roll item)
		{
			// Nothing to do here, since all the properties of a roll are immutable.
			// Ugh? With 42 references something might be off?
		}

		public uint PhotosInRoll (Roll roll)
		{
			var count = Context.Photos.Where (x => x.RollId == roll.Id).Count ();

			return (uint)count;
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

			using (Hyena.Data.Sqlite.IDataReader reader = Database.Query (query)) {
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
