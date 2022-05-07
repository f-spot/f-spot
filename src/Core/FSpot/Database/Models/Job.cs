// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace FSpot.Models
{
	public partial class Job : BaseDbSet
	{
		// TODO, Is the column Id being used as an index?
		//		 ie, the order in which the jobs should be run?
		[NotMapped]
		public long OldId { get; set; }
		public string JobType { get; set; }
		public string JobOptions { get; set; }
		public DateTime RunAt { get; set; }
		public long JobPriority { get; set; }

		[NotMapped]
		public bool Persistent { get; }

	}
}
