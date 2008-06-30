/*
 * PanZoom.cs
 *
 * Copyright 2007 Novell Inc.
 *
 * Author
 *   Larry Ewing <lewing@novell.com>
 *
 *  See COPYING for License information.
 *
 */
using System;
using Cairo;
using Gtk;

namespace FSpot.Widgets {		
	public class PanZoomOld : ITransition {
		ImageInfo info;
		ImageInfo buffer;
		TimeSpan duration = new TimeSpan (0, 0, 7);
		double pan_x;
		double pan_y;
		double x_offset;
		double y_offset;
		DateTime start;
		int frames = 0;
		double zoom;
		
		public PanZoomOld (ImageInfo info)
		{
			this.info = info;
		}
		
		public int Frames {
			get { return frames; }
		}
		
		public bool OnEvent (Widget w)
		{
			if (frames == 0) {
				start = DateTime.UtcNow;
				Gdk.Rectangle viewport = w.Allocation;
				
				zoom = Math.Max (viewport.Width / (double) info.Bounds.Width,
						 viewport.Height / (double) info.Bounds.Height);
				
				zoom *= 1.2;		     
				
				x_offset = (viewport.Width - info.Bounds.Width * zoom);
				y_offset = (viewport.Height - info.Bounds.Height * zoom);
				
				pan_x = 0;
				pan_y = 0;
				w.QueueDraw ();
			}
			frames ++;
			
			double percent = Math.Min ((DateTime.UtcNow - start).Ticks / (double) duration.Ticks, 1.0);
			
			double x = x_offset * percent;
			double y = y_offset * percent;

			if (w.IsRealized){
				w.GdkWindow.Scroll ((int)(x - pan_x), (int)(y - pan_y));
				pan_x = x;
				pan_y = y;
				//w.GdkWindow.ProcessUpdates (false);
			}
			
			return percent < 1.0;
		}
		
		public bool OnExpose (Context ctx, Gdk.Rectangle viewport)
		{
			double percent = Math.Min ((DateTime.UtcNow - start).Ticks / (double) duration.Ticks, 1.0);
			
			//Matrix m = info.Fill (allocation);
			Matrix m = new Matrix ();
			m.Translate (pan_x, pan_y);
			m.Scale (zoom, zoom);
			ctx.Matrix = m;
			
			SurfacePattern p = new SurfacePattern (info.Surface);
			ctx.Source = p;
			ctx.Paint ();
			p.Destroy ();
			
			return percent < 1.0;
		}
		
		public void Dispose ()
		{
			//info.Dispose ();
		}
	}
	
	public class PanZoom : ITransition {
		ImageInfo info;
		ImageInfo buffer;
		TimeSpan duration = new TimeSpan (0, 0, 7);
		int pan_x;
		int pan_y;
		DateTime start;
		int frames = 0;
		double zoom;
		
		public PanZoom (ImageInfo info)
		{
			this.info = info;
		}
		
		public int Frames {
			get { return frames; }
		}
		
		public bool OnEvent (Widget w)
		{
			Gdk.Rectangle viewport = w.Allocation;
			if (buffer == null) {
				double scale = Math.Max (viewport.Width / (double) info.Bounds.Width,
							 viewport.Height / (double) info.Bounds.Height);
				
				scale *= 1.2;		     
				buffer = new ImageInfo (info, w, 
							new Gdk.Rectangle (0, 0, 
									   (int) (info.Bounds.Width * scale), 
									   (int) (info.Bounds.Height * scale)));
				start = DateTime.UtcNow;
				//w.QueueDraw ();
				zoom = 1.0;
			}
			
			double percent = Math.Min ((DateTime.UtcNow - start).Ticks / (double) duration.Ticks, 1.0);
			
			int n_x = (int) Math.Floor ((buffer.Bounds.Width - viewport.Width) * percent);
			int n_y = (int) Math.Floor ((buffer.Bounds.Height - viewport.Height) * percent);
			
			if (n_x != pan_x || n_y != pan_y) {
				//w.GdkWindow.Scroll (- (n_x - pan_x), - (n_y - pan_y));
				w.QueueDraw ();
				w.GdkWindow.ProcessUpdates (false);
				//Log.DebugFormat ("{0} {1} elapsed", DateTime.UtcNow, DateTime.UtcNow - start);
			}
			pan_x = n_x;
			pan_y = n_y;
			
			return percent < 1.0;
		}
		
		public bool OnExpose (Context ctx, Gdk.Rectangle viewport)
		{
			double percent = Math.Min ((DateTime.UtcNow - start).Ticks / (double) duration.Ticks, 1.0);
			frames ++;
			
			//ctx.Matrix = m;
			
			SurfacePattern p = new SurfacePattern (buffer.Surface);
			p.Filter = Filter.Fast;
			Matrix m = new Matrix ();
			m.Translate (pan_x * zoom, pan_y * zoom);
			m.Scale (zoom, zoom);
			zoom *= .98;
			p.Matrix = m;
			ctx.Source = p;
			ctx.Paint ();
			p.Destroy ();
			
			return percent < 1.0;
		}
		
		public void Dispose ()
		{
			buffer.Dispose ();
		}
	}
}
