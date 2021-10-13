// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace FSpot.Models
{
	public partial class PhotoTag : BaseDbSet
	{
		public Photo Photo { get; set; }
		public Tag Tag { get; set; }

		public Guid PhotoId { get; set; }
		public Guid TagId { get; set; }

		[NotMapped]
		public long OldPhotoId { get; set; }

		[NotMapped]
		public long OldTagId { get; set; }
	}
}
