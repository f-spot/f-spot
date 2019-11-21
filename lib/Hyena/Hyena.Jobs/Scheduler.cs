//
// Scheduler.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Hyena;

namespace Hyena.Jobs
{
    public class Scheduler
    {
        private List<Job> jobs;

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
                        throw new ArgumentException ("Job not schedulable", "job");
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

            Action<Job> handler = JobAdded;
            if (handler != null) {
                handler (job);
            }
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
                List<Job> jobs_copy = new List<Job> (jobs);
                foreach (var job in jobs_copy) {
                    if (evenDataLossJobs || !job.Has (PriorityHints.DataLossIfStopped)) {
                        job.Cancel ();
                    }
                }
            }
        }

        private void OnJobFinished (object o, EventArgs args)
        {
            Job job = o as Job;

            lock (jobs) {
                jobs.Remove (job);
            }

            Action<Job> handler = JobRemoved;
            if (handler != null) {
                handler (job);
            }

            Schedule ();
        }

        private void Schedule ()
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

        private bool IsRunning (Job job)
        {
            return job.IsRunning;
        }

        private bool CanStart (Job job)
        {
            return CanStartJob (job, false);
        }

        private bool CanStartJob (Job job, bool pausedJob)
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

        private void StartJob (Job job)
        {
            ConflictingJobs (job).ForEach (PreemptJob);
            job.Start ();
        }

        private void PreemptJob (Job job)
        {
            job.Preempt ();
        }

        private IEnumerable<Job> ConflictingJobs (Job job)
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
