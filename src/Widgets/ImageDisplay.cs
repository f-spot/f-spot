using Gtk;
using Gdk;
using System;
using Cairo;

namespace FSpot.Widgets {
	[Binding(Gdk.Key.Up, "Up")]
	[Binding(Gdk.Key.Down, "Down")]
	[Binding(Gdk.Key.Left, "Previous")]
	[Binding(Gdk.Key.Right, "Next")] 
	[Binding(Gdk.Key.F, "ToggleFullscreen")]
	public class ImageDisplay : Gtk.EventBox {
		ImageInfo current;
		ImageInfo next;
		IBrowsableCollection collection;
		ITransition transition;
		double opacity = 0.5;
		Delay delay;
		int index = 0;

		public ImageDisplay (IBrowsableCollection collection) 
		{
			CanFocus = true;
			current = new ImageInfo (collection [index].DefaultVersionUri);
			this.collection = collection;
			if (collection.Count > index + 1) {
				next = new ImageInfo (collection [index + 1].DefaultVersionUri);
			}
			delay = new Delay (new GLib.IdleHandler (DrawFrame));
		}

		public bool Up ()
		{
			Console.WriteLine ("Up");
			transition = new CrossFade (current, next);
			delay.Start ();
			return true;
		}

		public bool Down ()
		{
			Console.WriteLine ("down");
			transition = new CrossFade (next, current);
			delay.Start ();
			return true;
		}
		
		public bool Previous ()
		{
			Console.WriteLine ("previous");
			if (index > 0)
				index --;
			next = current;
			current = new ImageInfo (collection [index].DefaultVersionUri);
			QueueDraw ();
			return true;
		}

		public bool Next ()
		{
			Console.WriteLine ("next");
			if (index < collection.Count - 1)
				index ++;
			
			current = next;
			next = new ImageInfo (collection [index].DefaultVersionUri);
			QueueDraw ();
			return true;
		}

		public bool ToggleFullscreen ()
		{
			
			return true;
		}

		public bool DrawFrame ()
		{
			QueueDraw ();
		        //using (new Timer ("frame time")) {
			if (IsRealized)
				GdkWindow.ProcessUpdates (false);
			//}
			return true;
		}
		
		private void SetClip (Graphics ctx, Region region)
		{
			foreach (Rectangle area in region.GetRectangles ()) {
				ctx.MoveTo (area.Left, area.Top);
				ctx.LineTo (area.Right, area.Top);
				ctx.LineTo (area.Right, area.Bottom);
				ctx.LineTo (area.Left, area.Bottom);
					
				ctx.ClosePath ();
			}
			ctx.Clip ();
		}
		
		private void OnExpose (Graphics ctx, Region region)
		{
			if (transition != null) {
				SetClip (ctx, region);
				
				if (! transition.OnExpose (ctx, Allocation, region.Clipbox)) {
					Console.WriteLine ("Frames = {0}", transition.Frames);
					transition = null;
					delay.Stop ();
				}
			} else {
				ctx.Operator = Operator.Source;
				SurfacePattern p = new SurfacePattern (current.Surface);
				p.Filter = Filter.Fast;
				Console.WriteLine (p.Filter);
				SetClip (ctx, region);
				ctx.Matrix = current.Fill (Allocation);

				ctx.Pattern = p;
				ctx.Paint ();
				p.Destroy ();
			}
		}

		protected override bool OnExposeEvent (EventExpose args)
		{
			bool double_buffer = false;
			base.OnExposeEvent (args);

			Graphics ctx = CairoUtils.CreateContext (GdkWindow);
			if (double_buffer) {
				ImageSurface cim = new ImageSurface (Format.RGB24, 
								     Allocation.Width, 
								     Allocation.Height);

				Graphics buffer = new Graphics (cim);
				OnExpose (buffer, args.Region);

				SurfacePattern sur = new SurfacePattern (cim);
				sur.Filter = Filter.Fast;
				ctx.Pattern = sur;
				SetClip (ctx, args.Region);

				ctx.Paint ();

				((IDisposable)buffer).Dispose ();
				((IDisposable)cim).Dispose ();
				sur.Destroy ();
			} else {
				OnExpose (ctx, args.Region);
			}

			((IDisposable)ctx).Dispose ();
			return true;
		}

		private interface ITransition {
			bool OnExpose (Graphics ctx, Rectangle allocation, Rectangle area);
			int Frames { get; }
		}

		private interface IEffect {
			bool OnExpose (Graphics ctx, Rectangle allocation, Rectangle area);
		}

		private class Slide : ITransition {
			ImageInfo begin;
			ImageInfo end;
			DateTime start;
			TimeSpan duration = new TimeSpan (0, 0, 2);
			int frames;

			public Slide (ImageInfo begin, ImageInfo end)
			{
				start = DateTime.Now;
				this.begin = begin;
				this.end = end;
			}
			
			public int Frames {
				get { return frames; }
			}

			public bool OnExpose (Graphics ctx, Rectangle allocation, Rectangle area)
			{
				TimeSpan elapsed = DateTime.Now - start;
				double fraction = elapsed.Ticks / (double) duration.Ticks; 
				
				frames++;

				ctx.Matrix.InitIdentity ();
				ctx.Operator = Operator.Source;
				
				SurfacePattern p = new SurfacePattern (begin.Surface);
				//p.Filter = Filter.Fast;
				ctx.Matrix = begin.Fill (allocation);
				ctx.Pattern = p;
				ctx.Paint ();

				ctx.Operator = Operator.Over;
				ctx.Matrix = end.Fill (allocation);
				SurfacePattern sur = new SurfacePattern (end.Surface);
				//sur.Filter = Filter.Fast;
				Pattern black = new SolidPattern (new Cairo.Color (0.0, 0.0, 0.0, fraction));
				ctx.Pattern = sur;
				ctx.Mask (black);

				return fraction < 1.0;
			}
		}

		private class CrossFade : ITransition {
			DateTime start;
			TimeSpan duration = new TimeSpan (0, 0, 2);
			ImageInfo begin;
			ImageInfo end;
			ImageInfo begin_buffer;
			ImageInfo end_buffer;
			int frames = 0;

			public CrossFade (ImageInfo begin, ImageInfo end)
			{
				start = DateTime.Now;
				this.begin = begin;
				this.end = end;
			}
			
			public int Frames {
				get { return frames; }
			}

			public bool OnExpose (Graphics ctx, Rectangle allocation, Rectangle area)
			{
				if (begin_buffer == null) {
					start = DateTime.Now;
					begin_buffer = new ImageInfo (begin, allocation);
				}

				if (end_buffer == null)
					end_buffer = new ImageInfo (end, allocation);

				frames ++;
				TimeSpan elapsed = DateTime.Now - start;
				double fraction = elapsed.Ticks / (double) duration.Ticks; 
				double opacity = Math.Sin (Math.Min (fraction, 1.0) * Math.PI * 0.5);
				
				ctx.Matrix.InitIdentity ();
				ctx.Operator = Operator.Source;
				
				SurfacePattern p = new SurfacePattern (begin_buffer.Surface);
				ctx.Matrix = begin_buffer.Fill (allocation);
				p.Filter = Filter.Fast;
				ctx.Pattern = p;
				ctx.Paint ();

				ctx.Operator = Operator.Over;
				ctx.Matrix = end_buffer.Fill (allocation);
				SurfacePattern sur = new SurfacePattern (end_buffer.Surface);
				Pattern black = new SolidPattern (new Cairo.Color (0.0, 0.0, 0.0, opacity));
				//ctx.Pattern = black;
				//ctx.Fill ();
				sur.Filter = Filter.Fast;
				ctx.Pattern = sur;
				ctx.Mask (black);

				if (fraction >= 1.0) {
					((IDisposable)end_buffer.Surface).Dispose ();
					((IDisposable)begin_buffer.Surface).Dispose ();
				}

				return fraction < 1.0;
			}
		}

		private class ImageInfo {
			public Surface Surface;
			public Rectangle Bounds;

			public ImageInfo (Uri uri)
			{
				ImageFile img = ImageFile.Create (uri);
				Pixbuf pixbuf = img.Load ();
				Surface = CairoUtils.CreateSurface (pixbuf);
				SetPixbuf (pixbuf);
				pixbuf.Dispose ();
			}

			public ImageInfo (Pixbuf pixbuf)
			{
				SetPixbuf (pixbuf);
			}

			public ImageInfo (ImageInfo info, Rectangle allocation)
			{
				Surface = new ImageSurface (Format.RGB24, allocation.Width, allocation.Height);
				Graphics ctx = new Graphics (Surface);
				Bounds = allocation;
				ctx.Matrix = info.Fill (allocation);
				Pattern p = new SurfacePattern (info.Surface);
				ctx.Pattern = p;
				ctx.Paint ();
				((IDisposable)ctx).Dispose ();
				p.Destroy ();
			}

			private void SetPixbuf (Pixbuf pixbuf)
			{
				Surface = CairoUtils.CreateSurface (pixbuf);
				Bounds.Width = pixbuf.Width;
				Bounds.Height = pixbuf.Height;
			}

			public Matrix Fill (Gdk.Rectangle allocation)
			{
				Matrix m = new Matrix ();
				m.InitIdentity ();
				
				double scale = Math.Max (allocation.Width / (double) Bounds.Width,
							 allocation.Height / (double) Bounds.Height);
				
				double x_offset = (allocation.Width  - Bounds.Width * scale) / 2.0;
				double y_offset = (allocation.Height  - Bounds.Height * scale) / 2.0;
				
				m.Translate (x_offset, y_offset);
				m.Scale (scale, scale);
				return m;
			}

			public Matrix Fit (Gdk.Rectangle allocation)
			{
				Matrix m = new Matrix ();
				m.InitIdentity ();
				
				double scale = Math.Min (allocation.Width / (double) Bounds.Width,
							 allocation.Height / (double) Bounds.Height);
				
				double x_offset = (allocation.Width  - Bounds.Width * scale) / 2.0;
				double y_offset = (allocation.Height  - Bounds.Height * scale) / 2.0;
				
				m.Translate (x_offset, y_offset);
				m.Scale (scale, scale);
				return m;
			}
		}
	}
}

