/* 
 * SoftFocus.cs
 *
 * Author
 *   Larry Ewing <lewing@novell.com>
 *
 * See COPYING for details.
 */
using Cairo;
using System;

namespace FSpot.Widgets {
	public class SoftFocus : IEffect {
		ImageInfo info;
		double radius;
		double amount;
		Gdk.Point center;
		ImageInfo blur;
		Pattern mask;
		bool double_buffer;

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
			double scale = Math.Max (200 / (double) source.Bounds.Width,
						 200 / (double) source.Bounds.Height);

			Gdk.Rectangle small = new Gdk.Rectangle (0, 0,
								(int) Math.Round (source.Bounds.Width * scale),
								(int) Math.Round (source.Bounds.Height * scale));
			
			MemorySurface image = new MemorySurface (Format.Argb32, 
								 small.Width,
								 small.Height);
			
			Context ctx = new Context (image);
			ctx.Matrix = source.Fill (small);
			ctx.Source = new SurfacePattern (source.Surface);
			Console.WriteLine (small);
			ctx.Paint ();
			((IDisposable)ctx).Dispose ();
			Gdk.Pixbuf normal = CairoUtils.CreatePixbuf (image);
			Gdk.Pixbuf blur = PixbufUtils.Blur (normal, 3);
			ImageInfo overlay = new ImageInfo (blur);
			blur.Dispose ();
			normal.Dispose ();
			return overlay;
		}
		
		private Pattern CreateMask ()
		{
			double max = Math.Max (blur.Bounds.Width, blur.Bounds.Height) * .25;
			double scale = blur.Bounds.Width / (double) info.Bounds.Width;

			RadialGradient circle = new RadialGradient (center.X * scale, center.Y * scale, radius * max,
								    center.X * scale, center.Y * scale, radius * max * .7);
			circle.AddColorStop (0, new Cairo.Color (0.0, 0.0, 0.0, 0.0));
			circle.AddColorStop (1.0, new Cairo.Color (1.0, 1.0, 1.0, 1.0));
			return circle;
		}
		
		public bool OnExpose (Context ctx, Gdk.Rectangle allocation)
		{
			SurfacePattern p = new SurfacePattern (info.Surface);
			Matrix m = info.Fill (allocation);
			ctx.Operator = Operator.Source;
			ctx.Matrix = m;
			ctx.Source = p;
			ctx.Paint ();

			SurfacePattern overlay = new SurfacePattern (blur.Surface);
			ctx.Matrix = new Matrix ();
			ctx.Matrix = blur.Fill (allocation);
			ctx.Operator = Operator.Over;
			ctx.Source = overlay;
			
			// FIXME ouch this is ugly.
			if (mask == null)
				Radius = Radius;

			//ctx.Paint ();
			ctx.Mask (mask);
			overlay.Destroy ();
			p.Destroy ();
			return true;
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
