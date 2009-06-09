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
			transparent_color = this.Style.BaseColors [(int)Gtk.StateType.Normal];
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

				//scroll_to_view
				QueueDraw ();
			} 
		}

		public int CheckSize {
			get { throw new NotImplementedException ();} 
			set { throw new NotImplementedException ();} 
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
		public double Zoom {
			get { return zoom; }
			set { 
				if (value < MIN_ZOOM || value > MAX_ZOOM)
					return;
				zoom = value;
				EventHandler eh = ZoomChanged;
				if (eh != null)
					eh (this, EventArgs.Empty);
				QueueDraw ();
			}
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

		public void ZoomAboutPoint (double zoom_increment, int x, int y)
		{
		}
		
		Gdk.Color transparent_color;
		public Gdk.Color TransparentColor {
			get { return transparent_color; }
			set { transparent_color = value; }
		}

		[Obsolete ("Use the TransparentColor property")]
		public void SetTransparentColor (Gdk.Color color)
		{
			TransparentColor = color;
		} 

		public void SetTransparentColor (string color) //format "#000000"
		{
			TransparentColor  = new Gdk.Color (
					Byte.Parse (color.Substring (1,2), System.Globalization.NumberStyles.AllowHexSpecifier),
					Byte.Parse (color.Substring (3,2), System.Globalization.NumberStyles.AllowHexSpecifier),
					Byte.Parse (color.Substring (5,2), System.Globalization.NumberStyles.AllowHexSpecifier)
			);
		}

		[Obsolete ("use the CheckSize Property instead")]
		public void SetCheckSize (int size)
		{
			CheckSize = size;
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

			int scaled_width, scaled_height;
			if (Pixbuf != null) {
				scaled_width = (int)Math.Floor (Pixbuf.Width * Zoom + .5);
				scaled_height = (int)Math.Floor (Pixbuf.Height * Zoom + .5);
			} else {
				scaled_width = scaled_height = 0;
			}

			int x_offset = scaled_width < Allocation.Width ? (Allocation.Width - scaled_width) / 2 : -XOffset;
			int y_offset = scaled_height < Allocation.Height ? (Allocation.Height - scaled_height) / 2 : -YOffset;

			Gdk.Rectangle win = Gdk.Rectangle.Zero;
			win.X = (int) Math.Floor (image.X * (double) (scaled_width - 1) / (this.Pixbuf.Width - 1) + 0.5) + x_offset;
			win.Y = (int) Math.Floor (image.Y * (double) (scaled_height - 1) / (this.Pixbuf.Height - 1) + 0.5) + y_offset;
			win.Width = (int) Math.Floor ((image.X + image.Width) * (double) (scaled_width - 1) / (this.Pixbuf.Width - 1) + 0.5) - win.X + x_offset;
			win.Height = (int) Math.Floor ((image.Y + image.Height) * (double) (scaled_height - 1) / (this.Pixbuf.Height - 1) + 0.5) - win.Y + y_offset;
	
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
			int scaled_width, scaled_height;
			if (Pixbuf != null) {
				scaled_width = (int)Math.Floor (Pixbuf.Width * Zoom + .5);
				scaled_height = (int)Math.Floor (Pixbuf.Height * Zoom + .5);
			} else {
				scaled_width = scaled_height = 0;
			}

			int x_offset = scaled_width < Allocation.Width ? (Allocation.Width - scaled_width) / 2 : -XOffset;
			int y_offset = scaled_height < Allocation.Height ? (Allocation.Height - scaled_height) / 2 : -YOffset;

			//Draw background
			if (y_offset > 0) 	//Top
				PaintBackground (new Rectangle (0, 0, Allocation.Width, y_offset), area);
			if (x_offset > 0) 	//Left
				PaintBackground (new Rectangle (0, y_offset, x_offset, scaled_height), area);
			if (x_offset >= 0)	//Right
				PaintBackground (new Rectangle (x_offset + scaled_width, y_offset, Allocation.Width - x_offset - scaled_width, scaled_height), area);
			if (y_offset >= 0)	//Bottom
				PaintBackground (new Rectangle (0, y_offset + scaled_height, Allocation.Width, Allocation.Height - y_offset - scaled_height), area);

			if (Pixbuf == null)
				return;

			area.Intersect (new Rectangle (x_offset, y_offset, scaled_width, scaled_height));

			//Short circuit for 1:1 zoom
			if (zoom == 1.0) {
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

				//FIXME: compute check pattern
				uint check_black = 0x00000000;
				uint check_dark = 0x00555555;
				int check_medium = 8;

				Pixbuf.CompositeColor (temp_pixbuf,
						       0, 0,
						       area.Width, area.Height,
						       -(area.X - x_offset), -(area.Y - y_offset),
						       zoom, zoom,
						       zoom == 1.0 ? InterpType.Nearest : interpolation, 255,
						       area.X - x_offset, area.Y - y_offset,
						       check_medium, check_black, check_dark);

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
	}
}
