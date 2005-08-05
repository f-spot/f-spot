using Gtk;
using Gdk;
using Gnome;
using GtkSharp;
using System;
using GLib;
using System.Runtime.InteropServices;

namespace FSpot {
	public class FullSlide : Gtk.Window {
		private SlideView slideview;
		private Gdk.Pixbuf screenshot;
		private Delay hide;
		private Gdk.Cursor busy;
		private Gdk.Cursor none;
		
		public FullSlide (Gtk.Window parent, IBrowsableItem [] items) : base ("Slideshow")
		{
			screenshot =  PixbufUtils.LoadFromScreen (parent.GdkWindow);
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
			none = Empty ();

			hide = new Delay (2000, new GLib.IdleHandler (HideCursor));
		}

		[DllImport("libgdk-2.0-0.dll")]
		static extern IntPtr gdk_cursor_new_from_pixbuf (IntPtr display, IntPtr pixbuf, int x, int y);

		public Gdk.Cursor Empty () 
		{
			Gdk.Cursor cempty = null;
			
			try {
				Gdk.Pixbuf empty = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, true, 8, 1, 1);
			        empty.Fill (0x00000000);
				IntPtr raw = gdk_cursor_new_from_pixbuf (this.GdkWindow.Display.Handle, empty.Handle, 0, 0);
				cempty = new Gdk.Cursor (raw);
			} catch (System.Exception e){
				System.Console.WriteLine (e.ToString ());
				return null;
			}

			return cempty;
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
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			hide.Stop ();

			this.busy.Unref ();
			if (this.none != null)
				this.none.Unref ();
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
		
		bool animate = true;
		uint animate_max = 160;
		
		bool black = false;
		uint flip_interval = 2000;
		uint transition_interval = 75;
		
		public bool Running {
			get {
				return flip_timer != 0 || transition_timer != 0;
			}
		}
		
		public bool Animate {
			get {
				return animate;
			}
			set {
				animate = value;
			}
		}
		
		public void Play () 
		{
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
				this.FromPixbuf = next;
			
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
					
					
					next_idx = idx;
					StartTweenIdle ();
					

					return true;
				} else {
					next.Dispose ();
					next = GetScaled (photos [0]);
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
			int width = Allocation.Width;
			int height = Allocation.Height;
			
			prev.CopyArea (0, 0, width, height, current, 0, 0);
			next.Composite (current, 0,0, width, height, 0, 0, 1, 1,
					Gdk.InterpType.Nearest, (int) System.Math.Round (255 * percent));
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
			Pixbuf scaled = PixbufUtils.ScaleToAspect (orig, Allocation.Width, Allocation.Height);
			
			orig.Dispose ();
			return scaled;
		}
		
		private Pixbuf GetScaled (IBrowsableItem photo)
		{
			Pixbuf orig = FSpot.PhotoLoader.LoadAtMaxSize (photo, Allocation.Width, Allocation.Height);
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
			transition_timer = 0;
			if (current_tween--  > 0) {
				StartTransitionTimer ();
				System.DateTime start_time = System.DateTime.Now;
				this.FromPixbuf = tweens[current_tween];
				GdkWindow.ProcessUpdates (false);
				System.TimeSpan span = System.DateTime.Now  - start_time;
				
				if (animate && span.TotalMilliseconds > animate_max) {
					animate = false;
					System.Console.WriteLine ("Disabling slide animation due to excessive frame interval {0}ms", 
								  span.TotalMilliseconds);
					current_tween = 0;
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
				if (!animate) {
					ClearTweens ();
					return false;
				}
				
				if (current_tween < tweens.Length && tweens[current_tween] == null) {
					tweens[current_tween] = new Pixbuf (Colorspace.Rgb, false, 8, 
									    Allocation.Width, Allocation.Height);
				}
				
				switch (current_tween) {
				case 9:
					tweens[current_tween] = Blend (tweens[current_tween], prev, next, .15);
					break;
				case 8:
					tweens[current_tween] = Blend (tweens[current_tween], prev, next, .3);
					break;
				case 7:
					tweens[current_tween] = Blend (tweens[current_tween], prev, next, .4);
					break;
				case 6:
					tweens[current_tween] = Blend (tweens[current_tween], prev, next, .5);
					break;
				case 5:
					tweens[current_tween] = Blend (tweens[current_tween], prev, next, .6);
					break;
				case 4:
					tweens[current_tween] = Blend (tweens[current_tween], prev, next, .7);
					break;
				case 3:
					tweens[current_tween] = Blend (tweens[current_tween], prev, next, .8);
					break;
				case 2:
					tweens[current_tween] = Blend (tweens[current_tween], prev, next, .9);
					break;
				case 1:
					tweens[current_tween] = Blend (tweens[current_tween], prev, next, .97);
				break;
				case 0:
					tweens[current_tween] = Blend (tweens[current_tween], prev, next, .99);
					break;
				default:
					tween_idle = 0;
					return false;
				}
				
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
			if (transition_timer != 0) {
				GLib.Source.Remove (transition_timer);
			}
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
			if (flip_timer != 0) {
				GLib.Source.Remove (flip_timer);
			}
			flip_timer = 0;
		}

		
		private void HandleSizeAllocate (object sender, SizeAllocatedArgs args)
		{	
			if (Pixbuf == null)
				return;
			
			//
			// The size has changed so we need to reload the images.
			//
			Pixbuf current = this.Pixbuf;
			if (current.Width != Allocation.Width || current.Height != Allocation.Height) {
				bool playing = (flip_timer != 0 || transition_timer != 0);
				
				if (current_idx < 0) {
					Gdk.Pixbuf old = this.Pixbuf;
					this.Pixbuf = GetScaled (this.Pixbuf);
					if (old != this.Pixbuf)
						old.Dispose ();
				} else {
					using (Pixbuf frame =  GetScaled (photos[current_idx])) {
						this.FromPixbuf =  frame;
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
				this.FromPixbuf = background;
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
