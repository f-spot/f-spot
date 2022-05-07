// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace FSpot.Models
{
	public class OldRoll : IConvert
	{
		public long Id { get; set; }
		public long Time { get; set; }

		public object Convert ()
		{
			return new Roll {
				OldId = Id,
				UtcTime = DateTimeOffset.FromUnixTimeSeconds (Time).UtcDateTime
			};
		}
	}
}
