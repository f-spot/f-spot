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

namespace FSpot.Utils
{
	public class CairoUtils
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

		public static IntPtr PixbufFromSurface(Surface source)
		{
			int width = cairo_image_surface_get_width (source.Handle);
			int height = cairo_image_surface_get_height (source.Handle);
			return IntPtr.Zero;
			/*
			GdkPixbuf *pixbuf = gdk_pixbuf_new(GDK_COLORSPACE_RGB,
							   TRUE,
						           8,
						           width,
						           height);

			guchar *gdk_pixels = gdk_pixbuf_get_pixels (pixbuf);
			int gdk_rowstride = gdk_pixbuf_get_rowstride (pixbuf);
			int n_channels = gdk_pixbuf_get_n_channels (pixbuf);
			cairo_format_t format;
			cairo_surface_t *surface;
			cairo_t *ctx;
			static const cairo_user_data_key_t key;
			int j;
		
			format = f_image_surface_get_format (source);
			surface = cairo_image_surface_create_for_data (gdk_pixels,
								 format,
								 width, height, gdk_rowstride);
			ctx = cairo_create (surface);
			cairo_set_source_surface (ctx, source, 0, 0);
			if (format == CAIRO_FORMAT_ARGB32)
				cairo_mask_surface (ctx, source, 0, 0);
			else
				cairo_paint (ctx);

			for (j = height; j; j--)
			{
				guchar *p = gdk_pixels;
				guchar *end = p + 4 * width;
				guchar tmp;

				while (p < end)
				{
					tmp = p[0];
			#if G_BYTE_ORDER == G_LITTLE_ENDIAN
					p[0] = p[2];
					p[2] = tmp;
			#else
					p[0] = p[1];
					p[1] = p[2];
					p[2] = p[3];
					p[3] = tmp;
			#endif
					p += 4;
				}
		
			gdk_pixels += gdk_rowstride;
			}

			cairo_destroy (ctx);
			cairo_surface_destroy (surface);
			return pixbuf;
*/
		}
	}
}
