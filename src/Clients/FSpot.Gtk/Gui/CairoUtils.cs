//
// CairoUtils.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2006 Novell, Inc.
// Copyright (C) 2006 Larry Ewing
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Cairo;

using Gdk;

namespace FSpot.Utils
{
	public static class CairoUtils
	{
		public static void SetSourceColor (this Context cr, Cairo.Color color)
			=> cr?.SetSourceRGBA (color.R, color.G, color.B, color.A);

		public static Surface CreateSurface (Drawable d)
		{
			d.GetSize (out var width, out var height);
			var surface = new XlibSurface (GdkUtils.GetXDisplay (d.Display),
								   (IntPtr)GdkUtils.GetXid (d),
								   GdkUtils.GetXVisual (d.Visual),
								   width, height);
			return surface;
		}

		public static unsafe Pixbuf PixbufFromSurface (ImageSurface source)
		{
			int width = source.Width;
			int height = source.Height;
			byte[] gdkPixels = new byte[width * height * 4];

			Format format = source.Format;

			Surface surface = new ImageSurface (gdkPixels, format, width, height, 4 * width);
			using (var ctx = new Context (surface)) {
				ctx.SetSourceSurface (source, 0, 0);

				if (format == Format.ARGB32)
					ctx.MaskSurface (source, 0, 0);
				else
					ctx.Paint ();
			}

			int j;
			for (j = height; j > 0; j--) {
				int p = (height - j) * 4 * width;
				int end = p + 4 * width;
				byte tmp;

				while (p < end) {
					tmp = gdkPixels[p + 0];
					if (System.BitConverter.IsLittleEndian) {
						gdkPixels[p + 0] = gdkPixels[p + 2];
						gdkPixels[p + 2] = tmp;
					} else {
						gdkPixels[p + 0] = gdkPixels[p + 1];
						gdkPixels[p + 1] = gdkPixels[p + 2];
						gdkPixels[p + 2] = gdkPixels[p + 3];
						gdkPixels[p + 3] = tmp;
					}
					p += 4;
				}
			}

			surface.Dispose ();
			var pixbuf = new Pixbuf (gdkPixels, Colorspace.Rgb, true, 8, width, height, 4 * width);
			return pixbuf;
		}
	}
}
