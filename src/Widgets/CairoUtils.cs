using System;
using Cairo;
using System.Runtime.InteropServices;

namespace FSpot.Widgets {
	public class CairoUtils {
                [DllImport ("libcairo-2.dll")]
                static extern void cairo_user_to_device (IntPtr cr, ref double x, ref double y);
		
		static void UserToDevice (Graphics g, ref double x, ref double y)
		{
			cairo_user_to_device (g.Handle, ref x, ref y);
		}
		
		[DllImport("libgdk-2.0-0.dll")]
		extern static void gdk_cairo_set_source_pixbuf (IntPtr handle,
								IntPtr pixbuf,
								double        pixbuf_x,
								double        pixbuf_y);
		
		static void SetSourcePixbuf (Graphics g, Gdk.Pixbuf pixbuf, double x, double y)
		{
			gdk_cairo_set_source_pixbuf (g.Handle, pixbuf.Handle, x, y);
		}
		
		[DllImport("libgdk-2.0-0.dll")]
		static extern IntPtr gdk_cairo_create (IntPtr raw);
		
		public static Cairo.Graphics CreateDrawable (Gdk.Drawable drawable)
		{
			Cairo.Graphics g = new Cairo.Graphics (gdk_cairo_create (drawable.Handle));
			if (g == null) 
				throw new Exception ("Couldn't create Cairo Graphics!");
			
			return g;
		}
	}
}
