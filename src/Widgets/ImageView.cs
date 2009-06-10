//
// FSpot.Widgets.ImageView.cs
//
// Author(s):
//	Stephane Delcroix  <stephane@delcroix.org>
//
// This is free software. See COPYING for details.
//

using System;

using Gtk;
using Gdk;

namespace FSpot.Widgets
{
	public class ImageView : Layout
	{
		public static double ZOOM_FACTOR = 1.1;

		protected double max_zoom = 10.0;
		protected double MAX_ZOOM {
			get { return max_zoom; }
		}

		protected double min_zoom = 0.1;
		protected double MIN_ZOOM {
			get { return min_zoom; }
		}

		public ImageView () : base (null, null)
		{
		}

		Pixbuf pixbuf;
		public Pixbuf Pixbuf {
			get { return pixbuf; } 
			set {
				pixbuf = value;
				if (pixbuf == null)
					min_zoom = 0.1;
				else {
					min_zoom = Math.Min (1.0,
						Math.Min ((double)Allocation.Width / (double)Pixbuf.Width,
						(double)Allocation.Height / (double)Pixbuf.Height));
				}

				UpdateScaledSize ();

				//scroll_to_view
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
				QueueDraw ();
			} 
		}

		public PointerMode PointerMode {
			get { throw new NotImplementedException ();} 
			set { throw new NotImplementedException ();} 
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

		int XOffset { get; set;}
		int YOffset { get; set;}

		[Obsolete ("use the Zoom Property")]
		public void GetZoom (out double zoomx, out double zoomy)
		{
			zoomx = zoomy = Zoom;
		}

		[Obsolete ("use the Zoom Property, or ZoomAboutPoint method")]
		public void SetZoom (double zoom_x, double zoom_y)
		{
			Zoom = zoom_x;
		}

		void DoZoom (double zoom, bool use_anchor, int x, int y)
		{
			if (zoom == this.zoom)
				return;

			if (zoom > MAX_ZOOM)
				zoom = MAX_ZOOM;
			else if (zoom < MIN_ZOOM)
				zoom = MIN_ZOOM;

			double oldzoom = this.zoom;
			this.zoom = zoom;

			UpdateScaledSize ();

			EventHandler eh = ZoomChanged;
			if (eh != null)
				eh (this, EventArgs.Empty);
			QueueDraw ();
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
			int x, y;
			int width, height;
	
			if (this.Pixbuf == null)
				return Gdk.Rectangle.Zero;

			int x_offset = (int)Width < Allocation.Width ? (Allocation.Width - (int)Width) / 2 : -XOffset;
			int y_offset = (int)Height < Allocation.Height ? (Allocation.Height - (int)Height) / 2 : -YOffset;

			Gdk.Rectangle win = Gdk.Rectangle.Zero;
			win.X = (int) Math.Floor (image.X * (double) (Width - 1) / (this.Pixbuf.Width - 1) + 0.5) + x_offset;
			win.Y = (int) Math.Floor (image.Y * (double) (Height - 1) / (this.Pixbuf.Height - 1) + 0.5) + y_offset;
			win.Width = (int) Math.Floor ((image.X + image.Width) * (double) (Width - 1) / (this.Pixbuf.Width - 1) + 0.5) - win.X + x_offset;
			win.Height = (int) Math.Floor ((image.Y + image.Height) * (double) (Height - 1) / (this.Pixbuf.Height - 1) + 0.5) - win.Y + y_offset;
	
			return win;
		}

		[Obsolete ("use the Selection Property")]
		public bool GetSelection (out int x, out int y, out int width, out int height)
		{
			if (selection == Rectangle.Zero) {
				x = y = width = height = 0;
				return false;
			}

			x = Selection.X;
			y = Selection.Y;
			width = Selection.Width;
			height = Selection.Height;
			return true;
		}

		[Obsolete ("set the Selection property to Gdk.Rectangle.Zero instead")]
		public void UnsetSelection () 
		{
			Selection = Gdk.Rectangle.Zero;
		}

		public event EventHandler ZoomChanged;
		public event EventHandler SelectionChanged;

		void PaintBackground (Rectangle backgound, Rectangle area)
		{
		}

		void PaintRectangle (Rectangle area, InterpType interpolation)
		{
			int x_offset = (int)Width < Allocation.Width ? (Allocation.Width - (int)Width) / 2 : -XOffset;
			int y_offset = (int)Height < Allocation.Height ? (Allocation.Height - (int)Height) / 2 : -YOffset;

			//Draw background
			if (y_offset > 0) 	//Top
				PaintBackground (new Rectangle (0, 0, Allocation.Width, y_offset), area);
			if (x_offset > 0) 	//Left
				PaintBackground (new Rectangle (0, y_offset, x_offset, (int)Height), area);
			if (x_offset >= 0)	//Right
				PaintBackground (new Rectangle (x_offset + (int)Width, y_offset, Allocation.Width - x_offset - (int)Width, (int)Height), area);
			if (y_offset >= 0)	//Bottom
				PaintBackground (new Rectangle (0, y_offset + (int)Height, Allocation.Width, Allocation.Height - y_offset - (int)Height), area);

			if (Pixbuf == null)
				return;

			area.Intersect (new Rectangle (x_offset, y_offset, (int)Width, (int)Height));

			//Short circuit for 1:1 zoom
			if (zoom == 1.0 &&
			    !Pixbuf.HasAlpha &&
			    Pixbuf.BitsPerSample == 8) {
				BinWindow.DrawPixbuf (Style.BlackGC,
						      Pixbuf,
						      area.X, area.Y,
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

				BinWindow.DrawPixbuf (Style.BlackGC,
						      temp_pixbuf,
						      0, 0,
						      area.X, area.Y,
						      area.Width, area.Height,
						      RgbDither.Max,
						      area.X - x_offset, area.Y - y_offset);
			}
		}

		protected override void OnSizeAllocated (Rectangle allocation)
		{
			if (Pixbuf == null)
				min_zoom = 0.1;
			else {
				min_zoom = Math.Min (1.0,
					Math.Min ((double)allocation.Width / (double)Pixbuf.Width,
					(double)allocation.Height / (double)Pixbuf.Height));
			}

			if (zoom < min_zoom)
				zoom = min_zoom;
			// Since this affects the zoom_scale we should alert it
			EventHandler eh = ZoomChanged;
			if (eh != null)
				eh (this, System.EventArgs.Empty);

			base.OnSizeAllocated (allocation);
	
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (evnt == null)
				return true;

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
			return base.OnScrollEvent (evnt);
		}

		void UpdateScaledSize ()
		{
			uint scaled_width, scaled_height;
			if (Pixbuf != null) {
				scaled_width = (uint)Math.Floor (Pixbuf.Width * Zoom + .5);
				scaled_height = (uint)Math.Floor (Pixbuf.Height * Zoom + .5);
			} else {
				scaled_width = scaled_height = 0;
			}
			Width = scaled_width;
			Height = scaled_height;
		}
	}
}
