// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace FSpot.Models
{
	public class OldPhoto : IConvert
	{
		public long Id { get; set; }
		public long Time { get; set; }
		public string BaseUri { get; set; }
		public string Filename { get; set; }
		public string Description { get; set; }
		public long RollId { get; set; }
		public long DefaultVersionId { get; set; }
		public long Rating { get; set; }

		public object Convert ()
		{
			// TODO, Post migration fixups
			return new Photo {
				OldId = Id,
				UtcTime = DateTimeOffset.FromUnixTimeSeconds (Time).UtcDateTime,
				BaseUri = BaseUri,
				Filename = Filename,
				Description = Description,
				//RollId = rollIdLookup[RollId],
				OldRollId = RollId,
				DefaultVersionId = DefaultVersionId,
				Rating = Rating
			};
		}
	}
}
