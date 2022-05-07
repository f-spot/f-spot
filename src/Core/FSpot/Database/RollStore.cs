// Copyright (C) 2003-2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
// Copyright (C) 2003 Ettore Perazzoli
// Copyright (C) 2007-2008 Stephane Delcroix
// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using FSpot.Models;

namespace FSpot.Database
{
	public class RollStore : DbStore<Roll>
	{
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

		public int PhotosInRoll (Roll roll)
		{
			var count = Context.Photos.Count (x => x.Roll == roll);
			return count;
		}

		public List<Roll> GetRolls (int limit = -1)
		{
			var rolls = new List<Roll> ();

			rolls.AddRange ((limit == -1)
				? Context.Rolls.Where (x => x.Photos.Any ())
				: Context.Rolls.Where (x => x.Photos.Any ()).Take (limit));

			return rolls;
		}
	}
}
