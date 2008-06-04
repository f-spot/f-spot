/*
 * Dissolve.cs
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
	public class Dissolve : ITransition {
		DateTime start;
		TimeSpan duration = new TimeSpan (0, 0, 1);
		ImageInfo begin;
		ImageInfo end;
		ImageInfo begin_buffer;
		ImageInfo end_buffer;
		int frames = 0;
		
		public Dissolve (ImageInfo begin, ImageInfo end)
		{
			this.begin = begin;
			this.end = end;
		}
		
		public int Frames {
			get { return frames; }
		}
		
		public bool OnEvent (Widget w)
		{
			if (begin_buffer == null) {
				begin_buffer = new ImageInfo (begin, w); //.Allocation);
			}
			
			if (end_buffer == null) {
				end_buffer = new ImageInfo (end, w); //.Allocation);
			}
			
			w.QueueDraw ();
			w.GdkWindow.ProcessUpdates (false);

			TimeSpan elapsed = DateTime.UtcNow - start;
			double fraction = elapsed.Ticks / (double) duration.Ticks; 
			
			return fraction < 1.0;
		}
		
		public bool OnExpose (Context ctx, Gdk.Rectangle allocation)
		{
			if (frames == 0)
				start = DateTime.UtcNow;
			
			frames ++;
			TimeSpan elapsed = DateTime.UtcNow - start;
			double fraction = elapsed.Ticks / (double) duration.Ticks; 
			double opacity = Math.Sin (Math.Min (fraction, 1.0) * Math.PI * 0.5);
			
			ctx.Operator = Operator.Source;
			
			SurfacePattern p = new SurfacePattern (begin_buffer.Surface);
			ctx.Matrix = begin_buffer.Fill (allocation);
			p.Filter = Filter.Fast;
			ctx.Source = p;
			ctx.Paint ();
			
			ctx.Operator = Operator.Over;
			ctx.Matrix = end_buffer.Fill (allocation);
			SurfacePattern sur = new SurfacePattern (end_buffer.Surface);
#if MONO_1_2_5
			Pattern black = new SolidPattern (new Cairo.Color (0.0, 0.0, 0.0, opacity));
#else
			Pattern black = new SolidPattern (new Cairo.Color (0.0, 0.0, 0.0, opacity), true);
#endif
			//ctx.Source = black;
			//ctx.Fill ();
			sur.Filter = Filter.Fast;
			ctx.Source = sur;
			ctx.Mask (black);
			//ctx.Paint ();
			
			ctx.Matrix = new Matrix ();
			
			ctx.MoveTo (allocation.Width / 2.0, allocation.Height / 2.0);
			ctx.Source = new SolidPattern (1.0, 0, 0);	
			#if debug
			ctx.ShowText (String.Format ("{0} {1} {2} {3} {4} {5} {6} {7}", 
						     frames,
						     sur.Status,
						     p.Status,
						     opacity, fraction, elapsed, start, DateTime.UtcNow));
			#endif
			sur.Destroy ();
			p.Destroy ();
			return fraction < 1.0;
		}
		
		public void Dispose ()
		{
			if (begin_buffer != null) {
				begin_buffer.Dispose ();
				begin_buffer = null;
			}
			
			if (end_buffer != null) {
				end_buffer.Dispose ();
				end_buffer = null;
			}
		}
	}
}

 
