// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Models
{
	public class OldMeta : IConvert
	{
		public long Id { get; set; }
		public string Name { get; set; }
		public string Data { get; set; }

		public object Convert ()
		{
			return new Meta {
				OldId = Id,
				Name = Name,
				Data = Data
			};
		}
	}
}
