// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace FSpot
{
	public static class ImportRollReassign
	{
		// The roll date is the newest photo. An equal time keeps the earlier
		// photo's time, matching the GTK walk that replaces only on '>'.
		public static bool TryAssign (IReadOnlyList<Photo> photos, Func<DateTime, uint> createRoll)
		{
			if (photos == null || photos.Count == 0)
				return false;

			DateTime newest = photos[0].Time;
			for (int i = 1; i < photos.Count; i++) {
				if (photos[i].Time > newest)
					newest = photos[i].Time;
			}

			uint rollId = createRoll (newest);
			foreach (Photo photo in photos)
				photo.RollId = rollId;
			return true;
		}
	}
}
