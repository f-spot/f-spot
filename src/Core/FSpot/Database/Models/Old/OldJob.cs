// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace FSpot.Models
{
	public class OldJob : IConvert
	{
		public long Id { get; set; }
		public string JobType { get; set; }
		public string JobOptions { get; set; }
		public long? RunAt { get; set; }
		public long JobPriority { get; set; }

		public object Convert ()
		{
			long runAt = RunAt ??= new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();

			return new Job {
				OldId = Id,
				JobType = JobType,
				JobOptions = JobOptions,
				RunAt = DateTimeOffset.FromUnixTimeSeconds (runAt).DateTime,
				JobPriority = JobPriority
			};
		}
	}
}
