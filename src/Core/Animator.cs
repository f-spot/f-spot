/*
 * Delay.cs
 *
 * Copyright 2007 Novell Inc.
 * 
 * Author
 *   Larry Ewing <lewing@novell.com>
 *
 * See COPYING for license information.
 *
 */

using System;

namespace FSpot
{
	public class Animator
	{
		Delay delay;
		DateTime start;
		TimeSpan duration;
		float percent;
		EventHandler tick;
		bool run_first;

		public float Percent {
			get { return percent; }
		}

		public bool RunWhenStarted {
			get { return run_first; }
			set { run_first = value; }
		}

		public Animator (TimeSpan duration, TimeSpan interval, EventHandler tick)
		{
			this.duration = duration;
			this.tick = tick;
			delay = new Delay ((uint)interval.TotalMilliseconds, HandleTimeout);
		}
		
		public Animator (int duration_milli, int interval_milli, EventHandler tick) 
			: this (new TimeSpan (0, 0, 0, 0, duration_milli),
				new TimeSpan (0, 0, 0, 0, interval_milli),
				tick)
		{
		}
		
		public bool HandleTimeout ()
		{
			percent = (DateTime.Now - start).Ticks / (float) duration.Ticks;
			EventHandler tick_handler = tick;
			if (tick_handler != null)
				tick_handler (this, EventArgs.Empty);

			return delay.IsPending;
		}

		public void Start ()
		{
			Start (run_first);
		}

		public void Start (bool run_now)
		{
			start = DateTime.Now;

			if (run_now)
				HandleTimeout ();

			delay.Start ();
		}
		
		public void Stop ()
		{
			delay.Stop ();
		}
	}
}
