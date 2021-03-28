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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Cairo;

using FSpot.Utils;

using Pinta.Core;

namespace FSpot.Widgets
{
	public class SoftFocus : IDisposable
	{
		ImageInfo info;
		double radius;
		double amount;
		Gdk.Point center;
		ImageInfo blur;
		Pattern mask;
		bool disposed;

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

				blur?.Dispose ();

				blur = CreateBlur (info);
			}
		}

		public double Radius {
			get { return radius; }
			set {
				radius = value;

				if (blur == null)
					return;

				mask?.Dispose ();

				mask = CreateMask ();
			}
		}

		ImageInfo CreateBlur (ImageInfo source)
		{
			double scale = Math.Max (256 / (double)source.Bounds.Width,
							   256 / (double)source.Bounds.Height);

			var small = new Gdk.Rectangle (0, 0,
									  (int)Math.Ceiling (source.Bounds.Width * scale),
									  (int)Math.Ceiling (source.Bounds.Height * scale));

			var image = new ImageSurface (Format.Argb32,
									 small.Width,
									 small.Height);

			var ctx = new Context (image);

			ctx.Matrix = source.Fit (small);
			ctx.Operator = Operator.Source;
			Pattern p = new SurfacePattern (source.Surface);
			ctx.SetSource (p);

			ctx.Paint ();
			p.Dispose ();
			ctx.Dispose ();

			ImageInfo overlay;
			using (var normal = image.ToPixbuf ()) {
				using (var pixbufBlur = PixbufUtils.Blur (normal, 3, null)) {
					overlay = new ImageInfo (pixbufBlur);
				}
			}

			image.Dispose ();
			return overlay;
		}

		Pattern CreateMask ()
		{
			double max = Math.Max (blur.Bounds.Width, blur.Bounds.Height) * .25;
			double scale = blur.Bounds.Width / (double)info.Bounds.Width;

			RadialGradient circle;

			circle = new RadialGradient (Center.X * scale, Center.Y * scale, radius * max * .7,
				Center.X * scale, Center.Y * scale, radius * max + max * .2);

			circle.AddColorStop (0, new Color (0.0, 0.0, 0.0, 0.0));
			circle.AddColorStop (1.0, new Color (1.0, 1.0, 1.0, 1.0));
			return circle;
		}

		public void Apply (Context ctx, Gdk.Rectangle allocation)
		{
			var p = new SurfacePattern (info.Surface);
			ctx.Matrix = new Matrix ();
			Matrix m = info.Fit (allocation);
			ctx.Operator = Operator.Over;
			ctx.Matrix = m;
			ctx.SetSource (p);
			ctx.Paint ();

			var overlay = new SurfacePattern (blur.Surface);
			ctx.Matrix = new Matrix ();
			ctx.Matrix = blur.Fit (allocation);
			ctx.Operator = Operator.Over;
			ctx.SetSource (overlay);

			// FIXME ouch this is ugly.
			if (mask == null)
				Radius = Radius;

			//ctx.Paint ();
			ctx.Mask (mask);
			overlay.Dispose ();
			p.Dispose ();
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			if (disposing) {
				// free managed resources
				if (mask != null) {
					mask.Dispose ();
					mask = null;
				}

				if (blur != null) {
					blur.Dispose ();
					blur = null;
				}
			}
			// free unmanaged resources
		}
	}
}
