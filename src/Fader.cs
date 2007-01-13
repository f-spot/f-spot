/*
 * Fader.cs
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
using Gtk;
using FSpot.Widgets;

namespace FSpot {
	public class Fader {
		bool composited;
		Delay fade_delay;
		TimeSpan duration;
		DateTime start;
		Gtk.Window win;
		double target_opacity;
		
		public Fader (Gtk.Window win, double target, int sec)
		{
			this.win = win;
			win.Mapped += HandleMapped;
			win.Unmapped += HandleUnmapped;
			win.ExposeEvent += HandleExposeEvent;
			duration = new TimeSpan (0, 0, sec);
			target_opacity = target;
		}
		
		[GLib.ConnectBefore]
		public void HandleMapped (object sender, EventArgs args)
		{
			composited = CompositeUtils.SupportsHint (win.Screen, "_NET_WM_WINDOW_OPACITY");
			if (!composited)
				return;
			
			CompositeUtils.SetWinOpacity (win, 0.0);
		}
		
		public void HandleExposeEvent (object sender, ExposeEventArgs args)
		{
			if (fade_delay == null) {
				fade_delay = new Delay (50, new GLib.IdleHandler (Update));
					start = DateTime.Now;
					fade_delay.Start ();
			}
		}
		
		public void HandleUnmapped (object sender, EventArgs args)
		{
			if (fade_delay != null)
				fade_delay.Stop ();
		}
		
		public bool Update ()
		{
			double percent = Math.Min ((DateTime.Now - start).Ticks / (double) duration.Ticks, 1.0);
			double opacity = Math.Sin (percent * Math.PI * 0.5) * target_opacity;
			CompositeUtils.SetWinOpacity (win, opacity);
			
			bool stop = opacity >= target_opacity;

			if (stop)
				fade_delay.Stop ();
			
			return !stop;
		}
	}
}
