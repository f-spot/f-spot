//
// CairoUtils.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2006 Novell, Inc.
// Copyright (C) 2006 Larry Ewing
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Cairo;
using Gdk;

namespace FSpot.Utils
{
	public static class CairoUtils
	{
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

		unsafe public static Pixbuf PixbufFromSurface (ImageSurface source)
		{
			int width = source.Width;
			int height = source.Height;
			byte [] gdkPixels = new byte [width * height * 4];

			Format format = source.Format;

			Surface surface = new ImageSurface (gdkPixels, format, width, height, 4 * width);
			Context ctx = new Context (surface);
			ctx.SetSourceSurface (source, 0, 0);

			if (format == Format.ARGB32)
				ctx.MaskSurface (source, 0, 0);
			else
				ctx.Paint ();

			int j;
			for (j = height; j > 0; j--) {
				int p = (height - j) * 4 * width;
				int end = p + 4 * width;
				byte tmp;

				while (p < end) {
					tmp = gdkPixels [p + 0];
					if (System.BitConverter.IsLittleEndian) {
						gdkPixels [p + 0] = gdkPixels [p + 2];
						gdkPixels [p + 2] = tmp;
					} else {
						gdkPixels [p + 0] = gdkPixels [p + 1];
						gdkPixels [p + 1] = gdkPixels [p + 2];
						gdkPixels [p + 2] = gdkPixels [p + 3];
						gdkPixels [p + 3] = tmp;
					}
					p += 4;
				}
			}

			surface.Dispose ();
			Pixbuf pixbuf = new Pixbuf (gdkPixels, Colorspace.Rgb, true, 8, width, height, 4 * width);
			return pixbuf;
		}
	}
}
