/*
 * FSpot.Utils.CairoUtils.cs
 *
 * Author(s)
 *   Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details
 *
 */

using System;
using Cairo;
using System.Runtime.InteropServices;

namespace FSpot.Utils {
	public class CairoUtils {
		static class NativeMethods
		{
//	                [DllImport ("libcairo-2.dll")]
//			public static extern void cairo_user_to_device (IntPtr cr, ref double x, ref double y);

			[DllImport("libgdk-2.0-0.dll")]
			public static extern IntPtr gdk_cairo_create (IntPtr raw);	
		}
		
//		[Obsolete ("use Cairo.Context.UserToDevice instead")]
//		static void UserToDevice (Context ctx, ref double x, ref double y)
//		{
//			NativeMethods.cairo_user_to_device (ctx.Handle, ref x, ref y);
//		}
		
		[Obsolete ("use Gdk.CairoHelper.Create instead")]
		public static Cairo.Context CreateContext (Gdk.Drawable drawable)
		{
			Cairo.Context ctx = new Cairo.Context (NativeMethods.gdk_cairo_create (drawable.Handle));
			if (ctx == null) 
				throw new Exception ("Couldn't create Cairo Graphics!");
			
			return ctx;
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
	}
}
