/*
 *
 * Author(s)
 *
 *   Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details
 *
 */

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
		
		public static void SetSourcePixbuf (Graphics g, Gdk.Pixbuf pixbuf, double x, double y)
		{
			gdk_cairo_set_source_pixbuf (g.Handle, pixbuf.Handle, x, y);
		}

		[DllImport("libgdk-2.0-0.dll")]
		extern static void gdk_cairo_set_source_pixmap (IntPtr handle,
								IntPtr drawable,
								double x,
								double y);

		public static void SetSourceDrawable (Graphics g, Gdk.Drawable d, double x, double y)
		{
			try {
				gdk_cairo_set_source_pixmap (g.Handle, d.Handle, x, y);
			} catch (EntryPointNotFoundException) {
				int width, height;
				d.GetSize (out width, out height);
				XlibSurface surface = new XlibSurface (GdkUtils.GetXDisplay (d.Display), 
								       (IntPtr)GdkUtils.GetXid (d),
								       GdkUtils.GetXVisual (d.Visual),
								       width, height);
				
				SurfacePattern p = new SurfacePattern (surface);
				Matrix m = new Matrix ();
				m.Translate (-x, -y);
				p.Matrix = m;
				g.Source = p;
			}
		}		

		[DllImport("libgdk-2.0-0.dll")]
		static extern IntPtr gdk_cairo_create (IntPtr raw);
		
		public static Cairo.Graphics CreateContext (Gdk.Drawable drawable)
		{
			Cairo.Graphics g = new Cairo.Graphics (gdk_cairo_create (drawable.Handle));
			if (g == null) 
				throw new Exception ("Couldn't create Cairo Graphics!");
			
			return g;
		}

		[DllImport("libfspot")]
		static extern IntPtr f_pixbuf_to_cairo_surface (IntPtr handle);

		public static Surface CreateSurface (Gdk.Pixbuf pixbuf)
		{
			IntPtr surface = f_pixbuf_to_cairo_surface (pixbuf.Handle);
			return Surface.LookupExternalSurface (surface);
		}
	}
}
