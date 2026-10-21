// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace FSpot.Jobs
{
	public interface IJob
	{
		void Run ();
	}

	public enum JobPriority
	{
		Lowest,
		BelowNormal,
		Normal,
		AboveNormal,
		Highest
	}

	// One background thread and a priority queue. The thread is a background
	// thread so a queued job does not keep the process alive after the UI exits.
	public static class Scheduler
	{
		static readonly object gate = new object ();
		static readonly PriorityQueue<IJob, int> queue = new PriorityQueue<IJob, int> ();
		static Thread worker;
		static int suspendCount;

		public static void Schedule (IJob job, JobPriority priority)
		{
			if (job == null)
				throw new ArgumentNullException (nameof (job));

			lock (gate) {
				// PriorityQueue dequeues the smallest number first.
				queue.Enqueue (job, -(int)priority);
				if (worker == null || !worker.IsAlive) {
					worker = new Thread (Process) {
						IsBackground = true,
						Name = "F-Spot jobs"
					};
					worker.Start ();
				} else {
					Monitor.Pulse (gate);
				}
			}
		}

		public static void Suspend ()
		{
			lock (gate)
				suspendCount++;
		}

		public static void Resume ()
		{
			lock (gate) {
				if (suspendCount > 0)
					suspendCount--;
				Monitor.Pulse (gate);
			}
		}

		static void Process ()
		{
			while (true) {
				IJob job;
				lock (gate) {
					while (suspendCount > 0 || queue.Count == 0) {
						if (suspendCount == 0 && queue.Count == 0) {
							worker = null;
							return;
						}
						Monitor.Wait (gate);
					}

					job = queue.Dequeue ();
				}

				try {
					job.Run ();
				} catch (Exception ex) {
					Logger.Log.Debug (ex, "Job threw an unhandled exception");
				}
			}
		}
	}
}
