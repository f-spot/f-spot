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
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Banshee.Kernel;

using FSpot.Database.Jobs;
using FSpot.Jobs;
using FSpot.Utils;

using Hyena;

namespace FSpot.Database
{
	public class JobStore : DbStore<Job>
	{
		const string jobsTableName = "jobs";
		readonly TinyIoCContainer container;
		readonly FSpotContext Context;

		public JobStore (IDb db) : base (db)
		{
			Context = new FSpotContext ();
			// this should be replaced by a global registry as soon as
			// extensions may provide job implementations
			container = new TinyIoCContainer ();
			container.Register (Db);
			container.Register<Job, SyncMetadataJob> (SyncMetadataJob.JobName).AsMultiInstance ();
			container.Register<Job, CalculateHashJob> (CalculateHashJob.JobName).AsMultiInstance ();

			// Register legacy names of jobs as previous versions of f-spot saved the full job type
			// name in the database which prevents refactoring of type names and namespaces.
			// This also prevents us from intoducing a new database schema version including an
			// upgrade.
			container.Register<Job, SyncMetadataJob> ("FSpot.Database.Jobs.SyncMetadataJob").AsMultiInstance ();
			container.Register<Job, SyncMetadataJob> ("FSpot.Jobs.SyncMetadataJob").AsMultiInstance ();
			container.Register<Job, CalculateHashJob> ("FSpot.Database.Jobs.CalculateHashJob").AsMultiInstance ();
			container.Register<Job, CalculateHashJob> ("FSpot.Jobs.CalculateHashJob").AsMultiInstance ();

			LoadAllItems ();
		}

		Job CreateJob (string type, uint id, string options, DateTime runAt, JobPriority priority)
		{
			using (var childContainer = container.GetChildContainer ()) {
				childContainer.Register (new JobData {
					Id = id,
					JobOptions = options,
					JobPriority = priority,
					RunAt = runAt,
					Persistent = true
				});
				childContainer.TryResolve<Job> (type, out var job);
				if (job == null) {
					Log.Error ($"Unknown job type {type} ignored.");
				}
				return job;
			}
		}

		void LoadAllItems ()
		{
			var jobs = Context.Jobs;

			Scheduler.Suspend ();
			foreach (var job in jobs) {
				job.Finished += HandleRemoveJob;
				Scheduler.Schedule (job, job.JobPriority);
				job.Status = JobStatus.Scheduled;
			}
		}

		public Job CreatePersistent (string job_type, string job_options)
		{
			var run_at = DateTime.Now;
			var job_priority = JobPriority.Lowest;
			var id = Database.Execute (new HyenaSqliteCommand ($"INSERT INTO {jobsTableName} (job_type, job_options, run_at, job_priority) VALUES (?, ?, ?, ?)",
				job_type,
				job_options,
				DateTimeUtil.FromDateTime (run_at),
				Convert.ToInt32 (job_priority)));

			Job job = CreateJob (job_type, (uint)id, job_options, run_at, job_priority);

			AddToCache (job);
			job.Finished += HandleRemoveJob;
			Scheduler.Schedule (job, job.JobPriority);
			job.Status = JobStatus.Scheduled;
			EmitAdded (job);

			return job;
		}

		public override void Commit (Job item)
		{
			if (item.Persistent)
				Database.Execute (new HyenaSqliteCommand (
					$"UPDATE {jobsTableName} " +
					"  SET job_type = ? " +
					"  SET job_options = ? " +
					"  SET run_at = ? " +
					"  SET job_priority = ? " +
					"  WHERE id = ?",
					"Empty", //FIXME
					item.JobOptions,
					DateTimeUtil.FromDateTime (item.RunAt),
					item.JobPriority,
					item.Id));

			EmitChanged (item);
		}

		public override Job Get (Guid id)
		{
			// we never use this
			return Context.Jobs.Find (id);
		}

		public override void Remove (Job item)
		{
			if (item.Persistent) {
				Context.Remove (item);
				Context.SaveChanges ();
			}

			EmitRemoved (item);
		}

		public void HandleRemoveJob (Object o, EventArgs e)
		{
			Remove (o as Job);
		}
	}
}
