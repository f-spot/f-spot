//
// FSpot.Widgets.Loupe.cs
//
// Author(s):
//	Larry Ewing  <lewing@novell.com:
//
// Copyright (c) 2005-2009 Novell, Inc.
//
// This is free software. See COPYING for details.
//
using Cairo;

using Gtk;
using Gdk;
using System;
using System.Runtime.InteropServices;
using Mono.Unix;
using FSpot.Core;
using FSpot.Utils;
using FSpot.Gui;
using Hyena;

namespace FSpot.Widgets {
	public class Loupe : Gtk.Window {
		protected PhotoImageView view;
		protected Gdk.Rectangle region;
		bool use_shape_ext = false;
		protected Gdk.Pixbuf source;
		protected Gdk.Pixbuf overlay;
		private int radius = 128;
		private int inner = 128;
		private int border = 6;
		private double angle = Math.PI / 4;
		Gdk.Point start;
		Gdk.Point start_hot;
		Gdk.Point hotspot;

		public Loupe (PhotoImageView view) : base ("Loupe")
		{
			this.view = view;
			Decorated = false;

			Gtk.Window win = (Gtk.Window) view.Toplevel;

			win.GetPosition (out old_win_pos.X, out old_win_pos.Y);
			win.ConfigureEvent += HandleToplevelConfigure;

			TransientFor = win;
			DestroyWithParent = true;

			BuildUI ();
		}

		Gdk.Point old_win_pos;
		[GLib.ConnectBefore]
		public void HandleToplevelConfigure (object o, ConfigureEventArgs args)
		{
			int x, y;
			int loupe_x, loupe_y;

			x = args.Event.X - old_win_pos.X;
			y = args.Event.Y - old_win_pos.Y;

			GetPosition (out loupe_x, out loupe_y);
			Move (loupe_x + x, loupe_y + y);

			old_win_pos.X = args.Event.X;
			old_win_pos.Y = args.Event.Y;
		}

		// FIXME
		//screen "composited-changed"

		public int Radius {
			get {
				return radius;
			}
			set {
				if (radius != value) {
					radius = value;
					UpdateSample ();
				}
			}
		}

		public int Border {
			get {
				return border;
			}
			set {
				if (border != value) {
					border = value;
					UpdateSample ();
				}
			}
		}

		public double Angle {
			get {
				return angle;
			}
			set {
				Gdk.Point then = hotspot;
				angle = value;
				Layout ();
				Gdk.Point now = hotspot;
				//System.Console.WriteLine ("{0} now {1}", then, now);
				int x, y;
				GdkWindow.GetOrigin (out x, out y);
				//GdkWindow.MoveResize (x + then.X - now.X, y + then.Y - now.Y, Bounds.Width, Bounds.Height);
				ShapeWindow ();
				Move (x + then.X - now.X, y + then.Y - now.Y);
				//QueueResize ();
			}
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			if (IsRealized)
				ShapeWindow ();

			base.OnSizeAllocated (allocation);
		}

		protected override void OnRealized ()
		{
			use_shape_ext = ! (CompositeUtils.IsComposited (Screen) && CompositeUtils.SetRgbaColormap (this));

			base.OnRealized ();
			ShapeWindow ();
		}

		void SetSamplePoint (Gdk.Point p)
		{
			region.X = p.X;
			region.Y = p.Y;
			region.Width = 2 * radius;
			region.Height = 2 * radius;

			if (view.Pixbuf != null) {
				region.Offset (- Math.Min (region.X, Math.Max (region.Right - view.Pixbuf.Width, radius)),
					       - Math.Min (region.Y, Math.Max (region.Bottom - view.Pixbuf.Height, radius)));

				region.Intersect (new Gdk.Rectangle (0, 0, view.Pixbuf.Width, view.Pixbuf.Height));
			}
			UpdateSample ();
		}

		protected virtual void UpdateSample ()
		{
			if (source != null)
				source.Dispose ();

			source = null;

			if (view.Pixbuf == null)
				return;

			int small = (int) (radius * view.Zoom);
			if (small != inner) {
				inner = small;
				QueueResize ();
			}

			Pixbuf tmp = new Gdk.Pixbuf (view.Pixbuf,
						 region.X, region.Y,
						 region.Width, region.Height);
			using (tmp)
				source = FSpot.Utils.PixbufUtils.TransformOrientation (tmp, view.PixbufOrientation);

			//FIXME sometimes that ctor returns results with a null
			//handle this case ourselves
			if (source.Handle == IntPtr.Zero)
				source = null;

			this.QueueDraw ();
		}

		[GLib.ConnectBefore]
		private void HandleImageViewMotion (object sender, MotionNotifyEventArgs args)
		{
			Gdk.Point coords;
			coords = new Gdk.Point ((int) args.Event.X, (int) args.Event.Y);

			SetSamplePoint (view.WindowCoordsToImage (coords));
		}

		private void ShapeWindow ()
		{
			Layout ();
			Gdk.Pixmap bitmap = new Gdk.Pixmap (GdkWindow,
							    Allocation.Width,
							    Allocation.Height, 1);

			Context g = CairoHelper.Create (bitmap);
			DrawShape (g, Allocation.Width, Allocation.Height);

			((IDisposable)g).Dispose ();

			if (use_shape_ext)
				ShapeCombineMask (bitmap, 0, 0);
			else {
				Cairo.Context rgba = CairoHelper.Create (GdkWindow);
				DrawShape (rgba, Allocation.Width, Allocation.Height);
				((IDisposable)rgba).Dispose ();
				try {
					CompositeUtils.InputShapeCombineMask (this, bitmap, 0,0);
				} catch (EntryPointNotFoundException) {
					Log.Warning ("gtk+ version doesn't support input shapping");
				}
			}
			bitmap.Dispose ();
		}

		Gdk.Point Center;
	        Requisition Bounds;

		public void Layout ()
		{
			double a = radius + border;
			double b = inner + border;
			double x_proj = (a + b - border) * Math.Cos (angle);
			double y_proj = (a + b - border) * Math.Sin (angle);

			Center.X = (int) Math.Ceiling (Math.Max (-x_proj + b, a));
			Center.Y = (int) Math.Ceiling (Math.Max (-y_proj + b, a));

			Bounds.Width = (int) Math.Ceiling (Math.Max (Math.Abs (x_proj) + b, a) + b + a);
			Bounds.Height = (int) Math.Ceiling (Math.Max (Math.Abs (y_proj) + b, a) + b + a);

			hotspot.X = (int) Math.Ceiling (Center.X + x_proj);
			hotspot.Y = (int) Math.Ceiling (Center.Y + y_proj);
		}

		private void DrawShape (Context g, int width, int height)
		{
			int inner_x = radius + border + inner;
			int cx = Center.X;
			int cy = Center.Y;

			g.Operator = Operator.Source;
			g.Source = new SolidPattern (new Cairo.Color (0,0,0,0));
			g.Rectangle (0, 0, width, height);
			g.Paint ();

			g.NewPath ();
			g.Translate (cx, cy);
			g.Rotate (angle);

			g.Source = new SolidPattern (new Cairo.Color (0.2, 0.2, 0.2, .6));
			g.Operator = Operator.Over;
			g.Rectangle (0, - (border + inner), inner_x, 2 * (border + inner));
			g.Arc (inner_x, 0, inner + border, 0, 2 * Math.PI);
			g.Arc (0, 0, radius + border, 0, 2 * Math.PI);
			g.Fill ();

			g.Source = new SolidPattern (new Cairo.Color (0, 0, 0, 1.0));
			g.Operator = Operator.DestOut;
			g.Arc (inner_x, 0, inner, 0, 2 * Math.PI);
#if true
			g.Fill ();
#else
			g.FillPreserve ();

			g.Operator = Operator.Over;
			RadialGradient rg = new RadialGradient (inner_x - (inner * 0.3), inner * 0.3 , inner * 0.1, inner_x, 0, inner);
			rg.AddColorStop (0, new Cairo.Color (0.0, 0.2, .8, 0.5));
			rg.AddColorStop (0.7, new Cairo.Color (0.0, 0.2, .8, 0.1));
			rg.AddColorStop (1.0, new Cairo.Color (0.0, 0.0, 0.0, 0.0));
			g.Source = rg;
			g.Fill ();
			rg.Destroy ();
#endif
			g.Operator = Operator.Over;
			g.Matrix = new Matrix ();
			g.Translate (cx, cy);
			if (source != null)
			CairoHelper.SetSourcePixbuf (g, source, -source.Width / 2, -source.Height / 2);

			g.Arc (0, 0, radius, 0, 2 * Math.PI);
			g.Fill ();

			if (overlay != null) {
				CairoHelper.SetSourcePixbuf (g, overlay, -overlay.Width / 2, -overlay.Height / 2);
				g.Arc (0, 0, radius, angle, angle + Math.PI);
				g.ClosePath ();
				g.FillPreserve ();
				g.Source = new SolidPattern (new Cairo.Color (1.0, 1.0, 1.0, 1.0));
				g.Stroke ();
			}
		}

		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			Context g = CairoHelper.Create (GdkWindow);

			DrawShape (g, Allocation.Width, Allocation.Height);
			//base.OnExposeEvent (args);
			((IDisposable)g).Dispose ();
			return false;

		}

		bool dragging = false;
		bool rotate = false;
		DelayedOperation drag;
		Gdk.Point pos;
		double start_angle = 0;
		Gdk.Point root_pos;
		Gdk.Point start_root;
		Gdk.Cursor opened_hand_cursor = new Gdk.Cursor (Gdk.CursorType.Hand1);
		Gdk.Cursor closed_hand_cursor = new Gdk.Cursor (Gdk.CursorType.Fleur);

		private void HandleMotionNotifyEvent (object sender, MotionNotifyEventArgs args)
		{
		        pos.X = (int) args.Event.XRoot - start.X;
		        pos.Y = (int) args.Event.YRoot - start.Y;

			root_pos.X = (int) args.Event.XRoot;
			root_pos.Y = (int) args.Event.YRoot;

			if (dragging)
				drag.Start ();
		}

		private bool DragUpdate ()
		{
			if (!dragging)
				return false;

			if (!rotate) {
				return MoveWindow ();
			} else {
				Gdk.Point initial = start_root;
				Gdk.Point hot = start_hot;
				Gdk.Point win = Gdk.Point.Zero;

				hot.X += win.X;
				hot.Y += win.Y;

				initial.X -= hot.X;
				initial.Y -= hot.Y;
				Gdk.Point now = root_pos;
				now.X -= hot.X;
				now.Y -= hot.Y;

				Vector v1 = new Vector (initial);
				Vector v2 = new Vector (now);

				double angle = Vector.AngleBetween (v1, v2);

				Angle = start_angle + angle;
				return false;
			}
		}

		private bool MoveWindow ()
		{
			Gdk.Point view_coords;
			Gdk.Point top;
			Gdk.Point current;

			GdkWindow.GetOrigin (out current.X, out current.Y);

			if (current == pos)
				return false;

			Move (pos.X, pos.Y);

			pos.Offset (hotspot.X, hotspot.Y);
			Gtk.Window toplevel = (Gtk.Window) view.Toplevel;
			toplevel.GdkWindow.GetOrigin (out top.X, out top.Y);
			toplevel.TranslateCoordinates (view,
						       pos.X - top.X,  pos.Y - top.Y,
						       out view_coords.X, out view_coords.Y);

			SetSamplePoint (view.WindowCoordsToImage (view_coords));

			return false;
		}

		private void HandleItemChanged (object sender, BrowsablePointerChangedEventArgs args)
		{
			UpdateSample ();
		}

		private void HandleButtonPressEvent (object sender, ButtonPressEventArgs args)
		{
			switch (args.Event.Type) {
			case Gdk.EventType.ButtonPress:
				if (args.Event.Button == 1) {
					start = new Gdk.Point ((int)args.Event.X, (int)args.Event.Y);
					start_root = new Gdk.Point ((int)args.Event.XRoot, (int)args.Event.YRoot);
					start_hot = hotspot;

					Gdk.Point win;
					GdkWindow.GetOrigin (out win.X, out win.Y);
					start_hot.X += win.X;
					start_hot.Y += win.Y;

					dragging = true;
					rotate = (args.Event.State & Gdk.ModifierType.ShiftMask) > 0;
					start_angle = Angle;
				} else {
					Angle += Math.PI /8;
				}
				break;
			case Gdk.EventType.TwoButtonPress:
				dragging = false;
				App.Instance.Organizer.HideLoupe ();
				break;
			}
		}

		private void HandleViewZoomChanged (object sender, System.EventArgs args)
		{
			UpdateSample ();
		}

		private void HandleButtonReleaseEvent (object sender, ButtonReleaseEventArgs args)
		{
			dragging = false;
		}

		private void HandleKeyPressEvent (object sender, Gtk.KeyPressEventArgs args)
		{
			switch (args.Event.Key) {
			case Gdk.Key.v:
				App.Instance.Organizer.HideLoupe ();
				args.RetVal = true;
				break;
			default:
				break;
			}
			return;
		}

		protected override void OnDestroyed ()
		{
			view.MotionNotifyEvent -= HandleImageViewMotion;
			view.Item.Changed -= HandleItemChanged;
			view.ZoomChanged -= HandleViewZoomChanged;

			opened_hand_cursor.Dispose ();
			closed_hand_cursor.Dispose ();
			opened_hand_cursor = null;
			closed_hand_cursor = null;

			base.OnDestroyed ();
		}

		protected Widget SetFancyStyle (Widget widget)
		{
			//widget.ModifyFg (Gtk.StateType.Normal, new Gdk.Color (127, 127, 127));
			//widget.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (0, 0, 0));
			return widget;
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			Layout ();
			requisition = Bounds;
		}

		protected virtual void BuildUI ()
		{
			SetFancyStyle (this);

			TransientFor = (Gtk.Window) view.Toplevel;
			SkipPagerHint = true;
			SkipTaskbarHint = true;

			//view.MotionNotifyEvent += HandleImageViewMotion;
			view.Item.Changed += HandleItemChanged;
			view.ZoomChanged += HandleViewZoomChanged;

			SetSamplePoint (Gdk.Point.Zero);

			AddEvents ((int) (Gdk.EventMask.PointerMotionMask
					  | Gdk.EventMask.ButtonPressMask
					  | Gdk.EventMask.ButtonReleaseMask));

			ButtonPressEvent += HandleButtonPressEvent;
			ButtonReleaseEvent += HandleButtonReleaseEvent;
			MotionNotifyEvent += HandleMotionNotifyEvent;
			KeyPressEvent += HandleKeyPressEvent;

			drag = new DelayedOperation (20, new GLib.IdleHandler (DragUpdate));

			// Update the cursor appropriate to indicate dragability/dragging
			bool inside = false, pressed = false;
			EnterNotifyEvent += delegate {
				inside = true;
				if (!pressed)
					GdkWindow.Cursor = opened_hand_cursor;
			};
			LeaveNotifyEvent += delegate {
				inside = false;
				if (!pressed)
					GdkWindow.Cursor = null;
			};
			ButtonPressEvent += delegate {
				pressed = true;
				GdkWindow.Cursor = closed_hand_cursor;
			};
			ButtonReleaseEvent += delegate {
				pressed = false;
				GdkWindow.Cursor = inside ? opened_hand_cursor : null;
			};
		}
	}
}
