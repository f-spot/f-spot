// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;

using FSpot.Jobs;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests.JobScheduler
{
	[TestClass]
	public class JobRunnerTests
	{
		public TestContext TestContext { get; set; }

		[TestMethod]
		public void Run_HighestPriorityFirst ()
		{
			var order = new List<string> ();
			using var done = new CountdownEvent (2);
			Scheduler.Suspend ();
			try {
				Scheduler.Schedule (new NamedJob ("low", order, done), JobPriority.Lowest);
				Scheduler.Schedule (new NamedJob ("high", order, done), JobPriority.Highest);
			} finally {
				Scheduler.Resume ();
			}

			Assert.IsTrue (done.Wait (TimeSpan.FromSeconds (2), TestContext.CancellationToken));
			Assert.AreEqual ("high", order[0]);
			Assert.AreEqual ("low", order[1]);
		}

		[TestMethod]
		public void Suspend_HoldsQueuedJobsUntilResume ()
		{
			var order = new List<string> ();
			using var done = new CountdownEvent (1);
			Scheduler.Suspend ();
			try {
				Scheduler.Schedule (new NamedJob ("held", order, done), JobPriority.Normal);
				Assert.IsFalse (done.Wait (TimeSpan.FromMilliseconds (50), TestContext.CancellationToken));
				Assert.IsEmpty (order);
			} finally {
				Scheduler.Resume ();
			}

			Assert.IsTrue (done.Wait (TimeSpan.FromSeconds (2), TestContext.CancellationToken));
			Assert.AreEqual ("held", order[0]);
		}

		[TestMethod]
		public void Run_AThrowingJobDoesNotStopTheNextJob ()
		{
			var order = new List<string> ();
			using var done = new CountdownEvent (1);
			Scheduler.Suspend ();
			try {
				Scheduler.Schedule (new ThrowingJob (), JobPriority.Highest);
				Scheduler.Schedule (new NamedJob ("after", order, done), JobPriority.Lowest);
			} finally {
				Scheduler.Resume ();
			}

			Assert.IsTrue (done.Wait (TimeSpan.FromSeconds (2), TestContext.CancellationToken));
			Assert.AreEqual ("after", order[0]);
		}

		sealed class NamedJob : IJob
		{
			readonly string name;
			readonly List<string> order;
			readonly CountdownEvent done;

			public NamedJob (string name, List<string> order, CountdownEvent done)
			{
				this.name = name;
				this.order = order;
				this.done = done;
			}

			public void Run ()
			{
				lock (order)
					order.Add (name);
				done.Signal ();
			}
		}

		sealed class ThrowingJob : IJob
		{
			public void Run ()
				=> throw new InvalidOperationException ("job failed");
		}
	}
}
