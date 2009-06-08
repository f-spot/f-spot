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

		public Gdk.InterpType Interpolation {
			get { throw new NotImplementedException ();} 
			set { throw new NotImplementedException ();} 
		}

		double zoom;
		public double Zoom {
			get { return zoom; }
			set { 
				if (value < MIN_ZOOM || value > MAX_ZOOM)
					return;
				zoom = value;
				EventHandler eh = ZoomChanged;
				if (eh != null)
					eh (this, EventArgs.Empty);
			}
		}

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

			throw new NotImplementedException ();
			
//			this.GetOffsets (out x, out y, out width, out height);
//	
//			Gdk.Rectangle win = Gdk.Rectangle.Zero;
//			win.X = (int) Math.Floor (image.X * (double) (width - 1) / (this.Pixbuf.Width - 1) + 0.5) + x;
//			win.Y = (int) Math.Floor (image.Y * (double) (height - 1) / (this.Pixbuf.Height - 1) + 0.5) + y;
//			win.Width = (int) Math.Floor ((image.X + image.Width) * (double) (width - 1) / (this.Pixbuf.Width - 1) + 0.5) - win.X + x;
//			win.Height = (int) Math.Floor ((image.Y + image.Height) * (double) (height - 1) / (this.Pixbuf.Height - 1) + 0.5) - win.Y + y;
//	
//			return win;
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

		[Obsolete ("drop this, this should be done automatically on pixbuf or allocation changed")]
		protected void UpdateMinZoom ()
		{
			if (Pixbuf == null)
				min_zoom = 0.1;
			else {
				min_zoom = Math.Min (1.0,
					Math.Min ((double)Allocation.Width / (double)Pixbuf.Width,
					(double)Allocation.Height / (double)Pixbuf.Height));
			}

			// Since this affects the zoom_scale we should alert it
			EventHandler eh = ZoomChanged;
			if (eh != null)
				eh (this, System.EventArgs.Empty);
		}

		public event EventHandler ZoomChanged;
		public event EventHandler SelectionChanged;

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			Console.WriteLine ("ImageView OnExposeEvent");

			if (evnt == null)
				return true;

			foreach (Rectangle rect in evnt.Region.GetRectangles ())
			{
				Console.WriteLine ("drawing a rect");
			}
			return true;
		}
	}
}
