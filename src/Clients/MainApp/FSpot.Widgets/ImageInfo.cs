//
// ImageInfo.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@src.gnome.org>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2007 Larry Ewing
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
using Gtk;

using FSpot.Utils;
using FSpot.Imaging;

using Hyena;

namespace FSpot.Widgets {
	public class ImageInfo : IDisposable {
		public Surface Surface;
		public Gdk.Rectangle Bounds;

		public ImageInfo (SafeUri uri)
		{
				using (var img = ImageFile.Create (uri)) {
					Pixbuf pixbuf = img.Load ();
					SetPixbuf (pixbuf);
					pixbuf.Dispose ();
				}
		}

		public ImageInfo (Pixbuf pixbuf)
		{
			SetPixbuf (pixbuf);
		}

		public ImageInfo (ImageInfo info, Widget w) : this (info, w, w.Allocation)
		{
		}

		public ImageInfo (ImageInfo info, Widget w, Gdk.Rectangle bounds)
		{
			Cairo.Surface similar = CairoUtils.CreateSurface (w.GdkWindow);
			Bounds = bounds;
			Surface = similar.CreateSimilar (Content.ColorAlpha, Bounds.Width, Bounds.Height);
			Context ctx = new Context (Surface);

			ctx.Matrix = info.Fill (Bounds);
			Pattern p = new SurfacePattern (info.Surface);
			ctx.Source = p;
			ctx.Paint ();
			((IDisposable)ctx).Dispose ();
			p.Destroy ();
		}

		public ImageInfo (ImageInfo info, Gdk.Rectangle allocation)
		{
			Surface = info.Surface.CreateSimilar (Content.Color,
							      allocation.Width,
							      allocation.Height);

			Context ctx = new Context (Surface);
			Bounds = allocation;

			ctx.Matrix = info.Fill (allocation);
			Pattern p = new SurfacePattern (info.Surface);
			ctx.Source = p;
			ctx.Paint ();
			((IDisposable)ctx).Dispose ();
			p.Destroy ();
		}

		private void SetPixbuf (Pixbuf pixbuf)
		{
			Surface = Hyena.Gui.PixbufImageSurface.Create(pixbuf);
			Bounds.Width = pixbuf.Width;
			Bounds.Height = pixbuf.Height;
		}

		public Matrix Fill (Gdk.Rectangle viewport)
		{
			Matrix m = new Matrix ();
			m.InitIdentity ();

			double scale = Math.Max (viewport.Width / (double) Bounds.Width,
						 viewport.Height / (double) Bounds.Height);

			double x_offset = Math.Round (((viewport.Width  - Bounds.Width * scale) / 2.0));
			double y_offset = Math.Round (((viewport.Height  - Bounds.Height * scale) / 2.0));

			m.Translate (x_offset, y_offset);
			m.Scale (scale, scale);
			return m;
		}

		//
		// this functions calculates the transformation needed to center and completely fill the
		// viewport with the Surface at the given tilt
		//
		public Matrix Fill (Gdk.Rectangle viewport, double tilt)
		{
			if (tilt == 0.0)
				return Fill (viewport);

			Matrix m = new Matrix ();
			m.InitIdentity ();

			double len;
			double orig_len;
			if (Bounds.Width > Bounds.Height) {
				len = viewport.Height;
				orig_len = Bounds.Height;
			} else {
				len = viewport.Width;
				orig_len = Bounds.Width;
			}

			double a = Math.Sqrt (viewport.Width * viewport.Width + viewport.Height * viewport.Height);
			double alpha = Math.Acos (len / a);
			double theta = alpha - Math.Abs (tilt);

			double slen = a * Math.Cos (theta);

			double scale = slen / orig_len;

			double x_offset = (viewport.Width  - Bounds.Width * scale) / 2.0;
			double y_offset = (viewport.Height  - Bounds.Height * scale) / 2.0;

			m.Translate (x_offset, y_offset);
			m.Scale (scale, scale);
			m.Invert ();
			m.Translate (viewport.Width * 0.5, viewport.Height * 0.5);
			m.Rotate (tilt);
			m.Translate (viewport.Width * -0.5, viewport.Height * -0.5);
			m.Invert ();
			return m;
		}

		public Matrix Fit (Gdk.Rectangle viewport)
		{
			Matrix m = new Matrix ();
			m.InitIdentity ();

			double scale = Math.Min (viewport.Width / (double) Bounds.Width,
						 viewport.Height / (double) Bounds.Height);

			double x_offset = (viewport.Width  - Bounds.Width * scale) / 2.0;
			double y_offset = (viewport.Height  - Bounds.Height * scale) / 2.0;

			m.Translate (x_offset, y_offset);
			m.Scale (scale, scale);
			return m;
		}

		public void Dispose ()
		{
			((IDisposable)Surface).Dispose ();
		}
	}
}
