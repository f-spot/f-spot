// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Models
{
	public class OldPhotoVersion : IConvert
	{
		public long PhotoId { get; set; }
		public long VersionId { get; set; }
		public string Name { get; set; }
		public string BaseUri { get; set; }
		public string Filename { get; set; }
		public string ImportMd5 { get; set; }
		public bool Protected { get; set; }

		public object Convert ()
		{
			return new PhotoVersion {
				OldPhotoId = PhotoId,
				VersionId = VersionId,
				Name = Name,
				BaseUri = BaseUri,
				Filename = Filename,
				ImportMd5 = ImportMd5,
				Protected = Protected
			};
		}
	}
}
