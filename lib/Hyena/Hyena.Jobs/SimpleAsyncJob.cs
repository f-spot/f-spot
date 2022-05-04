//
// SimpleAsyncJob.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

namespace Hyena.Jobs
{
	public abstract class SimpleAsyncJob : Job
	{
		Thread thread;

		public SimpleAsyncJob ()
		{
		}

		public SimpleAsyncJob (string name, PriorityHints hints, params Resource[] resources)
			: base (name, hints, resources)
		{
		}

		protected override void RunJob ()
		{
			if (thread == null) {
				thread = new Thread (InnerStart);
				thread.Name = string.Format ("Hyena.Jobs.JobRunner ({0})", Title);
				thread.Priority = this.Has (PriorityHints.SpeedSensitive) ? ThreadPriority.Normal : ThreadPriority.Lowest;
				thread.Start ();
			}
		}

		protected void AbortThread ()
		{
			if (thread != null) {
				thread.Abort ();
			}
		}

		void InnerStart ()
		{
			try {
				Run ();
			} catch (ThreadAbortException) {
			} catch (Exception e) {
				Log.Exception (e);
			}
		}

		protected abstract void Run ();
	}
}
