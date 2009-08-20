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
	}
}
