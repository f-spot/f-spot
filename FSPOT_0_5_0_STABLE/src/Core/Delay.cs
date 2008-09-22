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
	public class Delay
	{
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

		public void Restart ()
		{
			Stop ();
			Start ();
		}
	}
}
