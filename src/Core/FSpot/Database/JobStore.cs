//
// JobStore.cs
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

using FSpot.Jobs;
using FSpot.Models;

using TinyIoC;

namespace FSpot.Database
{
	public class JobStore : DbStore<Models.Job>
	{
		readonly TinyIoCContainer container;

		public JobStore ()
		{
			// this should be replaced by a global registry as soon as
			// extensions may provide job implementations
			container = new TinyIoCContainer ();
			container.Register<Jobs.Job, SyncMetadataJob> (SyncMetadataJob.JobName).AsMultiInstance ();
			container.Register<Jobs.Job, CalculateHashJob> (CalculateHashJob.JobName).AsMultiInstance ();

			LoadAllItems ();
		}

		Jobs.Job CreateJob (string type, string options, DateTime runAt, JobPriority priority)
		{
			using var childContainer = container.GetChildContainer ();

			childContainer.Register (new JobData {
				JobOptions = options,
				JobPriority = priority,
				RunAt = runAt,
				Persistent = true
			});

			childContainer.TryResolve<Jobs.Job> (type, out var job);
			if (job == null) {
				Logger.Log.Error ($"Unknown job type {type} ignored.");
			}
			return job;
		}

		void LoadAllItems ()
		{
			var jobs = Context.Jobs;

			Scheduler.Suspend ();
			//foreach (var job in jobs) {
			//	job.Finished += HandleRemoveJob;
			//	Scheduler.Schedule (job, job.JobPriority);
			//	job.Status = JobStatus.Scheduled;
			//}
		}

		public Models.Job CreatePersistent (string jobType, string jobOptions)
		{
			var job = new Models.Job {
				JobType = jobType,
				JobOptions = jobOptions,
				RunAt = DateTime.Now,
				JobPriority = (long)JobPriority.Lowest
			};
			Context.Jobs.Add (job);
			Context.SaveChanges ();

			//AddToCache (job);
			//job.Finished += HandleRemoveJob;
			//Scheduler.Schedule (job, job.JobPriority);
			//job.Status = JobStatus.Scheduled;

			EmitAdded (job);

			return job;
		}

		public override void Commit (Models.Job item)
		{
			//if (item.Persistent)
			//Database.Execute (new HyenaSqliteCommand (
			//	$"UPDATE {jobsTableName} " +
			//	"  SET job_type = ? " +
			//	"  SET job_options = ? " +
			//	"  SET run_at = ? " +
			//	"  SET job_priority = ? " +
			//	"  WHERE id = ?",
			//	"Empty", //FIXME
			//	item.JobOptions,
			//	DateTimeUtil.FromDateTime (item.RunAt),
			//	item.JobPriority,
			//	item.Id));

			if (item == null)
				throw new NullReferenceException (nameof (item));

			if (item.Persistent)
				Context.Jobs.Update (item);

			EmitChanged (item);
		}

		public override Models.Job Get (Guid id)
		{
			// we never use this
			return Context.Jobs.Find (id);
		}

		public override void Remove (Models.Job item)
		{
			if (item == null)
				throw new NullReferenceException (nameof (item));

			if (item.Persistent) {
				Context.Remove (item);
				Context.SaveChanges ();
			}

			EmitRemoved (item);
		}

		void HandleRemoveJob (object o, EventArgs e)
		{
			Remove (o as Jobs.Job);
		}
	}
}
