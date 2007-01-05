using Gtk;
using Gdk;
using System;
using Cairo;

namespace FSpot.Widgets {
	[Binding(Gdk.Key.Up, "Up")]
	[Binding(Gdk.Key.Down, "Down")]
	[Binding(Gdk.Key.Left, "TiltImage", 0.05)]
	[Binding(Gdk.Key.Right, "TiltImage", -0.05)] 
	[Binding(Gdk.Key.space, "Pan")]
	[Binding(Gdk.Key.Q, "Vingette")]
	[Binding(Gdk.Key.R, "RevealImage")]
	[Binding(Gdk.Key.P, "PushImage")]
	public class ImageDisplay : Gtk.EventBox {
		ImageInfo current;
		ImageInfo next;
		BrowsablePointer item;
		ITransition transition;
		IEffect effect;
		Delay delay;
		int index = 0;

		ITransition Transition {
			get { return transition; }
			set { 
				if (transition != null) 
					transition.Dispose ();

				transition = value;

				if (transition != null)
					delay.Start ();
				else 
					delay.Stop ();
			}
		}

		public ImageDisplay (BrowsablePointer item) 
		{
			this.item = item;
			CanFocus = true;
			current = new ImageInfo (item.Current.DefaultVersionUri);
			if (item.Collection.Count > item.Index + 1) {
				next = new ImageInfo (item.Collection [item.Index + 1].DefaultVersionUri);
			}
			delay = new Delay (30, new GLib.IdleHandler (DrawFrame));
		}

		protected override void OnDestroyed ()
		{
			if (current != null) {
				current.Dispose ();
				current = null;
			}

		        if (next != null) {
				next.Dispose ();
				next = null;
			}
			Transition = null;
			
			if (effect != null)
				effect.Dispose ();
			
			base.OnDestroyed ();
		}
	
		public bool Up ()
		{
			Console.WriteLine ("Up");
			Transition = new CrossFade (current, next);
			return true;
		}

		public bool Down ()
		{
			Console.WriteLine ("down");
			Transition = new CrossFade (next, current);
			return true;
		}

		public bool Vingette ()
		{
			SoftFocus f = effect as SoftFocus;
			
			if (f == null) {
				f = new SoftFocus (current);
				effect = f;
			}

			QueueDraw ();
			return true;
		}

		public bool TiltImage (double radians)
		{
			Tilt t = effect as Tilt;

			if (t == null) {
				t = new Tilt (current);
				effect = t;
			}

			t.Angle += radians;

			QueueDraw ();

			return true;
		}

		public bool Pan ()
		{
			Console.WriteLine ("space");
			Transition = new Wipe (current, next);
			return true;
		}
		
		public bool RevealImage ()
		{
			Console.WriteLine ("r");
			Transition = new Reveal (current, next);
			return true;
		}

		public bool PushImage ()
		{
			Console.WriteLine ("p");
			Transition = new Push (current, next);
			return true;
		}


		public bool DrawFrame ()
		{
			if (Transition != null)
				Transition.OnEvent (this);

			return true;
		}
		
		private static void SetClip (Context ctx, Gdk.Rectangle area) 
		{
			ctx.MoveTo (area.Left, area.Top);
			ctx.LineTo (area.Right, area.Top);
			ctx.LineTo (area.Right, area.Bottom);
			ctx.LineTo (area.Left, area.Bottom);
			
			ctx.ClosePath ();
			ctx.Clip ();
		}
		
		private static void SetClip (Context ctx, Region region)
		{
			foreach (Gdk.Rectangle area in region.GetRectangles ()) {
				ctx.MoveTo (area.Left, area.Top);
				ctx.LineTo (area.Right, area.Top);
				ctx.LineTo (area.Right, area.Bottom);
				ctx.LineTo (area.Left, area.Bottom);
					
				ctx.ClosePath ();
			}
			ctx.Clip ();
		}

		private void OnExpose (Context ctx, Region region)
		{
			if (Transition != null) {
				bool done = false;
				foreach (Gdk.Rectangle area in region.GetRectangles ()) {
					BlockProcessor proc = new BlockProcessor (area, 256);
					Gdk.Rectangle subarea;
					while (proc.Step (out subarea)) {
						ctx.Save ();
						SetClip (ctx, subarea);
						done = ! Transition.OnExpose (ctx, Allocation);
						ctx.Restore ();
					}
				}
				if (done) {
					System.Console.WriteLine ("frames = {0}", Transition.Frames);
					Transition = null;
				}
			} else if (effect != null) {
				foreach (Gdk.Rectangle area in region.GetRectangles ()) {
					BlockProcessor proc = new BlockProcessor (area, 30000);
					Gdk.Rectangle subarea;
					while (proc.Step (out subarea)) {
						ctx.Save ();
						SetClip (ctx, subarea);
						effect.OnExpose (ctx, Allocation);
						ctx.Restore ();
					}
				}
			} else {
				ctx.Operator = Operator.Source;
				SurfacePattern p = new SurfacePattern (current.Surface);
				p.Filter = Filter.Fast;
				SetClip (ctx, region);
				ctx.Matrix = current.Fill (Allocation);

				ctx.Source = p;
				ctx.Paint ();
				p.Destroy ();
			}
		}

		protected override bool OnExposeEvent (EventExpose args)
		{
			bool double_buffer = false;
			base.OnExposeEvent (args);

			Context ctx = CairoUtils.CreateContext (GdkWindow);
			if (double_buffer) {
				ImageSurface cim = new ImageSurface (Format.RGB24, 
								     Allocation.Width, 
								     Allocation.Height);

				Context buffer = new Context (cim);
				OnExpose (buffer, args.Region);

				SurfacePattern sur = new SurfacePattern (cim);
				sur.Filter = Filter.Fast;
				ctx.Source = sur;
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

		~ImageDisplay () 
		{
			Transition = null;
			current.Dispose ();
			next.Dispose ();
		}

		private interface ITransition : IDisposable {
			bool OnExpose (Context ctx, Gdk.Rectangle allocation);
			bool OnEvent (Widget w);
			int Frames { get; }
		}

		private interface IEffect : IDisposable {
			bool OnExpose (Context ctx, Gdk.Rectangle allocation);
		}

		private class SoftFocus : IEffect {
			ImageInfo info;
			double radius;
			double amount = 30;
			ImageInfo cache;
			Gdk.Point center;

			public SoftFocus (ImageInfo info)
			{
				this.info = info;
				center.X = info.Bounds.Width / 2;
				center.Y = info.Bounds.Height / 2;
				radius = Math.Min (info.Bounds.Width, info.Bounds.Height) / 4.0;
			}

			public Gdk.Point Center {
				get { return center; }
				set { center = value; }
			}

			public double Amount {
				get { return amount; }
				set { amount = value; }
			}

			public double Radius {
				get { return radius; }
				set { radius = value; }
			}

			private ImageInfo CreateBlur (ImageInfo source)
			{
				ImageSurface image = new ImageSurface (Format.Argb32, 
								       source.Bounds.Width,
								       source.Bounds.Height);
				Context ctx = new Context (image);
				ctx.Source = new SurfacePattern (source.Surface);
				ctx.Matrix = source.Fill (source.Bounds);
				ctx.Paint ();
				Gdk.Pixbuf normal = CairoUtils.CreatePixbuf (image);
				Gdk.Pixbuf blur = PixbufUtils.Blur (normal, amount);
				ImageInfo overlay = new ImageInfo (blur);
				blur.Dispose ();
				normal.Dispose ();
				return overlay;
			}

			private Pattern CreateMask ()
			{
				RadialGradient circle = new RadialGradient (center.X, center.Y, radius *.7,
									    center.X, center.Y, radius);
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
				//ctx.Source = new SolidPattern (0.0, 0.0, 0.0, 0.0);
				ctx.Paint ();
				
				Console.WriteLine ("we started here");
				ImageInfo blur = CreateBlur (info);
				SurfacePattern overlay = new SurfacePattern (blur.Surface);
				ctx.Matrix = blur.Fill (allocation);
				ctx.Operator = Operator.Over;
				ctx.Source = overlay;
				Console.WriteLine ("did we make it here");

				ctx.Mask (CreateMask ());
				//ctx.Paint ();
				blur.Dispose ();
				//circle.Dispose ();
				p.Destroy ();
				return true;
			}

			public void Dispose ()
			{
			}
		}

		private class Tilt : IEffect {
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
		
		private class PanZoomOld : ITransition {
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

		private class PanZoom : ITransition {
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
					buffer = new ImageInfo (info, w, new Gdk.Rectangle (0, 0, (int) (info.Bounds.Width * scale), (int) (info.Bounds.Height * scale)));
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
					Console.WriteLine ("{0} {1} elapsed", DateTime.UtcNow, DateTime.UtcNow - start);
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

		private class Wipe : ITransition {
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

		private class Reveal : ITransition {
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


		private class Push : ITransition {
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

			public Push (ImageInfo begin, ImageInfo end)
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
				fraction = Math.Min (fraction, 1.0);

				ctx.Operator = Operator.Source;
				Matrix em = end_buffer.Fill (allocation);
				em.Translate (Math.Round (allocation.Width - allocation.Width * fraction), 0);
				ctx.Matrix = em;
				SurfacePattern sur = new SurfacePattern (end_buffer.Surface);
				sur.Filter = Filter.Fast;
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
				end_buffer.Dispose ();
				begin_buffer.Dispose ();
			}
		}

		private class CrossFade : ITransition {
			DateTime start;
			TimeSpan duration = new TimeSpan (0, 0, 1);
			ImageInfo begin;
			ImageInfo end;
			ImageInfo begin_buffer;
			ImageInfo end_buffer;
			int frames = 0;

			public CrossFade (ImageInfo begin, ImageInfo end)
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
				Pattern black = new SolidPattern (new Cairo.Color (0.0, 0.0, 0.0, opacity));
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

		public class ImageInfo : IDisposable {
			public Surface Surface;
			public Gdk.Rectangle Bounds;

			public ImageInfo (Uri uri)
			{
				ImageFile img = ImageFile.Create (uri);
				Pixbuf pixbuf = img.Load ();
				SetPixbuf (pixbuf);
				pixbuf.Dispose ();
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
#if false
				Surface = new ImageSurface (Format.RGB24,
							    allocation.Width,
							    allocation.Height);
				Context ctx = new Context (Surface);
#else
				Console.WriteLine ("source status = {0}", info.Surface.Status);
				Surface = info.Surface.CreateSimilar (Content.Color,
								      allocation.Width,
								      allocation.Height);
				
				System.Console.WriteLine ("status = {1} pointer = {0}", Surface.Handle.ToString (), Surface.Status);
				Context ctx = new Context (Surface);
#endif
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
				Surface = CairoUtils.CreateSurface (pixbuf);
				Bounds.Width = pixbuf.Width;
				Bounds.Height = pixbuf.Height;
			}

			public Matrix Fill (Gdk.Rectangle viewport) 
			{
				Matrix m = new Matrix ();
				m.InitIdentity ();
				
				double scale = Math.Max (viewport.Width / (double) Bounds.Width,
							 viewport.Height / (double) Bounds.Height);
				
				double x_offset = (viewport.Width  - Bounds.Width * scale) / 2.0;
				double y_offset = (viewport.Height  - Bounds.Height * scale) / 2.0;
				
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
				
				double scale = Math.Round (Math.Min (viewport.Width / (double) Bounds.Width,
							 viewport.Height / (double) Bounds.Height));
				
				double x_offset = Math.Round ((viewport.Width  - Bounds.Width * scale) / 2.0);
				double y_offset = Math.Round ((viewport.Height  - Bounds.Height * scale) / 2.0);
				
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
}

