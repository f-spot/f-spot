// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations.Schema;

namespace FSpot.Models
{
	public partial class Meta : BaseDbSet
	{
		[NotMapped]
		public long OldId { get; set; }
		public string Name { get; set; }
		public string Data { get; set; }
	}
}
