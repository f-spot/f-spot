//
// Job.cs
//
// Author:
//   Daniel Köb <dkoeb@peony.at>
//
// Copyright (C) 2018 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using Banshee.Kernel;

namespace FSpot.Database.Jobs
{
	public class JobData
	{
		public IDb Db { get; set; }
		public uint Id { get; set; }
		public string JobOptions { get; set; }
		public JobPriority JobPriority { get; set; }
		public DateTime RunAt { get; set; }
		public bool Persistent { get; set; }
	}
}