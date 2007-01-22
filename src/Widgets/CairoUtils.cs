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
		
		static void UserToDevice (Context ctx, ref double x, ref double y)
		{
			cairo_user_to_device (ctx.Handle, ref x, ref y);
		}
		
		[DllImport("libgdk-2.0-0.dll")]
		extern static void gdk_cairo_set_source_pixbuf (IntPtr handle,
								IntPtr pixbuf,
								double        pixbuf_x,
								double        pixbuf_y);
		
		public static void SetSourcePixbuf (Context ctx, Gdk.Pixbuf pixbuf, double x, double y)
		{
			gdk_cairo_set_source_pixbuf (ctx.Handle, pixbuf.Handle, x, y);
		}

		[DllImport("libgdk-2.0-0.dll")]
		extern static void gdk_cairo_set_source_pixmap (IntPtr handle,
								IntPtr drawable,
								double x,
								double y);

		public static void SetSourceDrawable (Context ctx, Gdk.Drawable d, double x, double y)
		{
			try {
				gdk_cairo_set_source_pixmap (ctx.Handle, d.Handle, x, y);
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
				ctx.Source = p;
			}
		}		

		[DllImport("libgdk-2.0-0.dll")]
		static extern IntPtr gdk_cairo_create (IntPtr raw);
		
		public static Cairo.Context CreateContext (Gdk.Drawable drawable)
		{
			Cairo.Context ctx = new Cairo.Context (gdk_cairo_create (drawable.Handle));
			if (ctx == null) 
				throw new Exception ("Couldn't create Cairo Graphics!");
			
			return ctx;
		}

		[DllImport("libfspot")]
		static extern IntPtr f_pixbuf_to_cairo_surface (IntPtr handle);

		public static MemorySurface CreateSurface (Gdk.Pixbuf pixbuf)
		{
			IntPtr surface = f_pixbuf_to_cairo_surface (pixbuf.Handle);
			return new MemorySurface (surface);
		}

		public static Surface CreateSurface (Gdk.Drawable d)
		{
			int width, height;
			d.GetSize (out width, out height);
			XlibSurface surface = new XlibSurface (GdkUtils.GetXDisplay (d.Display), 
							       (IntPtr)GdkUtils.GetXid (d),
							       GdkUtils.GetXVisual (d.Visual),
							       width, height);
			return surface;
		}
		
		public static Surface CreateGlitzSurface (Gdk.Drawable d)
		{

			Console.WriteLine ("XvisID: " + GdkUtils.GetXVisualId (d.Visual));
			IntPtr fmt = NDesk.Glitz.GlitzAPI.glitz_glx_find_drawable_format_for_visual (GdkUtils.GetXDisplay (d.Display), 
												     d.Screen.Number, 
												     GdkUtils.GetXVisualId (d.Visual));
			
			Console.WriteLine ("fmt: " + fmt);
			
			uint w = 100, h = 100;
			IntPtr glitz_drawable = NDesk.Glitz.GlitzAPI.glitz_glx_create_drawable_for_window (GdkUtils.GetXDisplay (d.Display),
													   d.Screen.Number, 
													   fmt,      
													   GdkUtils.GetXid (d), w, h);
			
			NDesk.Glitz.Drawable ggd = new NDesk.Glitz.Drawable (glitz_drawable);
			IntPtr glitz_format = ggd.FindStandardFormat (NDesk.Glitz.FormatName.ARGB32);
			
			NDesk.Glitz.Surface ggs = new NDesk.Glitz.Surface (ggd, glitz_format, 100, 100, 0, IntPtr.Zero);
			Console.WriteLine (ggd.Features);
			bool doublebuffer = false;
			ggs.Attach (ggd, doublebuffer ? NDesk.Glitz.DrawableBuffer.BackColor : NDesk.Glitz.DrawableBuffer.FrontColor);
			
			//GlitzAPI.glitz_drawable_destroy (glitz_drawable);
			GlitzSurface gs = new GlitzSurface (ggs.Handle);
			
			return gs;
		}

		[DllImport("libfspot")]
		static extern IntPtr f_pixbuf_from_cairo_surface (IntPtr handle);
		/*
		public static Gdk.Pixbuf CreatePixbuf (Surface s)
		{
			
			IntPtr result = f_pixbuf_from_cairo_surface (s.Handle);
			return (Gdk.Pixbuf) GLib.Object.GetObject (result, true);
		}
		*/

		public static Gdk.Pixbuf CreatePixbuf (MemorySurface mem)
		{
			IntPtr result = f_pixbuf_from_cairo_surface (mem.Handle);
			return (Gdk.Pixbuf) GLib.Object.GetObject (result, true);
		}
	}
}
