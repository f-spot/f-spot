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

namespace FSpot {
	public class Animator {
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
			if (tick != null)
				tick (this, EventArgs.Empty);

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

	public class Delay {
		public Delay (uint interval, GLib.IdleHandler op)
		{
			this.op += op;
			this.interval = interval;
		}

		public Delay (GLib.IdleHandler op) 
		{
			this.op += op;
			this.interval = 0;
		}

		uint source;
		uint interval;

		private event GLib.IdleHandler op;
		private Gtk.Object obj;

		private bool HandleOperation ()
		{
			lock (this) {
				bool runagain = op ();
				if (!runagain)
					source = 0;

				return runagain;
			}
		}
		
		public void Start () {
			lock (this) {
				if (this.IsPending)
					return;

				if (interval != 0) 
					source = GLib.Timeout.Add (interval, new GLib.TimeoutHandler (HandleOperation));
				else 
					source = GLib.Idle.Add (new GLib.IdleHandler (HandleOperation));
			}
		}

		public bool IsPending {
			get {
				return source != 0;
			}
		}

		public void Connect (Gtk.Object obj)
		{
			obj.Destroyed += HandleDestroy; 
		}	     
		
		private void HandleDestroy (object sender, System.EventArgs args)
		{
			this.Stop ();
		}
		
		public void Stop () 
		{
			lock (this) {
				if (this.IsPending) {
					GLib.Source.Remove (source);
					source = 0;
				}
			}
		}
	}
}
