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

namespace FSpot.Widgets
{
	public class ImageView : Container
	{

#region public API
		public ImageView (Adjustment hadjustment, Adjustment vadjustment) : base ()
		{
			OnSetScrollAdjustments (hadjustment, vadjustment);
			children = new List<LayoutChild> ();
			AdjustmentsChanged += ScrollToAdjustments;
			//DoubleBuffered = false;
		}

		public ImageView () : this (null, null)
		{
		}

		Pixbuf pixbuf;
		public Pixbuf Pixbuf {
			get { return pixbuf; } 
			set {
				pixbuf = value;
				if (pixbuf == null)
					min_zoom = 0.1;
				else
					min_zoom = Math.Min (1.0,
						Math.Min ((double)Allocation.Width / (double)Pixbuf.Width,
						(double)Allocation.Height / (double)Pixbuf.Height));

				ComputeScaledSize ();
				AdjustmentsChanged -= ScrollToAdjustments;
				Hadjustment.Value = Vadjustment.Value = 0;
				XOffset = YOffset = 0;
				AdjustmentsChanged += ScrollToAdjustments;
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

		public PointerMode PointerMode {
			get { throw new NotImplementedException ();} 
			set { throw new NotImplementedException ();} 
		}

		Adjustment hadjustment;
		public Adjustment Hadjustment {
			get { return hadjustment; }
		}

		Adjustment vadjustment;
		public Adjustment Vadjustment {
			get { return vadjustment; }
		}

		Gdk.Rectangle selection = Rectangle.Zero;
		public Gdk.Rectangle Selection {
			get { return selection; }
			set { 
				if (value == selection)
					return;

				selection = value;

				EventHandler eh = SelectionChanged;
				if (eh != null)
					eh (this, EventArgs.Empty);
			}
		}

		public double SelectionXyRatio {
			get { throw new NotImplementedException ();} 
			set { throw new NotImplementedException ();} 
		}

		Cms.Transform transform;
		public Cms.Transform Transform {
			get { return transform; } 
			set { transform = value;} 
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
		public virtual double Zoom {
			get { return zoom; }
			set { DoZoom (value, false, 0, 0); }
		}

		public void ZoomAboutPoint (double zoom_increment, int x, int y)
		{
			DoZoom (zoom * zoom_increment, true, x, y);
		}	

		public Gdk.Point WindowCoordsToImage (Point win)
		{
			throw new NotImplementedException ();
		}

		public Gdk.Rectangle ImageCoordsToWindow (Gdk.Rectangle image)
		{
			if (this.Pixbuf == null)
				return Gdk.Rectangle.Zero;

			int x_offset = scaled_width < Allocation.Width ? (int)(Allocation.Width - scaled_width) / 2 : -XOffset;
			int y_offset = scaled_height < Allocation.Height ? (int)(Allocation.Height - scaled_height) / 2 : -YOffset;

			Gdk.Rectangle win = Gdk.Rectangle.Zero;
			win.X = (int) Math.Floor (image.X * (double) (scaled_width - 1) / (this.Pixbuf.Width - 1) + 0.5) + x_offset;
			win.Y = (int) Math.Floor (image.Y * (double) (scaled_height - 1) / (this.Pixbuf.Height - 1) + 0.5) + y_offset;
			win.Width = (int) Math.Floor ((image.X + image.Width) * (double) (scaled_width - 1) / (this.Pixbuf.Width - 1) + 0.5) - win.X + x_offset;
			win.Height = (int) Math.Floor ((image.Y + image.Height) * (double) (scaled_height - 1) / (this.Pixbuf.Height - 1) + 0.5) - win.Y + y_offset;
	
			return win;
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
			if (Pixbuf == null)
				min_zoom = 0.1;
			else
				min_zoom = Math.Min (1.0,
					Math.Min ((double)allocation.Width / (double)Pixbuf.Width,
					(double)allocation.Height / (double)Pixbuf.Height));

			if (zoom < min_zoom)
				zoom = min_zoom;
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

		bool dragging = false;
		int draganchor_x = 0;
		int draganchor_y = 0;
		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			Console.WriteLine ("OnButtonPressEvent {0}", evnt.Button);
			if (!HasFocus)
				GrabFocus ();

			if (dragging)
				return base.OnButtonPressEvent (evnt);

			switch (evnt.Button) {
			case 1:	
				dragging = true;
				draganchor_x = (int)evnt.X;
				draganchor_y = (int)evnt.Y;

				return true;
			default:
				break;
			}

			return base.OnButtonPressEvent (evnt);
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
			return base.OnScrollEvent (evnt);
		}


#endregion
#region private painting and misc 
		int XOffset { get; set;}
		int YOffset { get; set;}
		void DoZoom (double zoom, bool use_anchor, int x, int y)
		{
Console.WriteLine ("DoZoom {0} {1} {2} {3}", zoom, use_anchor, x, y);
			if (zoom == this.zoom)
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

			double x_anchor = (double)(XOffset + x) / (double)scaled_width;
			double y_anchor = (double)(YOffset + y) / (double)scaled_height;
			ComputeScaledSize ();

			Console.WriteLine ("offset {0} {1}", XOffset, YOffset);
			Console.WriteLine ("anchor {0} {1}", x_anchor, y_anchor);
			AdjustmentsChanged -= ScrollToAdjustments;
			Hadjustment.Value = XOffset = (int)(x_anchor * scaled_width - x); 
			Vadjustment.Value = YOffset = (int)(y_anchor * scaled_height - y); 
			AdjustmentsChanged += ScrollToAdjustments;
			Console.WriteLine ("offset {0} {1}", XOffset, YOffset);

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
Console.WriteLine ("PaintRectangle {0}", area);
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

			//Short circuit for 1:1 zoom
			if (zoom == 1.0 &&
			    !Pixbuf.HasAlpha &&
			    Pixbuf.BitsPerSample == 8) {
				GdkWindow.DrawPixbuf (Style.BlackGC,
						      Pixbuf,
						      area.X - x_offset, area.Y - y_offset,
						      area.X, area.Y,
						      area.Width, area.Height,
						      RgbDither.Max,
						      area.X - x_offset, area.Y - y_offset);
				return;
			}

			using (Pixbuf temp_pixbuf = new Pixbuf (Colorspace.Rgb, false, 8, area.Width, area.Height)) {
				if (Pixbuf.HasAlpha)
					temp_pixbuf.Fill (0x00000000);

				Pixbuf.CompositeColor (temp_pixbuf,
						       0, 0,
						       area.Width, area.Height,
						       -(area.X - x_offset), -(area.Y - y_offset),
						       zoom, zoom,
						       zoom == 1.0 ? InterpType.Nearest : interpolation, 255,
						       area.X - x_offset, area.Y - y_offset,
						       CheckPattern.CheckSize, CheckPattern.Color1, CheckPattern.Color2);

				GdkWindow.DrawPixbuf (Style.BlackGC,
						      temp_pixbuf,
						      0, 0,
						      area.X, area.Y,
						      area.Width, area.Height,
						      RgbDither.Max,
						      area.X - x_offset, area.Y - y_offset);
			}
		}

		uint scaled_width, scaled_height;
		void ComputeScaledSize ()
		{
			if (Pixbuf != null) {
				scaled_width = (uint)Math.Floor (Pixbuf.Width * Zoom + .5);
				scaled_height = (uint)Math.Floor (Pixbuf.Height * Zoom + .5);
			} else {
				scaled_width = scaled_height = 0;
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
			if (x < 0)
				x = 0;
			else if (x > Hadjustment.Upper - Hadjustment.PageSize) 
				x = (int)(Hadjustment.Upper - Hadjustment.PageSize);
			if (y < 0) 
				y = 0;
			else if (y > Vadjustment.Upper - Vadjustment.PageSize) 
				y = (int)(Vadjustment.Upper - Vadjustment.PageSize);

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

		void ScrollBy (int x, int y)
		{
			ScrollTo (XOffset + x, YOffset + y, true);
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
	}
}
