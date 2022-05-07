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

namespace FSpot.Jobs
{
	public abstract class Job : Models.Job, IJob
	{
		protected Job (JobData jobData)
		{
			JobOptions = jobData.JobOptions;
			JobPriority = jobData.JobPriority;
			RunAt = jobData.RunAt;
			Persistent = jobData.Persistent;
		}

		public string JobOptions { get; }
		internal JobPriority JobPriority { get; }
		//Not in use yet !
		public DateTime RunAt { get; }
		public bool Persistent { get; }

		public event EventHandler Finished;

		JobStatus status;
		public JobStatus Status {
			get => status;
			set {
				status = value;
				switch (value) {
				case JobStatus.Finished:
				case JobStatus.Failed:
					Finished?.Invoke (this, new EventArgs ());
					break;
				}
			}
		}

		public void Run ()
		{
			Status = JobStatus.Running;
			Status = Execute () ? JobStatus.Finished : JobStatus.Failed;
		}

		protected abstract bool Execute ();
	}
}