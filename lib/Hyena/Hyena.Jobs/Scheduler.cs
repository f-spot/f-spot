//
// Scheduler.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Hyena.Jobs
{
	public class Scheduler
	{
		List<Job> jobs;

		public event Action<Job> JobAdded;
		public event Action<Job> JobRemoved;

		public IEnumerable<Job> Jobs { get; private set; }

		public int JobCount {
			get { lock (jobs) { return jobs.Count; } }
		}

		public bool HasAnyDataLossJobs {
			get {
				lock (jobs) {
					return jobs.With (PriorityHints.DataLossIfStopped).Any ();
				}
			}
		}

		public Scheduler ()
		{
			jobs = new List<Job> ();
			Jobs = new ReadOnlyCollection<Job> (jobs);
		}

		public void Add (Job job)
		{
			lock (jobs) {
				lock (job) {
					if (jobs.Contains (job) || job.HasScheduler) {
						throw new ArgumentException ("Job not schedulable", nameof (job));
					}

					job.HasScheduler = true;
				}

				jobs.Add (job);
				job.State = JobState.Scheduled;
				job.Finished += OnJobFinished;

				if (CanStart (job)) {
					StartJob (job);
				}
			}

			JobAdded?.Invoke (job);
		}

		public void Cancel (Job job)
		{
			lock (jobs) {
				if (jobs.Contains (job)) {
					// Cancel will call OnJobFinished which will call Schedule
					job.Cancel ();
				}
			}
		}

		public void Pause (Job job)
		{
			lock (jobs) {
				if (jobs.Contains (job)) {
					if (job.Pause ()) {
						// See if any scheduled jobs can now be started
						Schedule ();
					}
				}
			}
		}

		public void Resume (Job job)
		{
			lock (jobs) {
				if (jobs.Contains (job) && CanStartJob (job, true)) {
					StartJob (job);
				}
			}
		}

		public void CancelAll (bool evenDataLossJobs)
		{
			lock (jobs) {
				var jobs_copy = new List<Job> (jobs);
				foreach (var job in jobs_copy) {
					if (evenDataLossJobs || !job.Has (PriorityHints.DataLossIfStopped)) {
						job.Cancel ();
					}
				}
			}
		}

		void OnJobFinished (object o, EventArgs args)
		{
			var job = o as Job;

			lock (jobs) {
				jobs.Remove (job);
			}

			JobRemoved?.Invoke (job);

			Schedule ();
		}

		void Schedule ()
		{
			lock (jobs) {
				// First try to start any non-LongRunning jobs
				jobs.Without (PriorityHints.LongRunning)
					.Where (CanStart)
					.ForEach (StartJob);

				// Then start any LongRunning ones
				jobs.With (PriorityHints.LongRunning)
					.Where (CanStart)
					.ForEach (StartJob);
			}
		}

		#region Job Query helpers

		bool IsRunning (Job job)
		{
			return job.IsRunning;
		}

		bool CanStart (Job job)
		{
			return CanStartJob (job, false);
		}

		bool CanStartJob (Job job, bool pausedJob)
		{
			if (!job.IsScheduled && !(pausedJob && job.IsPaused))
				return false;

			if (job.Has (PriorityHints.SpeedSensitive))
				return true;

			// Run only one non-SpeedSensitive job that uses a given Resource
			if (job.Has (PriorityHints.LongRunning))
				return jobs.Where (IsRunning)
						   .SharingResourceWith (job)
						   .Any () == false;

			// With the exception that non-LongRunning jobs will preempt LongRunning ones
			return jobs.Where (IsRunning)
					   .Without (PriorityHints.LongRunning)
					   .SharingResourceWith (job)
					   .Any () == false;
		}

		void StartJob (Job job)
		{
			ConflictingJobs (job).ForEach (PreemptJob);
			job.Start ();
		}

		void PreemptJob (Job job)
		{
			job.Preempt ();
		}

		IEnumerable<Job> ConflictingJobs (Job job)
		{
			if (job.Has (PriorityHints.SpeedSensitive)) {
				// Preempt non-SpeedSensitive jobs that use the same Resource(s)
				return jobs.Where (IsRunning)
						   .Without (PriorityHints.SpeedSensitive)
						   .SharingResourceWith (job);
			} else if (!job.Has (PriorityHints.LongRunning)) {
				// Preempt any LongRunning jobs that use the same Resource(s)
				return jobs.Where (IsRunning)
						   .With (PriorityHints.LongRunning)
						   .SharingResourceWith (job);
			}

			return Enumerable.Empty<Job> ();
		}

		#endregion

	}
}
