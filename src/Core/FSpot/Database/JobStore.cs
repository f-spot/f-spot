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

//using Banshee.Kernel;

using Hyena;

using FSpot.Jobs;
using FSpot.Utils;

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

		Job CreateJob (string type, string options, DateTime runAt, JobPriority priority)
		{
			using var childContainer = container.GetChildContainer ();

			childContainer.Register (new JobData {
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
				throw new NullReferenceException(nameof(item));

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
				throw new NullReferenceException(nameof(item));

			if (item.Persistent) {
				Context.Remove (item);
				Context.SaveChanges ();
			}

			EmitRemoved (item);
		}

		void HandleRemoveJob (object o, EventArgs e)
		{
			Remove (o as Job);
		}
	}
}
