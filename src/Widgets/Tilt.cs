/*
 * Tilt.cs
 *
 * Authors
 *   Larry Ewing <lewing@novell.com>
 *
 * See COPYING for license information.
 */
using System;
using Cairo;

namespace FSpot.Widgets {
	public class Tilt : IEffect {
		ImageInfo info;
		double angle;
		bool horizon;
		bool plumb;
		ImageInfo cache;
		
		public Tilt (ImageInfo info)
		{
			this.info = info;
		}
		
		public double Angle {
			get { return angle; }
			set { angle = Math.Max (Math.Min (value, Math.PI * .25), Math.PI * -0.25); }
		}
		
		public bool OnExpose (Context ctx, Gdk.Rectangle allocation)
		{
			ctx.Operator = Operator.Source;
			
			SurfacePattern p = new SurfacePattern (info.Surface);
			
			p.Filter = Filter.Fast;
			
			
			Matrix m = info.Fill (allocation, angle);
			
			ctx.Matrix = m;
			ctx.Source = p;
			ctx.Paint ();
			p.Destroy ();
			
			return true;
		}
		
		public void Dispose ()
		{
			
		}
	}
}
