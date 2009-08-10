//
// FSpot.Widgets.ImageView.cs
//
// Author(s):
//	Stephane Delcroix  <stephane@delcroix.org>
//
// This is free software. See COPYING for details.
//

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Gtk;
using Gdk;

using FSpot.Utils;

namespace FSpot.Widgets
{
	public class ImageView : Container
	{
#region public API
		public ImageView (Adjustment hadjustment, Adjustment vadjustment, bool can_select) : base ()
		{
			OnSetScrollAdjustments (hadjustment, vadjustment);
			children = new List<LayoutChild> ();
			AdjustmentsChanged += ScrollToAdjustments;
			WidgetFlags &= ~WidgetFlags.NoWindow;
			SetFlag (WidgetFlags.CanFocus);

			this.can_select = can_select;
		}

		public ImageView (bool can_select) : this (null, null, can_select)
		{
		}

		public ImageView () : this (true)
		{
		}

		Pixbuf pixbuf;
		public Pixbuf Pixbuf {
			get { return pixbuf; } 
			set {
				if (pixbuf == value)
					return;

				pixbuf = value;
				min_zoom = ComputeMinZoom (upscale);

				ComputeScaledSize ();
				AdjustmentsChanged -= ScrollToAdjustments;
				Hadjustment.Value = Vadjustment.Value = 0;
				XOffset = YOffset = 0;
				AdjustmentsChanged += ScrollToAdjustments;
				QueueDraw ();
			} 
		}

		PixbufOrientation pixbuf_orientation;
		public PixbufOrientation PixbufOrientation {
			get { return pixbuf_orientation; }
			set {
				if (value == pixbuf_orientation)
					return;
				pixbuf_orientation = value;
				min_zoom = ComputeMinZoom (upscale);
				ComputeScaledSize ();
				QueueDraw ();
			}
		}

		CheckPattern check_pattern = CheckPattern.Dark;
		public CheckPattern CheckPattern {
			get { return check_pattern; } 
			set { 
				if (check_pattern == value)
					return;
				check_pattern = value;
				if (Pixbuf != null && Pixbuf.HasAlpha)
					QueueDraw ();
			} 
		}

		PointerMode pointer_mode = PointerMode.Select;
		public PointerMode PointerMode {
			get { return pointer_mode; } 
			set { pointer_mode = value; } 
		}

		Adjustment hadjustment;
		public Adjustment Hadjustment {
			get { return hadjustment; }
		}

		Adjustment vadjustment;
		public Adjustment Vadjustment {
			get { return vadjustment; }
		}

		bool can_select = false;
		public bool CanSelect {
			get { return can_select; }
			set { 
				if (can_select == value)
					return;
				can_select = value;
				if (!can_select)
					selection = Rectangle.Zero;
			}
		}

		Gdk.Rectangle selection = Rectangle.Zero;
		public Gdk.Rectangle Selection {
			get {
				if (!can_select)
					return Rectangle.Zero;
				return selection;
			}
			set { 
				if (!can_select)
					return;

				if (value == selection)
					return;

				selection = value;

				EventHandler eh = SelectionChanged;
				if (eh != null)
					eh (this, EventArgs.Empty);
				QueueDraw ();
			}
		}

		double selection_xy_ratio = 0;
		public double SelectionXyRatio {
			get { return selection_xy_ratio; } 
			set {
				if (selection_xy_ratio == value)
					return;
				selection_xy_ratio = value;

				if (selection_xy_ratio == 0)
					return;

				if (Selection == Rectangle.Zero)
					return;

				Selection = ConstrainSelection (Selection, false, false);
			} 
		}

		InterpType interpolation = InterpType.Bilinear;
		public Gdk.InterpType Interpolation {
			get { return interpolation; } 
			set { 
				if (interpolation == value)
					return;
				interpolation = value;
				QueueDraw ();
			} 
		}

		double zoom = 1.0;
		public double Zoom {
			get { return zoom; }
			set { DoZoom (value, false, 0, 0); }
		}

		public void ZoomIn ()
		{
			Zoom *= ZOOM_FACTOR;
		}

		public void ZoomOut ()
		{
			Zoom *= 1.0/ZOOM_FACTOR;
		}

		public void ZoomAboutPoint (double zoom_increment, int x, int y)
		{
			DoZoom (zoom * zoom_increment, true, x, y);
		}	

		bool fit;
		public bool Fit {
			get { return fit; } 
		}

		public void ZoomFit (bool upscale)
		{
			Gtk.ScrolledWindow scrolled = Parent as Gtk.ScrolledWindow;
			if (scrolled != null)
				scrolled.SetPolicy (Gtk.PolicyType.Never, Gtk.PolicyType.Never);
			
			min_zoom = ComputeMinZoom (upscale);
			
			this.upscale = upscale;

			fit = true;
			DoZoom (MIN_ZOOM, false, 0, 0);

			if (scrolled != null)
				GLib.Idle.Add (delegate {scrolled.SetPolicy (Gtk.PolicyType.Automatic, Gtk.PolicyType.Automatic); return false;});
		}

		public Point WindowCoordsToImage (Point win)
		{
			if (Pixbuf == null)
				return Point.Zero;

			int x_offset = scaled_width < Allocation.Width ? (int)(Allocation.Width - scaled_width) / 2 : -XOffset;
			int y_offset = scaled_height < Allocation.Height ? (int)(Allocation.Height - scaled_height) / 2 : -YOffset;

			win.X = Clamp (win.X - x_offset, 0, (int)scaled_width - 1);
			win.Y = Clamp (win.Y - y_offset, 0, (int)scaled_height - 1);

			win = PixbufUtils.TransformOrientation ((int)scaled_width, (int)scaled_height, win, PixbufUtils.ReverseTransformation (pixbuf_orientation));

			return  new Point ((int) Math.Floor (win.X * (double)(((int)PixbufOrientation <= 4 ? Pixbuf.Width : Pixbuf.Height) - 1) / (double)(scaled_width - 1) + .5),
					   (int) Math.Floor (win.Y * (double)(((int)PixbufOrientation <= 4 ? Pixbuf.Height : Pixbuf.Width) - 1) / (double)(scaled_height - 1) + .5));
		}

		List<LayoutChild> children;
		public void Put (Gtk.Widget widget, int x, int y)
		{
			children.Add (new LayoutChild (widget, x, y));
			if (IsRealized)
				widget.ParentWindow = GdkWindow;
			widget.Parent = this;
		}

		public void Move (Gtk.Widget widget, int x, int y)
		{
			LayoutChild child = GetChild (widget);
			if (child == null)
				return;

			child.X = x;
			child.Y = y;
			if (Visible && widget.Visible)
				QueueResize ();
		}

		public event EventHandler ZoomChanged;
		public event EventHandler SelectionChanged;
#endregion

#region protectedAPI
		protected static double ZOOM_FACTOR = 1.1;
		protected double max_zoom = 10.0;
		protected double MAX_ZOOM {
			get { return max_zoom; }
		}

		protected double min_zoom = 0.1;
		protected double MIN_ZOOM {
			get { return min_zoom; }
		}

		bool upscale;
		protected void ZoomFit ()
		{
			ZoomFit (upscale);
		}

		protected virtual void ApplyColorTransform (Pixbuf pixbuf)
		{
		}

		protected Point ImageCoordsToWindow (Point image)
		{
			if (this.Pixbuf == null)
				return Point.Zero;

			image = PixbufUtils.TransformOrientation (Pixbuf.Width, Pixbuf.Height, image, pixbuf_orientation);
			int x_offset = scaled_width < Allocation.Width ? (int)(Allocation.Width - scaled_width) / 2 : -XOffset;
			int y_offset = scaled_height < Allocation.Height ? (int)(Allocation.Height - scaled_height) / 2 : -YOffset;

			return new Point ((int) Math.Floor (image.X * (double) (scaled_width - 1) / (((int)pixbuf_orientation <= 4 ? Pixbuf.Width : Pixbuf.Height) - 1) + 0.5) + x_offset,
					  (int) Math.Floor (image.Y * (double) (scaled_height - 1) / (((int)pixbuf_orientation <= 4 ? Pixbuf.Height : Pixbuf.Width) - 1) + 0.5) + y_offset);
		}

		protected Rectangle ImageCoordsToWindow (Rectangle image)
		{
			if (this.Pixbuf == null)
				return Gdk.Rectangle.Zero;

			image = PixbufUtils.TransformOrientation (Pixbuf.Width, Pixbuf.Height, image, pixbuf_orientation);
			int x_offset = scaled_width < Allocation.Width ? (int)(Allocation.Width - scaled_width) / 2 : -XOffset;
			int y_offset = scaled_height < Allocation.Height ? (int)(Allocation.Height - scaled_height) / 2 : -YOffset;

			Gdk.Rectangle win = Gdk.Rectangle.Zero;
			win.X = (int) Math.Floor (image.X * (double) (scaled_width - 1) / (((int)pixbuf_orientation <= 4 ? Pixbuf.Width : Pixbuf.Height) - 1) + 0.5) + x_offset;
			win.Y = (int) Math.Floor (image.Y * (double) (scaled_height - 1) / (((int)pixbuf_orientation <= 4 ? Pixbuf.Height : Pixbuf.Width) - 1) + 0.5) + y_offset;
			win.Width = (int) Math.Floor ((image.X + image.Width) * (double) (scaled_width - 1) / (((int)pixbuf_orientation <= 4 ? Pixbuf.Width : Pixbuf.Height) - 1) + 0.5) - win.X + x_offset;
			win.Height = (int) Math.Floor ((image.Y + image.Height) * (double) (scaled_height - 1) / (((int)pixbuf_orientation <= 4 ? Pixbuf.Height : Pixbuf.Width) - 1) + 0.5) - win.Y + y_offset;

			return win;
		}
#endregion

#region container
		protected override void OnAdded (Gtk.Widget widget)
		{
			Put (widget, 0, 0);
		}

		protected override void OnRemoved (Gtk.Widget widget)
		{
			LayoutChild child = null;
			foreach (var c in children) {
				if (child.Widget == widget) {
					child = c;
					break;
				}
			}

			if (child != null) {
				widget.Unparent ();
				children.Remove (child);
			}
		}

		protected override void ForAll (bool include_internals, Gtk.Callback callback)
		{
			foreach (var child in children) 
				callback (child.Widget);
		}
#endregion

#region GtkWidgetry
		protected override void OnRealized ()
		{
			SetFlag (Gtk.WidgetFlags.Realized);
			GdkWindow = new Gdk.Window (ParentWindow,
						    new Gdk.WindowAttr { WindowType = Gdk.WindowType.Child,
									 X = Allocation.X,
									 Y = Allocation.Y,
									 Width = Allocation.Width,
									 Height = Allocation.Height,
									 Wclass = Gdk.WindowClass.InputOutput,
									 Visual = this.Visual,
									 Colormap = this.Colormap,
									 Mask = this.Events
									      | EventMask.ExposureMask
									      | EventMask.ButtonPressMask
									      | EventMask.ButtonReleaseMask
									      | EventMask.PointerMotionMask
									      | EventMask.PointerMotionHintMask
									      | EventMask.ScrollMask
									      | EventMask.KeyPressMask },
						     Gdk.WindowAttributesType.X | Gdk.WindowAttributesType.Y |
						     Gdk.WindowAttributesType.Visual | Gdk.WindowAttributesType.Colormap);

			GdkWindow.SetBackPixmap (null, false);
			GdkWindow.UserData = Handle;

			Style.Attach (GdkWindow);
			Style.SetBackground (GdkWindow, Gtk.StateType.Normal);

			foreach (var child in children)
				child.Widget.ParentWindow = GdkWindow;
		}

		protected override void OnMapped ()
		{
			SetFlag (Gtk.WidgetFlags.Mapped);

			foreach (var child in children)
				if (child.Widget.Visible && !child.Widget.IsMapped)
					child.Widget.Map ();
			GdkWindow.Show ();
		}

		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			requisition.Width = requisition.Height = 0;

			foreach (var child in children)
				child.Widget.SizeRequest ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			min_zoom = ComputeMinZoom (upscale);

			if (fit || zoom < MIN_ZOOM)
				zoom = MIN_ZOOM;
			// Since this affects the zoom_scale we should alert it
			EventHandler eh = ZoomChanged;
			if (eh != null)
				eh (this, System.EventArgs.Empty);

			ComputeScaledSize ();

			foreach (var child in children) {
				Gtk.Requisition req = child.Widget.ChildRequisition;
				child.Widget.SizeAllocate (new Gdk.Rectangle (child.X, child.Y, req.Width, req.Height));
			}

			if (IsRealized) {
				GdkWindow.MoveResize (allocation.X, allocation.Y, allocation.Width, allocation.Height);
			}

			Hadjustment.PageSize = Math.Min (scaled_width, allocation.Width);
			Hadjustment.PageIncrement = scaled_width * .9;
			Hadjustment.StepIncrement = 32;
			Hadjustment.Lower = 0;

			Vadjustment.PageSize = Math.Min (scaled_height, allocation.Height);
			Vadjustment.PageIncrement = scaled_height * .9;
			Vadjustment.StepIncrement = 32;
			Vadjustment.Lower = 0;

			if (XOffset > Hadjustment.Upper - Hadjustment.PageSize)
				ScrollTo ((int)(Hadjustment.Upper - Hadjustment.PageSize), YOffset, false);
			if (YOffset > Vadjustment.Upper - Vadjustment.PageSize)
				ScrollTo (XOffset, (int)(Vadjustment.Upper - Vadjustment.PageSize), false);

			base.OnSizeAllocated (allocation);

			if (fit)
				ZoomFit (upscale);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (evnt.Window != GdkWindow)
				return false;

			foreach (Rectangle area in evnt.Region.GetRectangles ())
			{
				var p_area = new Rectangle (Math.Max (0, area.X), Math.Max (0, area.Y),
						      Math.Min (Allocation.Width, area.Width), Math.Min (Allocation.Height, area.Height));
				if (p_area == Rectangle.Zero)
					continue;

				//draw synchronously if InterpType.Nearest or zoom 1:1
				if (Interpolation == InterpType.Nearest || zoom == 1.0) {
					PaintRectangle (p_area, Interpolation);
					continue;
				}
				
				//delay all other interpolation types
//				GLib.Idle.Add (...);

				PaintRectangle (p_area, InterpType.Nearest);
			}
			
			if (can_select)
				OnSelectionExposeEvent (evnt);

			return true;
		}

		protected override void OnSetScrollAdjustments (Gtk.Adjustment hadjustment, Gtk.Adjustment vadjustment)
		{
			if (hadjustment == null)
				hadjustment = new Gtk.Adjustment (0, 0, 0, 0, 0, 0);
			if (vadjustment == null)
				vadjustment = new Gtk.Adjustment (0, 0, 0, 0, 0, 0);
			bool need_change = false;
			if (this.hadjustment != hadjustment) {
				this.hadjustment = hadjustment;
				this.hadjustment.Upper = scaled_width;
				this.hadjustment.ValueChanged += HandleAdjustmentsValueChanged;
				need_change = true;
			}
			if (this.vadjustment != vadjustment) {
				this.vadjustment = vadjustment;
				this.vadjustment.Upper = scaled_height;
				this.vadjustment.ValueChanged += HandleAdjustmentsValueChanged;
				need_change = true;
			}

			if (need_change)
				HandleAdjustmentsValueChanged (this, EventArgs.Empty);
		}	

//		bool dragging = false;
//		int draganchor_x = 0;
//		int draganchor_y = 0;
		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			bool handled = false;
			if (!HasFocus)
				GrabFocus ();

			if (PointerMode == PointerMode.None)
				return false;

			if (can_select)
				handled |= OnSelectionButtonPressEvent (evnt);

			if (handled)
				return handled;

	//		if (dragging)
	//			return base.OnButtonPressEvent (evnt);

	//		switch (evnt.Button) {
	//		case 1:	
	//			dragging = true;
	//			draganchor_x = (int)evnt.X;
	//			draganchor_y = (int)evnt.Y;

	//			handled = true;
	//		default:
	//			break;
	//		}

			return handled || base.OnButtonPressEvent (evnt);
		}

		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			bool handled = false;

			if (can_select)
				handled |= OnSelectionButtonReleaseEvent (evnt);

			if (handled)
				return handled;

			return handled |= base.OnButtonReleaseEvent (evnt);
		}

		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			bool handled = false;

			if (can_select)
				handled |= OnSelectionMotionNotifyEvent (evnt);

			return handled || base.OnMotionNotifyEvent (evnt);

		}

		protected override bool OnScrollEvent (EventScroll evnt)
		{
			if ((evnt.State & ModifierType.ShiftMask) == 0) {//no shift, let's zoom
				ZoomAboutPoint ((evnt.Direction == ScrollDirection.Up || evnt.Direction == ScrollDirection.Right) ? ZOOM_FACTOR : 1.0 / ZOOM_FACTOR,
						 (int)evnt.X, (int)evnt.Y);
				return true;
			}

			int x_incr = (int)Hadjustment.PageIncrement / 4;
			int y_incr = (int)Vadjustment.PageIncrement / 4;
			if ((evnt.State & ModifierType.ControlMask) == 0) {//no control scroll
				ScrollBy ((evnt.Direction == ScrollDirection.Left) ? -x_incr : (evnt.Direction == ScrollDirection.Right) ? x_incr : 0,
					  (evnt.Direction == ScrollDirection.Up) ? -y_incr : (evnt.Direction == ScrollDirection.Down) ? y_incr : 0);
				return true;
			} else { //invert x and y for scrolling
				ScrollBy ((evnt.Direction == ScrollDirection.Up) ? -y_incr : (evnt.Direction == ScrollDirection.Down) ? y_incr : 0,
					  (evnt.Direction == ScrollDirection.Left) ? -x_incr : (evnt.Direction == ScrollDirection.Right) ? x_incr : 0);	
				return true;
			}
		}

		protected override bool OnKeyPressEvent (EventKey evnt)
		{
			if ((evnt.State & (ModifierType.Mod1Mask | ModifierType.ControlMask)) != 0)
				return base.OnKeyPressEvent (evnt);

			bool handled = true;
			int x, y;
			Gdk.ModifierType type;

			switch(evnt.Key) {
			case Gdk.Key.Up:
			case Gdk.Key.KP_Up:
			case Gdk.Key.k:
			case Gdk.Key.K:
				ScrollBy (0, -Vadjustment.StepIncrement);
				break;
			case Gdk.Key.Down:
			case Gdk.Key.KP_Down:
			case Gdk.Key.j:
			case Gdk.Key.J:
				ScrollBy (0, Vadjustment.StepIncrement);
				break;
			case Gdk.Key.Left:
			case Gdk.Key.KP_Left:
			case Gdk.Key.h:
			case Gdk.Key.H:
				ScrollBy (-Hadjustment.StepIncrement, 0);
				break;
			case Gdk.Key.Right:
			case Gdk.Key.KP_Right:
			case Gdk.Key.l:
			case Gdk.Key.L:
				ScrollBy (Hadjustment.StepIncrement, 0);
				break;
			case Gdk.Key.equal:
			case Gdk.Key.plus:
			case Gdk.Key.KP_Add:
				ZoomIn ();
				break;
			case Gdk.Key.minus:
			case Gdk.Key.KP_Subtract:
				ZoomOut ();
				break;
			case Gdk.Key.Key_0:
			case Gdk.Key.KP_0:
				ZoomFit ();
				break;
			case Gdk.Key.KP_1:
			case Gdk.Key.Key_1:
				GdkWindow.GetPointer (out x, out y, out type);
				DoZoom (1.0, true, x, y);
				break;
			case Gdk.Key.Key_2:
			case Gdk.Key.KP_2:
				GdkWindow.GetPointer (out x, out y, out type);
				DoZoom (2.0, true, x, y);
				break;
			default:
				handled = false;
				break;
			}
			
			return handled || base.OnKeyPressEvent (evnt);
		}
#endregion

#region private painting, zooming and misc 
		int XOffset { get; set;}
		int YOffset { get; set;}
		void DoZoom (double zoom, bool use_anchor, int x, int y)
		{
			fit = zoom == MIN_ZOOM;

			if (zoom == this.zoom)
				return;
			
			if (System.Math.Abs (this.zoom - zoom) < System.Double.Epsilon)
				return;

			if (zoom > MAX_ZOOM)
				zoom = MAX_ZOOM;
			else if (zoom < MIN_ZOOM)
				zoom = MIN_ZOOM;

			this.zoom = zoom;
			
			if (!use_anchor) {
				x = (int)Allocation.Width / 2;
				y = (int)Allocation.Height / 2;
			}

			int x_offset = scaled_width < Allocation.Width ? (int)(Allocation.Width - scaled_width) / 2 : -XOffset;
			int y_offset = scaled_height < Allocation.Height ? (int)(Allocation.Height - scaled_height) / 2 : -YOffset;
			double x_anchor = (double)(x - x_offset) / (double)scaled_width;
			double y_anchor = (double)(y - y_offset) / (double)scaled_height;
			ComputeScaledSize ();

			AdjustmentsChanged -= ScrollToAdjustments;
			if (scaled_width < Allocation.Width)
				Hadjustment.Value = XOffset = 0;
			else
				Hadjustment.Value = XOffset = Clamp ((int)(x_anchor * scaled_width - x), 0, (int)(Hadjustment.Upper - Hadjustment.PageSize));
			if (scaled_height < Allocation.Height)
				Vadjustment.Value = YOffset = 0;
			else
				Vadjustment.Value = YOffset = Clamp ((int)(y_anchor * scaled_height - y), 0, (int)(Vadjustment.Upper - Vadjustment.PageSize));
			AdjustmentsChanged += ScrollToAdjustments;

			EventHandler eh = ZoomChanged;
			if (eh != null)
				eh (this, EventArgs.Empty);

			QueueDraw ();
		}

		void PaintBackground (Rectangle backgound, Rectangle area)
		{
			GdkWindow.DrawRectangle (Style.BackgroundGCs [(int)StateType.Normal], true, area);
		}

		void PaintRectangle (Rectangle area, InterpType interpolation)
		{
			int x_offset = scaled_width < Allocation.Width ? (int)(Allocation.Width - scaled_width) / 2 : -XOffset;
			int y_offset = scaled_height < Allocation.Height ? (int)(Allocation.Height - scaled_height) / 2 : -YOffset;
			//Draw background
			if (y_offset > 0) 	//Top
				PaintBackground (new Rectangle (0, 0, Allocation.Width, y_offset), area);
			if (x_offset > 0) 	//Left
				PaintBackground (new Rectangle (0, y_offset, x_offset, (int)scaled_height), area);
			if (x_offset >= 0)	//Right
				PaintBackground (new Rectangle (x_offset + (int)scaled_width, y_offset, Allocation.Width - x_offset - (int)scaled_width, (int)scaled_height), area);
			if (y_offset >= 0)	//Bottom
				PaintBackground (new Rectangle (0, y_offset + (int)scaled_height, Allocation.Width, Allocation.Height - y_offset - (int)scaled_height), area);

			if (Pixbuf == null)
				return;

			area.Intersect (new Rectangle (x_offset, y_offset, (int)scaled_width, (int)scaled_height));

			if (area.Width <= 0  || area.Height <= 0)
				return;

			//Short circuit for 1:1 zoom
			if (zoom == 1.0 &&
			    !Pixbuf.HasAlpha &&
			    Pixbuf.BitsPerSample == 8 &&
			    pixbuf_orientation == PixbufOrientation.TopLeft) {
				GdkWindow.DrawPixbuf (Style.BlackGC,
						      Pixbuf,
						      area.X - x_offset, area.Y - y_offset,
						      area.X, area.Y,
						      area.Width, area.Height,
						      RgbDither.Max,
						      area.X - x_offset, area.Y - y_offset);
				return;
			}

			Rectangle pixbuf_area = PixbufUtils.TransformOrientation ((int)scaled_width,
										  (int)scaled_height,
										  new Rectangle ((area.X - x_offset),
												 (area.Y - y_offset),
												 area.Width,
												 area.Height),
										  PixbufUtils.ReverseTransformation (pixbuf_orientation));
			using (Pixbuf temp_pixbuf = new Pixbuf (Colorspace.Rgb, false, 8, pixbuf_area.Width, pixbuf_area.Height)) {
				if (Pixbuf.HasAlpha)
					temp_pixbuf.Fill (0x00000000);

				Pixbuf.CompositeColor (temp_pixbuf,
						       0, 0,
						       pixbuf_area.Width, pixbuf_area.Height,
						       -pixbuf_area.X, -pixbuf_area.Y,
						       zoom, zoom,
						       zoom == 1.0 ? InterpType.Nearest : interpolation, 255,
						       pixbuf_area.X, pixbuf_area.Y,
						       CheckPattern.CheckSize, CheckPattern.Color1, CheckPattern.Color2);


				ApplyColorTransform (temp_pixbuf);

				using (var dest_pixbuf = PixbufUtils.TransformOrientation (temp_pixbuf, pixbuf_orientation)) {
					GdkWindow.DrawPixbuf (Style.BlackGC,
							      dest_pixbuf,
							      0, 0,
							      area.X, area.Y,
							      area.Width, area.Height,
							      RgbDither.Max,
							      area.X - x_offset, area.Y - y_offset);
				}
			}
		}

		uint scaled_width, scaled_height;
		void ComputeScaledSize ()
		{
			if (Pixbuf == null)
				scaled_width = scaled_height = 0;
			else {
				double width;
				double height;
				if ((int)pixbuf_orientation <= 4 ) { //TopLeft, TopRight, BottomRight, BottomLeft
					width = Pixbuf.Width;
					height = Pixbuf.Height;
				} else {			//LeftTop, RightTop, RightBottom, LeftBottom
					width = Pixbuf.Height;
					height = Pixbuf.Width;
				}
				scaled_width = (uint)Math.Floor (width * Zoom + .5);
				scaled_height = (uint)Math.Floor (height * Zoom + .5);
			}

			Hadjustment.Upper = scaled_width;
			Vadjustment.Upper = scaled_height;
		}

		event EventHandler AdjustmentsChanged;
		void HandleAdjustmentsValueChanged (object sender, EventArgs e)
		{
			EventHandler eh = AdjustmentsChanged;
			if (eh != null)
				eh (this, EventArgs.Empty);
		}

		void ScrollToAdjustments (object sender, EventArgs e)
		{
			ScrollTo ((int)Hadjustment.Value, (int)Vadjustment.Value, false);
		}

		void ScrollTo (int x, int y, bool change_adjustments)
		{
			x = Clamp (x, 0, (int)(Hadjustment.Upper - Hadjustment.PageSize));
			y = Clamp (y, 0, (int)(Vadjustment.Upper - Vadjustment.PageSize));

			int xof = x - XOffset;
			int yof = y - YOffset;
			XOffset = x;
			YOffset = y;

			if (IsRealized) {
				GdkWindow.Scroll (-xof, -yof);
				GdkWindow.ProcessUpdates (true);
			}

			if (change_adjustments) {
				AdjustmentsChanged -= ScrollToAdjustments;
				Hadjustment.Value = XOffset;
				Vadjustment.Value = YOffset;
				AdjustmentsChanged += ScrollToAdjustments;
			}
		}

		void ScrollBy (double x, double y)
		{
			ScrollTo ((int)(XOffset + x), (int)(YOffset + y), true);
		}

		static int Clamp (int value, int min, int max)
		{
			return Math.Min (Math.Max (value, min), max);
		}

		double ComputeMinZoom (bool upscale)
		{
			if (Pixbuf == null)
				return 0.1;

			double width;
			double height;
			if ((int)pixbuf_orientation <= 4 ) { //TopLeft, TopRight, BottomRight, BottomLeft
				width = Pixbuf.Width;
				height = Pixbuf.Height;
			} else {			//LeftTop, RightTop, RightBottom, LeftBottom
				width = Pixbuf.Height;
				height = Pixbuf.Width;
			}
			if (upscale)
				return Math.Min ((double)Allocation.Width / width,
						 (double)Allocation.Height / height);
			return Math.Min (1.0,
					 Math.Min ((double)Allocation.Width / width,
						   (double)Allocation.Height / height));
		}
#endregion

#region children
		class LayoutChild {
			Gtk.Widget widget;
			public Gtk.Widget Widget {
				get { return widget; }
			}

			public int X {get; set; }
			public int Y {get; set; }

			public LayoutChild (Gtk.Widget widget, int x, int y)
			{
				this.widget = widget;
				X = x;
				Y = y;
			}
		}

		LayoutChild GetChild (Gtk.Widget widget)
		{
			foreach (var child in children)
				if (child.Widget == widget)
					return child;
			return null;
		}
#endregion

#region selection
		bool OnSelectionExposeEvent (EventExpose evnt)
		{
			if (selection == Rectangle.Zero)
				return false;

			Rectangle win_selection = ImageCoordsToWindow (selection);
			using (var evnt_region = evnt.Region.Copy ()) {
				using (Region r = new Region ()) {
					r.UnionWithRect (win_selection);
					evnt_region.Subtract (r);
				}

				using (Cairo.Context ctx = CairoHelper.Create (GdkWindow)) {
					ctx.SetSourceRGBA (.5, .5, .5, .7);
					CairoHelper.Region (ctx, evnt_region);
					ctx.Fill ();
				}
			}
			return true;
		}

		enum DragMode {
			None,
			Move,
			Extend,
		}

		const int SELECTION_SNAP_DISTANCE = 8;
		DragMode GetDragMode (int x, int y)
		{
			Rectangle win_selection = ImageCoordsToWindow (selection);
			if (Rectangle.Inflate (win_selection, -SELECTION_SNAP_DISTANCE, -SELECTION_SNAP_DISTANCE).Contains (x, y))
				return DragMode.Move;
			if (Rectangle.Inflate (win_selection, SELECTION_SNAP_DISTANCE, SELECTION_SNAP_DISTANCE).Contains (x, y))
				return DragMode.Extend;
			return DragMode.None;
		}

		bool is_dragging_selection = false;
		bool fixed_height = false;
		bool fixed_width = false;
		bool is_moving_selection = false;
		Point selection_anchor = Point.Zero;
		bool OnSelectionButtonPressEvent (EventButton evnt)
		{
			if (evnt.Button != 1)
				return false;

			if (evnt.Type == EventType.TwoButtonPress) {
				is_dragging_selection = false;
				is_moving_selection = false;
				return false;
			}
			
			Point img = WindowCoordsToImage (new Point ((int)evnt.X, (int)evnt.Y));
			switch (GetDragMode ((int)evnt.X, (int)evnt.Y)) {
				case DragMode.None:
					is_dragging_selection = true;
					PointerMode = PointerMode.Select;
					Selection = Rectangle.Zero;
					selection_anchor = img;
					break;
				case DragMode.Extend:
					Rectangle win_sel = ImageCoordsToWindow (Selection);
					is_dragging_selection = true;
					if (Math.Abs (win_sel.X - evnt.X) < SELECTION_SNAP_DISTANCE &&
					    Math.Abs (win_sel.Y - evnt.Y) < SELECTION_SNAP_DISTANCE) {	 			//TopLeft
						selection_anchor = new Point (Selection.X + Selection.Width, Selection.Y + Selection.Height);
					} else if (Math.Abs (win_sel.X + win_sel.Width - evnt.X) < SELECTION_SNAP_DISTANCE &&
						   Math.Abs (win_sel.Y - evnt.Y) < SELECTION_SNAP_DISTANCE) { 			//TopRight
						selection_anchor = new Point (Selection.X, Selection.Y + Selection.Height);
					} else if (Math.Abs (win_sel.X - evnt.X) < SELECTION_SNAP_DISTANCE &&
						   Math.Abs (win_sel.Y + win_sel.Height - evnt.Y) < SELECTION_SNAP_DISTANCE) {	//BottomLeft
						selection_anchor = new Point (Selection.X + Selection.Width, Selection.Y);
					} else if (Math.Abs (win_sel.X + win_sel.Width - evnt.X) < SELECTION_SNAP_DISTANCE &&
						   Math.Abs (win_sel.Y + win_sel.Height - evnt.Y) < SELECTION_SNAP_DISTANCE) {	//BottomRight
						selection_anchor = new Point (Selection.X, Selection.Y);
					} else if (Math.Abs (win_sel.X - evnt.X) < SELECTION_SNAP_DISTANCE) {			//Left
						selection_anchor = new Point (Selection.X + Selection.Width, Selection.Y);
						fixed_height = true;
					} else if (Math.Abs (win_sel.X + win_sel.Width - evnt.X) < SELECTION_SNAP_DISTANCE) {	//Right
						selection_anchor = new Point (Selection.X, Selection.Y);
						fixed_height = true;
					} else if (Math.Abs (win_sel.Y - evnt.Y) < SELECTION_SNAP_DISTANCE) {			//Top
						selection_anchor = new Point (Selection.X, Selection.Y + Selection.Height);
						fixed_width = true;
					} else if (Math.Abs (win_sel.Y + win_sel.Height - evnt.Y) < SELECTION_SNAP_DISTANCE) {	//Bottom
						selection_anchor = new Point (Selection.X, Selection.Y);
						fixed_width = true;
					} else {
						fixed_width = fixed_height = false;
						is_dragging_selection = false;
					}
						
					break;
				case DragMode.Move:
					is_moving_selection = true;
					selection_anchor = img;
					SelectionSetPointer ((int)evnt.X, (int)evnt.Y);
					break;
			}

			return true;
		}

		bool OnSelectionButtonReleaseEvent (EventButton evnt)
		{
			if (evnt.Button != 1)
				return false;

			is_dragging_selection = false;
			is_moving_selection = false;
			fixed_width = fixed_height = false;

			SelectionSetPointer ((int)evnt.X, (int)evnt.Y);
			return true;
		}

		void SelectionSetPointer (int x, int y)
		{
			if (is_moving_selection)
				GdkWindow.Cursor = new Cursor (CursorType.Fleur);
			else {
				switch (GetDragMode (x, y)) {
				case DragMode.Move:
					GdkWindow.Cursor = new Cursor (CursorType.Hand1);
					break;
				default:
					GdkWindow.Cursor = null;
					break;
				case DragMode.Extend:
					Rectangle win_sel = ImageCoordsToWindow (Selection);
					if (Math.Abs (win_sel.X - x) < SELECTION_SNAP_DISTANCE &&
					    Math.Abs (win_sel.Y - y) < SELECTION_SNAP_DISTANCE) {	 			//TopLeft
						GdkWindow.Cursor = new Cursor (CursorType.TopLeftCorner);
					} else if (Math.Abs (win_sel.X + win_sel.Width - x) < SELECTION_SNAP_DISTANCE &&
						   Math.Abs (win_sel.Y - y) < SELECTION_SNAP_DISTANCE) { 			//TopRight
						GdkWindow.Cursor = new Cursor (CursorType.TopRightCorner);
					} else if (Math.Abs (win_sel.X - x) < SELECTION_SNAP_DISTANCE &&
						   Math.Abs (win_sel.Y + win_sel.Height - y) < SELECTION_SNAP_DISTANCE) {	//BottomLeft
						GdkWindow.Cursor = new Cursor (CursorType.BottomLeftCorner);
					} else if (Math.Abs (win_sel.X + win_sel.Width - x) < SELECTION_SNAP_DISTANCE &&
						   Math.Abs (win_sel.Y + win_sel.Height - y) < SELECTION_SNAP_DISTANCE) {	//BottomRight
						GdkWindow.Cursor = new Cursor (CursorType.BottomRightCorner);
					} else if (Math.Abs (win_sel.X - x) < SELECTION_SNAP_DISTANCE) {			//Left
						GdkWindow.Cursor = new Cursor (CursorType.LeftSide);
					} else if (Math.Abs (win_sel.X + win_sel.Width - x) < SELECTION_SNAP_DISTANCE) {	//Right
						GdkWindow.Cursor = new Cursor (CursorType.RightSide);
					} else if (Math.Abs (win_sel.Y - y) < SELECTION_SNAP_DISTANCE) {			//Top
						GdkWindow.Cursor = new Cursor (CursorType.TopSide);
					} else if (Math.Abs (win_sel.Y + win_sel.Height - y) < SELECTION_SNAP_DISTANCE) {	//Bottom
						GdkWindow.Cursor = new Cursor (CursorType.BottomSide);
					}
					break;
				}
			}

			
		}

		const int SELECTION_THRESHOLD = 5;
		bool OnSelectionMotionNotifyEvent (EventMotion evnt)
		{
			int x, y;
			ModifierType mod;

			if (evnt.IsHint)
				GdkWindow.GetPointer (out x, out y, out mod);
			else {
				x = (int)evnt.X;
				y = (int)evnt.Y;
			}


			Point img = WindowCoordsToImage (new Point (x, y));
			if (is_dragging_selection) {
				Point win_anchor = ImageCoordsToWindow (selection_anchor);
				if (Selection == Rectangle.Zero &&
				    Math.Abs (evnt.X - win_anchor.X) < SELECTION_THRESHOLD &&
				    Math.Abs (evnt.Y - win_anchor.Y) < SELECTION_THRESHOLD) {
					SelectionSetPointer (x, y);
					return true;
				}
	
				
				if (selection_xy_ratio == 0)
					Selection = new Rectangle (fixed_width ? Selection.X : Math.Min (selection_anchor.X, img.X),
								   fixed_height ? Selection.Y : Math.Min (selection_anchor.Y, img.Y),
								   fixed_width ? Selection.Width : Math.Abs (selection_anchor.X - img.X),
								   fixed_height ? Selection.Height : Math.Abs (selection_anchor.Y - img.Y));

				else
					Selection = ConstrainSelection (new Rectangle (Math.Min (selection_anchor.X, img.X),
										       Math.Min (selection_anchor.Y, img.Y),
										       Math.Abs (selection_anchor.X - img.X),
										       Math.Abs (selection_anchor.Y - img.Y)),
									fixed_width, fixed_height);

				SelectionSetPointer (x, y);
				return true;
			}

			if (is_moving_selection) {
				Selection = new Rectangle (Clamp (Selection.X + img.X - selection_anchor.X, 0, Pixbuf.Width - Selection.Width),
							   Clamp (Selection.Y + img.Y - selection_anchor.Y, 0, Pixbuf.Height - Selection.Height),
							   Selection.Width, Selection.Height);
				selection_anchor = img;
				SelectionSetPointer (x, y);
				return true;
			}

			SelectionSetPointer (x, y);
			return true;
		}

		Rectangle ConstrainSelection (Rectangle sel, bool fixed_width, bool fixed_height)
		{
			double constrain = selection_xy_ratio;
			if ((double)sel.Width > (double)sel.Height && selection_xy_ratio < 1 ||
			    (double)sel.Width < (double)sel.Height && selection_xy_ratio > 1)
				constrain = 1.0 / constrain;


			double ratio = (double)sel.Width / (double)sel.Height;
			int height = sel.Height;
			int width = sel.Width;
			if (ratio > constrain) {
				height = (int)((double)sel.Width / constrain);
				if (height > Pixbuf.Height) {
					height = sel.Height;
					width = (int)(height * constrain);
				}
			} else {
				width = (int)(height * constrain);
				if (width > Pixbuf.Width) {
					width = sel.Width;
					height = (int)((double)width / constrain);
				}
			}

			return new Rectangle (sel.X + width < Pixbuf.Width ? sel.X : Pixbuf.Width - width,
					      sel.Y + height < Pixbuf.Height ? sel.Y : Pixbuf.Height - height,
					      width, height);
		}
#endregion
	}
}
