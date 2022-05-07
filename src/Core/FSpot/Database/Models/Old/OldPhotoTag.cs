// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Models
{
	public class OldPhotoTag : IConvert
	{
		public long PhotoId { get; set; }
		public long TagId { get; set; }

		public object Convert ()
		{
			return new PhotoTag {
				OldPhotoId = PhotoId,
				OldTagId = TagId
			};
		}
	}
}
