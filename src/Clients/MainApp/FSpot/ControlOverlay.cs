/*
 * ControlOverlay.cs
 *
 * Copyright 2007 Novell Inc.
 *
 * Author
 *   Larry Ewing <lewing@novell.com>
 *
 * See COPYING for license information.
 */
using Cairo;

using System;
using Gtk;
using FSpot.Widgets;
using FSpot.Utils;
using FSpot.Core;
using FSpot.Gui;

namespace FSpot {
	public class ControlOverlay : Window {
		Widget host;
		Window host_toplevel;
		bool composited;
		VisibilityType visibility;
		int round = 12;
		DelayedOperation hide;
		DelayedOperation fade;
		DelayedOperation dismiss;
		bool auto_hide = true;
		double x_align = 0.5;
		double y_align = 1.0;

		public enum VisibilityType
		{
			None,
			Partial,
			Full
		}

		public double XAlign {
			get { return x_align; }
			set {
				x_align = value;
				Relocate ();
			}
		}

		public double YAlign {
			get { return  y_align; }
			set {
				y_align = value;
				Relocate ();
			}
		}


		public bool AutoHide {
			get { return auto_hide; }
			set { auto_hide = value; }
		}

		public VisibilityType Visibility {
			get { return visibility; }
			set {
				if (dismiss.IsPending && value != VisibilityType.None)
					return;

				switch (value) {
				case VisibilityType.None:
					FadeToTarget (0.0);
					break;
				case VisibilityType.Partial:
					FadeToTarget (0.4);
					break;
				case VisibilityType.Full:
					FadeToTarget (0.8);
					break;
				}
				visibility = value;
			}

		}

		public ControlOverlay (Gtk.Widget host) : base (WindowType.Popup)
		{
			this.host = host;
			Decorated = false;
			DestroyWithParent = true;
			Name = "FullscreenContainer";
			AllowGrow = true;
			//AllowShrink = true;
			KeepAbove = true;

			host_toplevel = (Gtk.Window) host.Toplevel;

			TransientFor = host_toplevel;

			host_toplevel.ConfigureEvent += HandleHostConfigure;
			host_toplevel.SizeAllocated += HandleHostSizeAllocated;

			AddEvents ((int) (Gdk.EventMask.PointerMotionMask));
			hide = new DelayedOperation (2000, HideControls);
			fade = new DelayedOperation (40, FadeToTarget);
			dismiss = new DelayedOperation (2000, delegate { /* do nothing */ return false; });
		}

		protected override void OnDestroyed ()
		{
			hide.Stop ();
			fade.Stop ();
			base.OnDestroyed ();
		}

		public bool HideControls ()
		{
			int x, y;
			Gdk.ModifierType type;

			if (!auto_hide)
				return false;

			if (IsRealized) {
				GdkWindow.GetPointer (out x, out y, out type);
				if (Allocation.Contains (x, y)) {
					hide.Start ();
					return true;
				}
			}

			hide.Stop ();
			Visibility = VisibilityType.None;
			return false;
		}

		protected virtual void ShapeSurface (Context cr, Cairo.Color color)
		{
			cr.Operator = Operator.Source;
			Cairo.Pattern p = new Cairo.SolidPattern (new Cairo.Color (0, 0, 0, 0));
			cr.Source = p;
			p.Destroy ();
			cr.Paint ();
			cr.Operator = Operator.Over;

			Cairo.Pattern r = new SolidPattern (color);
			cr.Source = r;
			r.Destroy ();
			cr.MoveTo (round, 0);
			if (x_align == 1.0)
				cr.LineTo (Allocation.Width, 0);
			else
				cr.Arc (Allocation.Width - round, round, round, - Math.PI * 0.5, 0);
			if (x_align == 1.0 || y_align == 1.0)
				cr.LineTo (Allocation.Width, Allocation.Height);
			else
				cr.Arc (Allocation.Width - round, Allocation.Height - round, round, 0, Math.PI * 0.5);
			if (y_align == 1.0)
				cr.LineTo (0, Allocation.Height);
			else
				cr.Arc (round, Allocation.Height - round, round, Math.PI * 0.5, Math.PI);
			cr.Arc (round, round, round, Math.PI, Math.PI * 1.5);
			cr.ClosePath ();
			cr.Fill ();
		}

		double target;
		double opacity = 0;

		bool FadeToTarget ()
		{
			//Log.Debug ("op {0}\ttarget{1}", opacity, target);
			Visible = (opacity > 0.05);
			if (Math.Abs (target - opacity) < .05)
				return false;
			if (target > opacity)
				opacity += .04;
			else
				opacity -= .04;
			if (Visible)
				CompositeUtils.SetWinOpacity (this, opacity);
			else
				Hide ();
			return true;
		}

		bool FadeToTarget (double target)
		{
			//Log.Debug ("FadeToTarget {0}", target);
			Realize ();
			this.target = target;
			fade.Start ();

			if (target > 0.0)
				hide.Restart ();

			return false;
		}

		private void ShapeWindow ()
		{
			if (composited)
				return;

			Gdk.Pixmap bitmap = new Gdk.Pixmap (GdkWindow,
							    Allocation.Width,
							    Allocation.Height, 1);

			Context cr = Gdk.CairoHelper.Create (bitmap);
			ShapeCombineMask (bitmap, 0, 0);
			ShapeSurface (cr, new Color (1, 1, 1));
			ShapeCombineMask (bitmap, 0, 0);
			((IDisposable)cr).Dispose ();
			bitmap.Dispose ();

		}

		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			Gdk.Color c = Style.Background (State);
			Context cr = Gdk.CairoHelper.Create (GdkWindow);

			ShapeSurface (cr, new Cairo.Color (c.Red / (double) ushort.MaxValue,
							   c.Blue / (double) ushort.MaxValue,
							   c.Green / (double) ushort.MaxValue,
							   0.8));

			((IDisposable)cr).Dispose ();
			return base.OnExposeEvent (args);
		}

		protected override bool OnMotionNotifyEvent (Gdk.EventMotion args)
		{
			this.Visibility = VisibilityType.Full;
			base.OnMotionNotifyEvent (args);
			return false;
		}

		protected override void OnSizeAllocated (Gdk.Rectangle rec)
		{
			base.OnSizeAllocated (rec);
			Relocate ();
			ShapeWindow ();
			QueueDraw ();
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
			if (!IsRealized || !host_toplevel.IsRealized)
				return;

			host.GdkWindow.GetOrigin (out x, out y);

			int xOrigin = x;
			int yOrigin = y;

			x += (int) (host.Allocation.Width * x_align);
			y += (int) (host.Allocation.Height * y_align);

			x -= (int) (Allocation.Width * 0.5);
			y -= (int) (Allocation.Height * 0.5);

			x = Math.Max (0, Math.Min (x, xOrigin + host.Allocation.Width - Allocation.Width));
			y = Math.Max (0, Math.Min (y, yOrigin + host.Allocation.Height - Allocation.Height));

			Move (x, y);
		}

		protected override void OnRealized ()
		{
			composited = CompositeUtils.IsComposited (Screen) && CompositeUtils.SetRgbaColormap (this);
			AppPaintable = composited;

			base.OnRealized ();

			ShapeWindow ();
			Relocate ();
		}

		public void Dismiss ()
		{
			Visibility = VisibilityType.None;
			dismiss.Start ();
		}

		protected override void OnMapped ()
		{
			base.OnMapped ();
			Relocate ();
		}
	}
}
