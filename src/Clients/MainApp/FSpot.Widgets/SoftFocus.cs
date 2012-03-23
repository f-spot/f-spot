//
// SoftFocus.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
// Copyright (C) 2008 Ruben Vermeersch
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

using Pinta.Core;

namespace FSpot.Widgets {

	public class SoftFocus : IDisposable {

		ImageInfo info;
		double radius;
		double amount;
		Gdk.Point center;
		ImageInfo blur;
		Pattern mask;

		public SoftFocus (ImageInfo info)
		{
			this.info = info;
			center.X = info.Bounds.Width / 2;
			center.Y = info.Bounds.Height / 2;
			Amount = 3;
			Radius = .5;
		}

		public Gdk.Point Center {
			get { return center; }
			set { center = value; }
		}

		public double Amount {
			get { return amount; }
			set {
				amount = value;

				if (blur != null)
					blur.Dispose ();

				blur = CreateBlur (info);
			}
		}

		public double Radius {
			get { return radius; }
			set {
				radius = value;

				if (blur == null)
					return;

				if (mask != null)
					mask.Destroy ();

				mask = CreateMask ();
			}
		}

		private ImageInfo CreateBlur (ImageInfo source)
		{
			double scale = Math.Max (256 / (double) source.Bounds.Width,
						 256 / (double) source.Bounds.Height);

			Gdk.Rectangle small = new Gdk.Rectangle (0, 0,
								(int) Math.Ceiling (source.Bounds.Width * scale),
								(int) Math.Ceiling (source.Bounds.Height * scale));

			ImageSurface image = new ImageSurface (Format.Argb32,
								 small.Width,
								 small.Height);

			Context ctx = new Context (image);

			ctx.Matrix = source.Fit (small);
			ctx.Operator = Operator.Source;
			Pattern p = new SurfacePattern (source.Surface);
			ctx.Source = p;

			ctx.Paint ();
			p.Destroy ();
			((IDisposable)ctx).Dispose ();
			Gdk.Pixbuf normal = image.ToPixbuf();

			Gdk.Pixbuf blur = PixbufUtils.Blur (normal, 3, null);

			ImageInfo overlay = new ImageInfo (blur);
			blur.Dispose ();
			normal.Dispose ();
			image.Destroy ();
			return overlay;
		}

		private Pattern CreateMask ()
		{
			double max = Math.Max (blur.Bounds.Width, blur.Bounds.Height) * .25;
			double scale = blur.Bounds.Width / (double) info.Bounds.Width;

			RadialGradient circle;

			circle = new RadialGradient (center.X * scale, center.Y * scale, radius * max * .7,
							 center.X * scale, center.Y * scale, radius * max + max * .2);

			circle.AddColorStop (0, new Cairo.Color (0.0, 0.0, 0.0, 0.0));
			circle.AddColorStop (1.0, new Cairo.Color (1.0, 1.0, 1.0, 1.0));
			return circle;
		}

		public void Apply (Context ctx, Gdk.Rectangle allocation)
		{
			SurfacePattern p = new SurfacePattern (info.Surface);
			ctx.Matrix = new Matrix ();
			Matrix m = info.Fit (allocation);
			ctx.Operator = Operator.Over;
			ctx.Matrix = m;
			ctx.Source = p;
			ctx.Paint ();

			SurfacePattern overlay = new SurfacePattern (blur.Surface);
			ctx.Matrix = new Matrix ();
			ctx.Matrix = blur.Fit (allocation);
			ctx.Operator = Operator.Over;
			ctx.Source = overlay;

			// FIXME ouch this is ugly.
			if (mask == null)
				Radius = Radius;

			//ctx.Paint ();
			ctx.Mask (mask);
			overlay.Destroy ();
			p.Destroy ();
		}

		public void Dispose ()
		{
			if (mask != null)
				mask.Destroy ();

			if (blur != null)
				blur.Dispose ();
		}
	}
}
