//
// JobStatus.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2007 Novell, Inc.
// Copyright (C) 2007 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Jobs
{
	public enum JobStatus
	{
		Created,
		Scheduled,
		Running,
		Finished,
		Failed
	}
}
