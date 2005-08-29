using Gdk;
using Gtk;
using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace FSpot {
public class ImageView : Layout {
	private Cms.Transform transform;

	[DllImport ("libfspot")]
	static extern IntPtr f_image_view_new ();

	[DllImport ("libgobject-2.0-0.dll")]
	static extern uint g_signal_connect_data (IntPtr obj, String name, SelectionChangedDelegate cb, int key, IntPtr p, int flags);

	public ImageView () : base (null, null)
	{
		Raw = f_image_view_new ();

		g_signal_connect_data (Raw, "selection_changed", new SelectionChangedDelegate (SelectionChangedCallback), 0,
				       IntPtr.Zero, 0);
	}


	public enum PointerModeType {
		None,
		Select,
		Scroll
	}

	[DllImport ("libfspot")]
	static extern PointerModeType f_image_view_get_pointer_mode  (IntPtr image_view);
	[DllImport ("libfspot")]
	static extern void f_image_view_set_pointer_mode (IntPtr image_view, PointerModeType mode);

	public PointerModeType PointerMode {
		get {
			return f_image_view_get_pointer_mode (Handle);
		}
		set {
			f_image_view_set_pointer_mode (Handle, value);
		}
	}


	[DllImport ("libfspot")]
	static extern void f_image_view_set_selection_xy_ratio  (IntPtr image_view, double selection_xy_ratio);
	[DllImport ("libfspot")]
	static extern double f_image_view_get_selection_xy_ratio (IntPtr image_view);

	public double SelectionXyRatio {
		get {
			return f_image_view_get_selection_xy_ratio (Handle);
		}
		set {
			f_image_view_set_selection_xy_ratio (Handle, value);
		}
	}


	[DllImport ("libfspot")]
	static extern bool f_image_view_get_selection (IntPtr image_view,
						       out int x_return, out int y_return,
						       out int width_return, out int height_return);

	// FIXME property?  Kinda sucky.
	public bool GetSelection (out int x, out int y, out int width, out int height)
	{
		return f_image_view_get_selection (Handle, out x, out y, out width, out height);
	}

	
	[DllImport ("libfspot")]
	static extern void f_image_view_unset_selection (IntPtr image_view);

	public void UnsetSelection ()
	{
		f_image_view_unset_selection (Handle);
	}

	[DllImport ("libfspoteog")]
	static extern void image_view_set_transparent_color (IntPtr view, out Gdk.Color color);

	public void SetTransparentColor (Gdk.Color color)
	{
		image_view_set_transparent_color (Handle, out color);
	} 

	[DllImport ("libfspoteog")]
	static extern void image_view_set_pixbuf (IntPtr view, IntPtr pixbuf);
	[DllImport ("libfspoteog")]
	static extern IntPtr image_view_get_pixbuf (IntPtr view);

	public Pixbuf Pixbuf {
		get {
			IntPtr raw_pixbuf = image_view_get_pixbuf (Handle);
			if (raw_pixbuf == IntPtr.Zero)
				return null;

			Pixbuf result = (Gdk.Pixbuf) GLib.Object.GetObject (raw_pixbuf, true);
			return result;
		}
		set {
			if (value == null)
				image_view_set_pixbuf (Handle, IntPtr.Zero);
			else
				image_view_set_pixbuf (Handle, value.Handle);
		}
	}

	[DllImport ("libfspoteog")]
	static extern void image_view_set_zoom (IntPtr view, double zoomx, double zoomy,
						bool have_anchor, int anchorx, int anchory);

	public void SetZoom (double zoom_x, double zoom_y)
	{
		double old_zoom_x, old_zoom_y;

		GetZoom (out old_zoom_x, out old_zoom_y);
		if (System.Math.Abs (old_zoom_y - zoom_y) > System.Double.Epsilon
		    || System.Math.Abs (old_zoom_x - zoom_x) > System.Double.Epsilon) {
			//System.Console.WriteLine ("{0} {1} zooming", zoom_x, zoom_y);
			image_view_set_zoom (Handle, zoom_x, zoom_y, false, 0, 0);
		}
	}

	public void SetZoom (double zoom_x, double zoom_y, int anchor_x, int anchor_y)
	{
		image_view_set_zoom (Handle, zoom_x, zoom_y, true, anchor_x, anchor_y);
	}


	[DllImport ("libfspoteog")]
	static extern void image_view_get_zoom (IntPtr view, out double zoomx, out double zoomy);

	public void GetZoom (out double zoomx, out double zoomy)
	{
		image_view_get_zoom (Handle, out zoomx, out zoomy);
	}


	[DllImport ("libfspoteog")]
	static extern void image_view_get_offsets_and_size (IntPtr view,
							    out int xofs_return, out int yofs_return,
							    out int scaled_width, out int scaled_height);

	public void GetOffsets (out int x_offset, out int y_offset, out int scaled_width, out int scaled_height)
	{
		image_view_get_offsets_and_size (Handle,
						 out x_offset, out y_offset, out scaled_width, out scaled_height);
	}

	public Gdk.Rectangle ImageCoordsToWindow (Gdk.Rectangle image)
	{
		int x, y;
		int width, height;

		if (this.Pixbuf == null)
			return Gdk.Rectangle.Zero;
		
		this.GetOffsets (out x, out y, out width, out height);

		Gdk.Rectangle win = Gdk.Rectangle.Zero;
		win.X = (int) Math.Floor (image.X * (double) (width - 1) / (this.Pixbuf.Width - 1) + 0.5) + x;
		win.Y = (int) Math.Floor (image.Y * (double) (height - 1) / (this.Pixbuf.Height - 1) + 0.5) + y;
		win.Width = (int) Math.Floor ((image.X + image.Width) * (double) (width - 1) / (this.Pixbuf.Width - 1) + 0.5) - win.X + x;
		win.Height = (int) Math.Floor ((image.Y + image.Height) * (double) (height - 1) / (this.Pixbuf.Height - 1) + 0.5) - win.Y + y;

		return win;
	}

	[DllImport ("libfspoteog")]
	static extern void image_view_set_display_brightness (IntPtr view, float display_brightness);

	public double DisplayBrightness
	{
		set {
			image_view_set_display_brightness (Handle, (float) value);
		}
	}


	[DllImport ("libfspoteog")]
	static extern void image_view_set_display_contrast (IntPtr view, float display_contrast);

	public double DisplayContrast
	{
		set {
			image_view_set_display_contrast (Handle, (float) value);
		}
	}

	[DllImport ("libfspoteog")]
	static extern void image_view_set_display_transform (IntPtr view, HandleRef transform);

	public Cms.Transform Transform {
		set {
			this.transform = value;
			if (value != null)
				image_view_set_display_transform (Handle, transform.Handle);
			else 
				image_view_set_display_transform (Handle, new HandleRef (value, IntPtr.Zero));
		}
		get {
			return transform;
		}
	}


	private delegate void SelectionChangedDelegate (IntPtr obj, IntPtr data);
	private static void SelectionChangedCallback (IntPtr raw, IntPtr unused_data)
	{
		ImageView view = GLib.Object.GetObject (raw, false) as ImageView;

		if (view.SelectionChanged != null)
			view.SelectionChanged ();
	}

	public delegate void SelectionChangedHandler ();
	public event SelectionChangedHandler SelectionChanged;
}
}
