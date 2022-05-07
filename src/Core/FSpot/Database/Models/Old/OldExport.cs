// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Models
{
	public class OldExport : IConvert
	{
		public long Id { get; set; }
		public long ImageId { get; set; }
		public long ImageVersionId { get; set; }
		public string ExportType { get; set; }
		public string ExportToken { get; set; }

		public object Convert ()
		{
			return new Export {
				OldId = Id,
				OldImageId = ImageId,
				ImageVersionId = ImageVersionId,
				ExportType = ExportType,
				ExportToken = ExportToken
			};
		}
	}
}
