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
