/*
 * Jobs/JobStatus.cs
 *
 * Author(s)
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details
 */

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
