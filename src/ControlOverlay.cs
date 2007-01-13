/*
 * ControlBox.cs
 *
 * Copyright 2007 Novell Inc.
 *
 * Author
 *   Larry Ewing <lewing@novell.com>
 *
 * See COPYING for license information.
 */

using System;
using Gtk;
using FSpot.Widgets;
using Cairo;

namespace FSpot {
	public class ControlOverlay : Window {
		Widget host;
		Window host_toplevel;
		bool composited;
		VisibilityType visibility;

		public enum VisibilityType
		{
			None,
			Partial,
			Full
		}

		public VisibilityType Visibility {
			get { return visibility; }
			set {
				visibility = value;
				switch (visibility) {
				case VisibilityType.None:
					this.Hide ();
					break;
				case VisibilityType.Partial:
					CompositeUtils.SetWinOpacity (this, 0.3);
					this.Show ();
					CompositeUtils.SetWinOpacity (this, 0.3);
					break;
				case VisibilityType.Full:
					CompositeUtils.SetWinOpacity (this, .5);
					this.Show ();
					CompositeUtils.SetWinOpacity (this, .5);
					break;
				}
			}

		}

		public ControlOverlay (Gtk.Widget host) : base (WindowType.Popup)
		{
			this.host = host;
			Decorated = false;
			DestroyWithParent = true;
			
			host_toplevel = (Gtk.Window) host.Toplevel;
			
			TransientFor = host_toplevel;

			this.ModifyFg (Gtk.StateType.Normal, new Gdk.Color (127, 127, 127));
			this.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (0, 0, 0));

			host_toplevel.ConfigureEvent += HandleHostConfigure;
			host_toplevel.SizeAllocated += HandleHostSizeAllocated;
		}

		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
						int round = 12;
			Context g = CairoUtils.CreateContext (GdkWindow);
			g.Operator = Operator.Source;
			g.Source = new SolidPattern (new Cairo.Color (0, 0, 0, 0));
			g.Paint ();
			g.Operator = Operator.Over;

			g.Source = new SolidPattern (new Cairo.Color (0, 0, 0, .7));
			g.MoveTo (round, 0);
			g.Arc (Allocation.Width - round, round, round, - Math.PI * 0.5, 0);
			g.Arc (Allocation.Width - round, Allocation.Height - round, round, 0, Math.PI * 0.5);
			g.Arc (round, Allocation.Height - round, round, Math.PI * 0.5, Math.PI);
			g.Arc (round, round, round, Math.PI, Math.PI * 1.5);
			g.ClosePath ();
			g.Fill ();

			((IDisposable)g).Dispose ();
			return base.OnExposeEvent (args);
		}

		protected override bool OnMotionNotifyEvent (Gdk.EventMotion args)
		{
			base.OnMotionNotifyEvent (args);
			return false;
		}

		private void HandleHostSizeAllocated (object o, SizeAllocatedArgs args)
		{
			Relocate ();
		}

		private void HandleHostConfigure (object o, ConfigureEventArgs args)
		{
			Relocate ();
		}

		private void Relocate ()
		{
			int x, y;
			if (!host_toplevel.IsMapped)
				return;

			Realize ();
			host_toplevel.GdkWindow.GetOrigin (out x, out y);

			//Console.WriteLine ("({0}, {1}) top alloc {2}", x, y, host_toplevel.Allocation);
			x += (int) (host_toplevel.Allocation.Width * 0.5);
			y += (int) (host_toplevel.Allocation.Height * 0.8);
			
			x -= (int) (Allocation.Width * 0.5);
			//Console.WriteLine ("QQQWDSDFSDFWQQQQQQQQQQQQQQQQQQQQ ({0},{0}", x, y);
			Move (x, y);
		}
		
		protected override void OnRealized ()
		{
			bool composited = CompositeUtils.IsComposited (Screen) && CompositeUtils.SetRgbaColormap (this);
			AppPaintable = composited;
			base.OnRealized ();
			Visibility = VisibilityType.Full;
		}

		protected override void OnMapped ()
		{
			base.OnMapped ();
			Relocate ();
		}
	}
}
