//
// Loupe.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2005-2007 Novell, Inc.
// Copyright (C) 2005-2007 Larry Ewing
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Cairo;
using Gtk;
using Gdk;

using FSpot.Core;
using FSpot.Utils;
using FSpot.Gui;

using Hyena;

namespace FSpot.Widgets
{
	public class Loupe : Gtk.Window
	{
		protected PhotoImageView view;
		protected Gdk.Rectangle region;
		bool use_shape_ext = false;
		protected Pixbuf source;
		protected Pixbuf overlay;
		int radius = 128;
		int inner = 128;
		int border = 6;
		double angle = Math.PI / 4;
		Gdk.Point start;
		Gdk.Point start_hot;
		Gdk.Point hotspot;

		public Loupe (PhotoImageView view) : base ("Loupe")
		{
			this.view = view;
			Decorated = false;

			var win = (Gtk.Window) view.Toplevel;

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

			var tmp = new Pixbuf (view.Pixbuf,
						 region.X, region.Y,
						 region.Width, region.Height);
			using (tmp)
				source = FSpot.Utils.PixbufUtils.TransformOrientation (tmp, view.PixbufOrientation);

			//FIXME sometimes that ctor returns results with a null
			//handle this case ourselves
			if (source.Handle == IntPtr.Zero)
				source = null;

			QueueDraw ();
		}

		[GLib.ConnectBefore]
		void HandleImageViewMotion (object sender, MotionNotifyEventArgs args)
		{
			Gdk.Point coords;
			coords = new Gdk.Point ((int) args.Event.X, (int) args.Event.Y);

			SetSamplePoint (view.WindowCoordsToImage (coords));
		}

		void ShapeWindow ()
		{
			Layout ();
			var bitmap = new Pixmap (GdkWindow,
							    Allocation.Width,
							    Allocation.Height, 1);

			Context g = CairoHelper.Create (bitmap);
			DrawShape (g, Allocation.Width, Allocation.Height);

			g.Dispose ();

			if (use_shape_ext)
				ShapeCombineMask (bitmap, 0, 0);
			else {
				Context rgba = CairoHelper.Create (GdkWindow);
				DrawShape (rgba, Allocation.Width, Allocation.Height);
				rgba.Dispose ();
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

		void DrawShape (Context g, int width, int height)
		{
			int inner_x = radius + border + inner;
			int cx = Center.X;
			int cy = Center.Y;

			g.Operator = Operator.Source;
			g.SetSource (new SolidPattern (new Cairo.Color (0,0,0,0)));
			g.Rectangle (0, 0, width, height);
			g.Paint ();

			g.NewPath ();
			g.Translate (cx, cy);
			g.Rotate (angle);

			g.SetSource (new SolidPattern (new Cairo.Color (0.2, 0.2, 0.2, .6)));
			g.Operator = Operator.Over;
			g.Rectangle (0, - (border + inner), inner_x, 2 * (border + inner));
			g.Arc (inner_x, 0, inner + border, 0, 2 * Math.PI);
			g.Arc (0, 0, radius + border, 0, 2 * Math.PI);
			g.Fill ();

			g.SetSource (new SolidPattern (new Cairo.Color (0, 0, 0, 1.0)));
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
				g.SetSource (new SolidPattern (new Cairo.Color (1.0, 1.0, 1.0, 1.0)));
				g.Stroke ();
			}
		}

		protected override bool OnExposeEvent (EventExpose args)
		{
			Context g = CairoHelper.Create (GdkWindow);

			DrawShape (g, Allocation.Width, Allocation.Height);
			//base.OnExposeEvent (args);
			g.Dispose ();
			return false;

		}

		bool dragging;
		bool rotate;
		DelayedOperation drag;
		Gdk.Point pos;
		double start_angle = 0;
		Gdk.Point root_pos;
		Gdk.Point start_root;
		Cursor opened_hand_cursor = new Cursor (CursorType.Hand1);
		Cursor closed_hand_cursor = new Cursor (CursorType.Fleur);

		void HandleMotionNotifyEvent (object sender, MotionNotifyEventArgs args)
		{
			pos.X = (int) args.Event.XRoot - start.X;
			pos.Y = (int) args.Event.YRoot - start.Y;

			root_pos.X = (int) args.Event.XRoot;
			root_pos.Y = (int) args.Event.YRoot;

			if (dragging)
				drag.Start ();
		}

		bool DragUpdate ()
		{
			if (!dragging)
				return false;

			if (!rotate)
				return MoveWindow ();

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

			var v1 = new Vector (initial);
			var v2 = new Vector (now);

			double angleBetween = Vector.AngleBetween (v1, v2);

			Angle = start_angle + angleBetween;
			return false;
		}

		bool MoveWindow ()
		{
			Gdk.Point view_coords;
			Gdk.Point top;
			Gdk.Point current;

			GdkWindow.GetOrigin (out current.X, out current.Y);

			if (current == pos)
				return false;

			Move (pos.X, pos.Y);

			pos.Offset (hotspot.X, hotspot.Y);
			var toplevel = (Gtk.Window) view.Toplevel;
			toplevel.GdkWindow.GetOrigin (out top.X, out top.Y);
			toplevel.TranslateCoordinates (view,
						       pos.X - top.X,  pos.Y - top.Y,
						       out view_coords.X, out view_coords.Y);

			SetSamplePoint (view.WindowCoordsToImage (view_coords));

			return false;
		}

		void HandleItemChanged (object sender, BrowsablePointerChangedEventArgs args)
		{
			UpdateSample ();
		}

		void HandleButtonPressEvent (object sender, ButtonPressEventArgs args)
		{
			switch (args.Event.Type) {
			case EventType.ButtonPress:
				if (args.Event.Button == 1) {
					start = new Gdk.Point ((int)args.Event.X, (int)args.Event.Y);
					start_root = new Gdk.Point ((int)args.Event.XRoot, (int)args.Event.YRoot);
					start_hot = hotspot;

					Gdk.Point win;
					GdkWindow.GetOrigin (out win.X, out win.Y);
					start_hot.X += win.X;
					start_hot.Y += win.Y;

					dragging = true;
					rotate = (args.Event.State & ModifierType.ShiftMask) > 0;
					start_angle = Angle;
				} else {
					Angle += Math.PI /8;
				}
				break;
			case EventType.TwoButtonPress:
				dragging = false;
				App.Instance.Organizer.HideLoupe ();
				break;
			}
		}

		void HandleViewZoomChanged (object sender, EventArgs args)
		{
			UpdateSample ();
		}

		void HandleButtonReleaseEvent (object sender, ButtonReleaseEventArgs args)
		{
			dragging = false;
		}

		void HandleKeyPressEvent (object sender, KeyPressEventArgs args)
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

			AddEvents ((int) (EventMask.PointerMotionMask
					  | EventMask.ButtonPressMask
					  | EventMask.ButtonReleaseMask));

			ButtonPressEvent += HandleButtonPressEvent;
			ButtonReleaseEvent += HandleButtonReleaseEvent;
			MotionNotifyEvent += HandleMotionNotifyEvent;
			KeyPressEvent += HandleKeyPressEvent;

			drag = new DelayedOperation (20, new GLib.IdleHandler (DragUpdate));

			// Update the cursor appropriate to indicate dragability/dragging
			bool inside = false, pressed = false;
			EnterNotifyEvent += (o, args) => {
				inside = true;
				if (!pressed)
					GdkWindow.Cursor = opened_hand_cursor;
			};
			LeaveNotifyEvent += (o, args) => {
				inside = false;
				if (!pressed)
					GdkWindow.Cursor = null;
			};
			ButtonPressEvent += (o, args) => {
				pressed = true;
				if (null != GdkWindow)
					GdkWindow.Cursor = closed_hand_cursor;
			};
			ButtonReleaseEvent += (o, args) => {
				pressed = false;
				GdkWindow.Cursor = inside ? opened_hand_cursor : null;
			};
		}
	}
}
