/* 
 * Wipe.cs
 *
 * Copyright 2007 Novell Inc.
 *
 * Author
 *   Larry Ewing <lewing@novell.com>
 *
 * See COPYING for license information.
 *
 */

using System;
using Cairo;
using Gtk;

namespace FSpot.Widgets {
	public class Wipe : ITransition {
		DateTime start;
		TimeSpan duration = new TimeSpan (0, 0, 3);
		ImageInfo end;
		ImageInfo begin;
		ImageInfo end_buffer;
		ImageInfo begin_buffer;
		int frames;
		double fraction;
		
		public int Frames {
			get { return frames; }
		}
		
		public Wipe (ImageInfo begin, ImageInfo end)
		{
			this.begin = begin;
			this.end = end;
		}
		
		public bool OnEvent (Widget w)
		{
			if (begin_buffer == null) {
				begin_buffer = new ImageInfo (begin, w); //.Allocation);
			}

			if (end_buffer == null) {
				end_buffer = new ImageInfo (end, w); //.Allocation);
				start = DateTime.UtcNow;
			}
			
			w.QueueDraw ();
			
			TimeSpan elapsed = DateTime.UtcNow - start;
			fraction = elapsed.Ticks / (double) duration.Ticks; 
			
			frames++;
			
			return fraction < 1.0;
		}
		
		Pattern CreateMask (Gdk.Rectangle coverage, double fraction)
		{
			LinearGradient fade = new LinearGradient (0, 0,
								  coverage.Width, 0);
			fade.AddColorStop (Math.Max (fraction - .1, 0.0), new Cairo.Color (1.0, 1.0, 1.0, 1.0));
			fade.AddColorStop (Math.Min (fraction + .1, 1.0), new Cairo.Color (0.0, 0.0, 0.0, 0.0));
			
			return fade;
		}
		
		public bool OnExpose (Context ctx, Gdk.Rectangle allocation)
		{
			ctx.Operator = Operator.Source;
			SurfacePattern p = new SurfacePattern (begin_buffer.Surface);
			ctx.Matrix = begin_buffer.Fill (allocation);
			p.Filter = Filter.Fast;
			ctx.Source = p;
			ctx.Paint ();
			
			ctx.Operator = Operator.Over;
			ctx.Matrix = end_buffer.Fill (allocation);
			SurfacePattern sur = new SurfacePattern (end_buffer.Surface);
			sur.Filter = Filter.Fast;
			ctx.Source = sur;
			Pattern mask = CreateMask (allocation, fraction);
			ctx.Mask (mask);
			mask.Destroy ();
			p.Destroy ();
			sur.Destroy ();
			
			return fraction < 1.0;
		}
		
		public void Dispose ()
		{
			begin_buffer.Dispose ();
			end_buffer.Dispose ();
		}
	}
}
