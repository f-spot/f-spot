using Gtk;
using Gdk;
using Gnome;
using GtkSharp;
using System;
using GLib;
using System.Runtime.InteropServices;
using FSpot;
using FSpot.Utils;

namespace FSpot {
	public class XScreenSaverSlide : Gtk.Window {
		public const string ScreenSaverEnviroment = "XSCREENSAVER_WINDOW";

		public XScreenSaverSlide () : base (String.Empty)
		{
		}
	       
		protected override void OnRealized ()
		{
			string env = Environment.GetEnvironmentVariable (ScreenSaverEnviroment);
			
			if (env != null) {
				try {
					env = env.ToLower ();
					
					if (env.StartsWith ("0x"))
						env = env.Substring (2);

					uint xid = UInt32.Parse (env, System.Globalization.NumberStyles.HexNumber);
					
					GdkWindow = Gdk.Window.ForeignNew (xid);
					Style.Attach (GdkWindow);
					GdkWindow.Events = EventMask.ExposureMask 
						| EventMask.StructureMask 
						| EventMask.EnterNotifyMask 
						| EventMask.LeaveNotifyMask 
						| EventMask.FocusChangeMask;
					
					Style.SetBackground (GdkWindow, Gtk.StateType.Normal);
					GdkWindow.SetDecorations ((Gdk.WMDecoration) 0);
					GdkWindow.UserData = this.Handle;
					SetFlag (WidgetFlags.Realized);
					SizeRequest ();
					Gdk.Rectangle geom;
					int depth;
					GdkWindow.GetGeometry (out geom.X, out geom.Y, out geom.Width, out geom.Height, out depth);
					SizeAllocate (new Gdk.Rectangle (geom.X, geom.Y, geom.Width, geom.Height));
					Resize (geom.Width, geom.Height);
					return;
				} catch (System.Exception e) {
					System.Console.WriteLine (e);
				}
			} else {
				System.Console.WriteLine ("{0} not set, falling back to window", ScreenSaverEnviroment);
			}

			SetSizeRequest (640, 480);
			base.OnRealized ();
		}
	}

	public class FullSlide : Gtk.Window {
		private SlideView slideview;
		private Gdk.Pixbuf screenshot;
		private Delay hide;
		private Gdk.Cursor busy;
		private Gdk.Cursor none;
		
		public FullSlide (Gtk.Window parent, IBrowsableItem [] items) : base ("Slideshow")
		{
			screenshot =  PixbufUtils.LoadFromScreen (parent.GdkWindow);
			
			this.Destroyed += HandleDestroyed;

			this.TransientFor = parent;

			this.ButtonPressEvent += HandleSlideViewButtonPressEvent;
			this.KeyPressEvent += HandleSlideViewKeyPressEvent;
			this.AddEvents ((int) (EventMask.ButtonPressMask | EventMask.KeyPressMask | EventMask.PointerMotionMask));
			slideview = new SlideView (screenshot, items);
			this.Add (slideview);
			this.Decorated = false;
			this.Fullscreen();
			this.Realize ();

			busy = new Gdk.Cursor (Gdk.CursorType.Watch);
			this.GdkWindow.Cursor = busy;
			none = GdkUtils.CreateEmptyCursor (GdkWindow.Display);

			hide = new Delay (2000, new GLib.IdleHandler (HideCursor));
		}

		public void Play ()
		{
			Gdk.GCValues values = new Gdk.GCValues ();
			values.SubwindowMode = SubwindowMode.IncludeInferiors;
			Gdk.GC fillgc = new Gdk.GC (this.GdkWindow, values, Gdk.GCValuesMask.Subwindow);
			
			slideview.Show ();
			this.GdkWindow.SetBackPixmap (null, false);
			this.Show ();
			screenshot.RenderToDrawable (this.GdkWindow, fillgc, 
						     0, 0, 0, 0, -1, -1, RgbDither.Normal, 0, 0);
			
			slideview.Play ();
			hide.Start ();
		}
		
		[GLib.ConnectBefore]
		private void HandleSlideViewKeyPressEvent (object sender, KeyPressEventArgs args)
		{
			this.Destroy ();
			args.RetVal = true;
		}

		protected override bool OnMotionNotifyEvent (Gdk.EventMotion args)
		{
			base.OnMotionNotifyEvent (args);
			this.GdkWindow.Cursor = busy;
			hide.Start ();
			return true;
		}
		
		private bool HideCursor ()
		{
			this.GdkWindow.Cursor = none;
			return false;
		}
		
		private void HandleDestroyed (object sender, System.EventArgs args)
		{
			hide.Stop ();
		}

		private void HandleSlideViewButtonPressEvent (object sender, ButtonPressEventArgs args)
		{
			this.Destroy ();
			args.RetVal = true;
		}
	}
	
	public class SlideView : Gtk.Image {
		IBrowsableItem [] photos;
		Pixbuf last;
		Pixbuf next;
		
		
		Pixbuf [] tweens = new Pixbuf [10];	
		int current_tween;
		uint tween_idle;
		uint resize_idle;
		
		int current_idx = 0;	
		int next_idx = 0;
		
		uint flip_timer = 0;
		uint transition_timer = 0;
		
		uint fail_count = 0;
		bool animate = true;
		uint animate_max = 200;
		
		bool black = false;
		uint flip_interval = 2000;
		uint transition_interval = 75;
		
		public bool Running {
			get {
				return flip_timer != 0 || transition_timer != 0;
			}
		}
		
		public bool Animate {
			get { return animate; }
			set { animate = value; }
		}
		
		public void Play () 
		{
			if (photos.Length < 1)
				return;

			StopTweenIdle ();
			if (current_idx >= 0) {
				Pixbuf frame = GetScaled (photos[current_idx]);
				this.Pixbuf = frame;
				frame.Dispose ();
			} 
			
			if (PreloadNextImage (current_idx + 1))
				StartFlipTimer ();
		}
		
		public void Pause () 
		{
			StopTranstionTimer ();
			StopFlipTimer ();
		}
		
		public void Stop ()
		{
			StopTweenIdle ();
			StopTranstionTimer ();
			StopFlipTimer ();
		}
		
		public void Forward ()
		{
			if (PreloadNextImage (current_idx + 1))
				ShowNext ();
		}
		
		public void Back ()
		{
			if (PreloadNextImage (current_idx - 1))
				ShowNext ();
		}	
		
		private void ShowNext ()
		{
			StopTweenIdle ();
			
			if (current_idx != next_idx && next != null)
				this.Pixbuf = next;
			
			current_idx = next_idx;
			
			black = false;
			transition_interval = 75;
			flip_interval = 2000;
		}
		
		private bool PreloadNextImage (int idx)
		{
			try {
				if (idx < photos.Length && idx >= 0) {
					if (next != null)
						next.Dispose ();
					
					next = GetScaled (photos [idx]);
					if (next == null)
						next = GetScaled (PixbufUtils.ShallowCopy (PixbufUtils.ErrorPixbuf));
					
					next_idx = idx;
					StartTweenIdle ();
					
					return true;
				} else {
					if (next != null)
						next.Dispose ();

					next = GetScaled (photos [0]);
					if (next == null)
						next = GetScaled (PixbufUtils.ShallowCopy (PixbufUtils.ErrorPixbuf));
					next_idx = 0;
					StartTweenIdle ();
					
					return false;
				}
			} catch (GLib.GException e) {
				System.Console.WriteLine (e);
				idx = (idx + 1) % photos.Length;
				return PreloadNextImage (idx);
			}
		}
		
		private Pixbuf CrossFade (Pixbuf current, Pixbuf prev, Pixbuf next, double percent)
		{ 
			Rectangle area = new Rectangle (0, 0, Allocation.Width, Allocation.Height);
			BlockProcessor proc = new BlockProcessor (area, 256);
			Rectangle subarea;

			while (proc.Step (out subarea)) {
				if (IsRealized)
					GdkWindow.ProcessUpdates (false);
				
				prev.CopyArea (subarea.X, subarea.Y, subarea.Width, subarea.Height, current, subarea.X, subarea.Y);
				next.Composite (current, subarea.X, subarea.Y, subarea.Width, subarea.Height, 0, 0, 1, 1,
						Gdk.InterpType.Nearest, (int) System.Math.Round (255 * percent));
			}
			return current;
		}
		
		private Pixbuf BlackFade (Pixbuf current, Pixbuf prev, Pixbuf next, double percent)
		{ 
			int width = Allocation.Width;
			int height = Allocation.Height;
			
			current.Fill (0);		
			
			if (percent < 0.5)
				prev.Composite (current, 0,0, width, height, 0, 0, 1, 1,
						Gdk.InterpType.Nearest, (int)System.Math.Round (255  * (1 - percent * 2)));
			else
				next.Composite (current, 0,0, width, height, 0, 0, 1, 1,
						Gdk.InterpType.Nearest, (int)System.Math.Round (255 * (percent * 2 - 1)));
			return current;
		}
		
		private Pixbuf Blend (Pixbuf current, Pixbuf prev, Pixbuf next, double percent)
		{
			if (black) {
				return BlackFade (current, prev, next, percent);
			} else {
				return CrossFade (current, prev, next, percent);
			}
		}
		
		private Pixbuf GetScaled (Pixbuf orig)
		{
			Gdk.Rectangle pos;
			int width = Allocation.Width;
			int height = Allocation.Height;
			double scale = PixbufUtils.Fit (orig, width, height, false, out pos.Width, out pos.Height);
			pos.X = (width - pos.Width) / 2;
			pos.Y = (height - pos.Height) / 2;
			
			Pixbuf scaled = new Pixbuf (Colorspace.Rgb, false, 8, width, height);
			scaled.Fill (0x000000); 
			
			Rectangle rect = new Rectangle (pos.X, pos.Y, 256, 256);
			Rectangle subarea;
			
			while (rect.Top < pos.Bottom) {
				while (rect.X < pos.Right) {
					if (IsRealized) 
						GdkWindow.ProcessUpdates (false);

					rect.Intersect (pos, out subarea);
					orig.Composite (scaled, subarea.X, subarea.Y, 
							subarea.Width, subarea.Height,
							pos.X, pos.Y, scale, scale,
							Gdk.InterpType.Bilinear,
							255);
					rect.X += rect.Width;
				}
				rect.X = pos.X;
				rect.Y += rect.Height;
			}
			
			orig.Dispose ();
			return scaled;
		}
		
		private Pixbuf GetScaled (IBrowsableItem photo)
		{
			Pixbuf orig;
			try { 
				orig = FSpot.PhotoLoader.LoadAtMaxSize (photo, Allocation.Width, Allocation.Height);
			} catch {
				orig = null;
			}

			if (orig == null)
				return null;

			Pixbuf result = GetScaled (orig);
			if (orig != result)
				orig.Dispose ();
			
			return result;
		}
		
		private bool HandleFlipTimer ()
		{	
			StopTweenIdle ();
			
			StartTransitionTimer ();
			
			flip_timer = 0;
			return false;
		}
		
		private bool HandleTransitionTimer ()
		{			
			System.DateTime start_time = System.DateTime.Now;
			transition_timer = 0;
			if (current_tween--  > 0) {
				StartTransitionTimer ();
				this.Pixbuf = tweens[current_tween];
				GdkWindow.ProcessUpdates (false);
				System.TimeSpan span = System.DateTime.Now  - start_time;
				
				if (Animate) { 
					if (span.TotalMilliseconds > animate_max) {
						fail_count++;
						
						if (fail_count > 3) {
							Animate = false;
							System.Console.WriteLine ("Disabling slide animation due to 3 consecutive excessive frame intervals {0}ms", 
										  span.TotalMilliseconds);
							current_tween = 0;
						}
					} else {
						fail_count = 0;
					}
				} 
			} else {
				ShowNext ();

				PreloadNextImage (current_idx + 1);
				StartFlipTimer ();
			}
			
			return false;			
		}

		
		private bool HandleTweenIdle ()
		{
			using (Pixbuf prev = this.Pixbuf) {	
				if (!Animate) {
					ClearTweens ();
					return false;
				}
				
				if (photos.Length < 2) { // Only one photo. Nothing to do
					ClearTweens ();
					return false;
				}
				
				if (current_tween >= tweens.Length) {
					tween_idle = 0;
					return false;
				}

				if (current_tween < tweens.Length && tweens[current_tween] == null) {
					tweens[current_tween] = new Pixbuf (Colorspace.Rgb, false, 8, 
									    Allocation.Width, Allocation.Height);
				}

				double blend_val;
#if USE_EXP
				double blend_t = (-10 * current_tween) / ((double)tweens.Length - 1);
				blend_val = 1.0 - (.01 / (.01 + (.99 * Math.Exp(blend_t))));
#else
				double [] blends = new double [] { .99, .97, .9, .8, .7, .6, .5, .4, .3, .15};
				blend_val = blends [current_tween];
#endif
				tweens[current_tween] = Blend (tweens[current_tween], prev, next, blend_val);
				current_tween++;
				return true;
			}
		}	
		
		private void StartTweenIdle () 
		{
			if (tween_idle == 0) {
				current_tween = 0;	
				tween_idle = GLib.Idle.Add (new GLib.IdleHandler (HandleTweenIdle));
			}
		}
		
		private void StopTweenIdle ()
		{
			if (tween_idle != 0) {
				GLib.Source.Remove (tween_idle);
			}
			tween_idle = 0;
	
		}
		
		private void StartTransitionTimer ()
		{
			if (transition_timer == 0)
				transition_timer = GLib.Timeout.Add (transition_interval, 
								     new TimeoutHandler (HandleTransitionTimer));
		}
		
		private void StopTranstionTimer ()
		{
			if (transition_timer != 0)
				GLib.Source.Remove (transition_timer);

			transition_timer = 0;
		}
		
		private void StartFlipTimer ()
		{
			if (flip_timer == 0)
				flip_timer = GLib.Timeout.Add (flip_interval, 
							       new TimeoutHandler (HandleFlipTimer));
		}
		
		private void StopFlipTimer ()
		{	
			if (flip_timer != 0)
				GLib.Source.Remove (flip_timer);

			flip_timer = 0;
		}

		
		private void HandleSizeAllocate (object sender, SizeAllocatedArgs args)
		{	
			Pixbuf current = this.Pixbuf;

			if (current == null)
				return;

			//
			// The size has changed so we need to reload the images.
			//
			if (current.Width != Allocation.Width || current.Height != Allocation.Height) {
				bool playing = (flip_timer != 0 || transition_timer != 0);
				
				if (current_idx < 0) {
					using (Gdk.Pixbuf old = this.Pixbuf) {
						this.Pixbuf = GetScaled (old);
						current.Dispose ();
					}
				} else {
					using (Pixbuf frame =  GetScaled (photos[current_idx])) {
						this.Pixbuf =  frame;
						current.Dispose ();
					}
				}
				
				Stop ();
				
				ClearTweens ();
				
				if (playing && current_idx != next_idx)
					Play ();
				
				
			}
		}

		private void ClearTweens () {
			for (int i = 0; i < tweens.Length; i++) {
				if (tweens[i] != null) 
					tweens[i].Dispose ();
				tweens[i] = null;
			}
		}
		
		private void HandleDestroyed (object sender, EventArgs args)
		{
			ClearTweens ();
			Stop ();
		}

		public SlideView (Pixbuf background, IBrowsableItem [] photos) : base ()
		{
			this.photos = photos;

			if (background != null) {
				this.Pixbuf = background;
				background.Dispose ();
				
				current_idx = -1;
				black = true;
				flip_interval = 1500;
			}
			
			SizeAllocated += new SizeAllocatedHandler (HandleSizeAllocate);
			Destroyed += new EventHandler (HandleDestroyed);
		}
	}
}		
