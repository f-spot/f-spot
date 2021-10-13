// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace FSpot.Models
{
	public partial class Export : BaseDbSet
	{
		[NotMapped]
		public long OldId { get; set; }
		[NotMapped]
		public long OldImageId { get; set; }
		public Guid ImageId { get; set; }
		public long ImageVersionId { get; set; }
		public string ExportType { get; set; }
		public string ExportToken { get; set; }
	}
}
