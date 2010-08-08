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

namespace FSpot.Core
{
	public class Delay
	{
		object syncHandle = new object ();

		public Delay (uint interval, GLib.IdleHandler op)
		{
			this.op = op;
			this.interval = interval;
		}

		public Delay (GLib.IdleHandler op)
		{
			this.op = op;
		}

		uint source;
		uint interval;

		private GLib.IdleHandler op;

		private bool HandleOperation ()
		{
			lock (syncHandle) {
				bool runagain = op ();
				if (!runagain)
					source = 0;

				return runagain;
			}
		}

		public void Start () {
			lock (syncHandle) {
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
			if (obj == null)
				throw new ArgumentNullException ("obj");
			obj.Destroyed += HandleDestroy;
		}

		private void HandleDestroy (object sender, System.EventArgs args)
		{
			this.Stop ();
		}

		public void Stop ()
		{
			lock (syncHandle) {
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
