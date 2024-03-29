//
// SchedulerTests.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

using NUnit.Framework;

namespace Hyena.Jobs
{
	[TestFixture]
	public class SchedulerTests
	{
		Scheduler scheduler;

		[SetUp]
		public void Setup ()
		{
			//Log.Debugging = true;
			TestJob.job_count = 0;
			Log.Debug ("New job scheduler test");
		}

		[TearDown]
		public void TearDown ()
		{
			if (scheduler != null) {
				// Ensure the scheduler's jobs are all finished, otherwise
				// their job threads will be killed, throwing an exception
				while (scheduler.JobCount > 0) ;
			}

			//Log.Debugging = false;
		}

		[Test]
		public void TestSimultaneousSpeedJobs ()
		{
			scheduler = new Scheduler ();
			scheduler.Add (new TestJob (200, PriorityHints.SpeedSensitive, Resource.Cpu, Resource.Disk));
			scheduler.Add (new TestJob (200, PriorityHints.SpeedSensitive, Resource.Cpu, Resource.Disk));
			scheduler.Add (new TestJob (200, PriorityHints.None, Resource.Cpu, Resource.Disk));

			// Test that two SpeedSensitive jobs with the same Resources will run simultaneously
			AssertJobsRunning (2);

			// but that the third that isn't SpeedSensitive won't run until they are both done
			while (scheduler.JobCount > 1) ;
			Assert.AreEqual (PriorityHints.None, scheduler.Jobs.First ().PriorityHints);
		}

		[Test]
		public void TestOneNonSpeedJobPerResource ()
		{
			// Test that two SpeedSensitive jobs with the same Resources will run simultaneously
			scheduler = new Scheduler ();
			scheduler.Add (new TestJob (200, PriorityHints.None, Resource.Cpu, Resource.Disk));
			scheduler.Add (new TestJob (200, PriorityHints.None, Resource.Cpu, Resource.Disk));
			AssertJobsRunning (1);
		}

		[Test]
		public void TestSpeedJobPreemptsNonSpeedJobs ()
		{
			scheduler = new Scheduler ();
			var a = new TestJob (200, PriorityHints.None, Resource.Cpu);
			var b = new TestJob (200, PriorityHints.None, Resource.Disk);
			var c = new TestJob (200, PriorityHints.LongRunning, Resource.Database);
			scheduler.Add (a);
			scheduler.Add (b);
			scheduler.Add (c);

			// Test that three jobs got started
			AssertJobsRunning (3);

			scheduler.Add (new TestJob (200, PriorityHints.SpeedSensitive, Resource.Cpu, Resource.Disk));

			// Make sure the SpeedSensitive jobs has caused the Cpu and Disk jobs to be paused
			AssertJobsRunning (2);
			Assert.AreEqual (true, a.IsScheduled);
			Assert.AreEqual (true, b.IsScheduled);
			Assert.AreEqual (true, c.IsRunning);
		}

		/*[Test]
        public void TestManyJobs ()
        {
            var timer = System.Diagnostics.Stopwatch.StartNew ();
            scheduler = new Scheduler ("TestManyJobs");

            // First add some long running jobs
            for (int i = 0; i < 100; i++) {
                scheduler.Add (new TestJob (20, PriorityHints.LongRunning, Resource.Cpu));
            }

            // Then add some normal jobs that will prempt them
            for (int i = 0; i < 100; i++) {
                scheduler.Add (new TestJob (10, PriorityHints.None, Resource.Cpu));
            }

            // Then add some SpeedSensitive jobs that will prempt all of them
            for (int i = 0; i < 100; i++) {
                scheduler.Add (new TestJob (5, PriorityHints.SpeedSensitive, Resource.Cpu));
            }

            while (scheduler.Jobs.Count > 0);
            Log.DebugFormat ("Took {0} to schedule and process all jobs", timer.Elapsed);
            //scheduler.StopAll ();
        }*/

		/*[Test]
        public void TestCannotDisposeWhileDatalossJobsScheduled ()
        {
            scheduler = new Scheduler ();
            TestJob loss_job;
            scheduler.Add (new TestJob (200, PriorityHints.SpeedSensitive, Resource.Cpu));
            scheduler.Add (loss_job = new TestJob (200, PriorityHints.DataLossIfStopped, Resource.Cpu));

            AssertJobsRunning (1);
            Assert.AreEqual (false, scheduler.JobInfo[loss_job].IsRunning);

            try {
                //scheduler.StopAll ();
                Assert.Fail ("Cannot stop with dataloss job scheduled");
            } catch {
            }
        }

        public void TestCannotDisposeWhileDatalossJobsRunning ()
        {
            scheduler = new Scheduler ();
            scheduler.Add (new TestJob (200, PriorityHints.DataLossIfStopped, Resource.Cpu));
            AssertJobsRunning (1);

            try {
                //scheduler.StopAll ();
                Assert.Fail ("Cannot stop with dataloss job running");
            } catch {
            }
        }*/

		void AssertJobsRunning (int count)
		{
			Assert.AreEqual (count, scheduler.Jobs.Count (j => j.IsRunning));
		}

		class TestJob : SimpleAsyncJob
		{
			internal static int job_count;
			int iteration;
			int sleep_time;

			public TestJob (int sleep_time, PriorityHints hints, params Resource[] resources)
				: base (string.Format ("{0} ( {1}, {2})", job_count++, hints, resources.Aggregate ("", (a, b) => a += b.Id + " ")),
						hints,
						resources)
			{
				this.sleep_time = sleep_time;
			}

			protected override void Run ()
			{
				for (int i = 0; !IsCancelRequested && i < 2; i++) {
					YieldToScheduler ();
					Hyena.Log.DebugFormat ("{0} iteration {1}", Title, iteration++);
					System.Threading.Thread.Sleep (sleep_time);
				}

				OnFinished ();
			}
		}
	}
}
