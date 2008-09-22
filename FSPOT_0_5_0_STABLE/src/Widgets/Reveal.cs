/*
 * Reveal.cs
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
	public class Reveal : ITransition {
		DateTime start;
		TimeSpan duration = new TimeSpan (0, 0, 2);
		ImageInfo end;
		ImageInfo begin;
		ImageInfo end_buffer;
		ImageInfo begin_buffer;
		double fraction;
		int frames;
		
		public int Frames {
			get { return frames; }
		}

		public Reveal (ImageInfo begin, ImageInfo end)
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
		
		public bool OnExpose (Context ctx, Gdk.Rectangle allocation)
		{
			ctx.Operator = Operator.Source;
			ctx.Matrix = end_buffer.Fill (allocation);
			SurfacePattern sur = new SurfacePattern (end_buffer.Surface);
			ctx.Source = sur;
			ctx.Paint ();
			sur.Destroy ();
			
			ctx.Operator = Operator.Over;
			SurfacePattern p = new SurfacePattern (begin_buffer.Surface);
			Matrix m = begin_buffer.Fill (allocation);
			m.Translate (Math.Round (- allocation.Width * fraction), 0);
			ctx.Matrix = m;
			p.Filter = Filter.Fast;
			ctx.Source = p;
			ctx.Paint ();
			p.Destroy ();
			
			return fraction < 1.0;
		}
		
		public void Dispose ()
		{
			begin_buffer.Dispose ();
			end_buffer.Dispose ();
		}
	}
}
