//
// Job.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
// Copyright (C) 2007-2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Banshee.Kernel;

using FSpot.Core;
using FSpot.Jobs;

namespace FSpot.Database.Jobs
{
	public abstract class Job : DbItem, IJob
	{
		public Job (IDb db, JobData jobData) : base (jobData.Id)
		{
			JobOptions = jobData.JobOptions;
			JobPriority = jobData.JobPriority;
			RunAt = jobData.RunAt;
			Persistent = jobData.Persistent;
			Db = db;
		}

		public string JobOptions { get; private set; }
		internal JobPriority JobPriority { get; private set; }
		//Not in use yet !
		public DateTime RunAt { get; private set; }
		public bool Persistent { get; private set; }
		protected IDb Db { get; private set; }

		public event EventHandler Finished;

		private JobStatus status;
		public JobStatus Status {
			get { return status; }
			set {
				status = value;
				switch (value) {
				case JobStatus.Finished:
				case JobStatus.Failed:
					if (Finished != null)
						Finished (this, new EventArgs ());
					break;
				default:
					break;
				}
			}
		}

		public void Run ()
		{
			Status = JobStatus.Running;
			if (Execute ())
				Status = JobStatus.Finished;
			else
				Status = JobStatus.Failed;
		}

		protected abstract bool Execute ();
	}
}