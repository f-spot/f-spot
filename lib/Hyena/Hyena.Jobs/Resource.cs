//
// Resource.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Jobs
{
	public class Resource
	{
		// Convenience Resources for programs to use
		public static readonly Resource Cpu = new Resource { Id = "cpu", Name = "CPU" };
		public static readonly Resource Disk = new Resource { Id = "disk", Name = "Disk" };
		public static readonly Resource Database = new Resource { Id = "db", Name = "Database" };

		public string Id { get; set; }
		public string Name { get; set; }
	}
}
