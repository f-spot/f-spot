// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace FSpot.Models
{
	public partial class Roll : BaseDbSet
	{
		[NotMapped]
		public long OldId { get; set; }
		public DateTime UtcTime { get; set; }
		public List<Photo> Photos { get; set; }
	}
}
