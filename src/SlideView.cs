using Gtk;
using Gdk;
using Gnome;
using GtkSharp;
using System;
using GLib;

public class SlideView : Gtk.Image {
	Photo [] photos;
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

	bool black = false;
	uint flip_interval = 2000;
	uint transition_interval = 75;

	public bool Running {
		get {
			return flip_timer != 0 || transition_timer != 0;
		}
	}

	public void Play () 
	{
		StopTweenIdle ();
		if (current_idx >= 0) {
			Pixbuf frame = GetScaled (photos[current_idx]);
			this.FromPixbuf = frame;
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
		Pixbuf orig;
	
		try {
			if (idx < photos.Length && idx >= 0) {
				//Console.WriteLine ("next_idx = " + next_idx + " idx = " + idx);
				next = GetScaled (photos [idx]);
				
				next_idx = idx;
				StartTweenIdle ();
				

				return true;
			} else {
				//Console.WriteLine ("What happens now?");
				next = GetScaled (photos [0]);
				next_idx = 0;
				StartTweenIdle ();
				
				return false;
			}
		} catch (GLib.GException e) {
			Console.WriteLine (e);
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
				Gdk.InterpType.Nearest, (int)(255 * percent + 0.5));
		return current;
	}

	private Pixbuf BlackFade (Pixbuf current, Pixbuf prev, Pixbuf next, double percent)
	{ 
		int width = Allocation.Width;
		int height = Allocation.Height;

		current.Fill (0);		

		if (percent < 0.5)
			prev.Composite (current, 0,0, width, height, 0, 0, 1, 1,
					Gdk.InterpType.Nearest, (int)(255 * (1 - percent * 2) + 0.5));
		else
			next.Composite (current, 0,0, width, height, 0, 0, 1, 1,
					Gdk.InterpType.Nearest, (int)(255 * (percent * 2 - 1) + 0.5));
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
		int width = Allocation.Width;
		int height = Allocation.Height;
		Pixbuf scaled = new Pixbuf (Colorspace.Rgb, false, 8, width, height);
		scaled.Fill (0);
		
		if (width < 10 || height < 10)
			return scaled;	

		if (orig == null) {
			return scaled;
		}

		double scale = Math.Min (width / (double)orig.Width, height / (double)orig.Height);

		int scale_width = (int)(scale * orig.Width);
		int scale_height = (int)(scale * orig.Height);
		int scale_x = (width - scale_width) / 2;
		int scale_y = (height - scale_height) / 2;

		orig.Composite (scaled, scale_x, scale_y, 
				scale_width, scale_height,
				scale_x, scale_y, scale, scale,
				Gdk.InterpType.Bilinear,
				255);

		orig.Dispose ();
		System.GC.Collect ();
		return scaled;
	}

	private Pixbuf GetScaled (Photo photo)
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

			this.FromPixbuf = tweens[current_tween];
			GdkWindow.ProcessUpdates (false);
		} else {
			ShowNext ();

			if (PreloadNextImage (current_idx + 1));
				StartFlipTimer ();
		}
		
		return false;			
	}

	private bool HandleTweenIdle ()
	{
		using (Pixbuf prev = this.Pixbuf) {	
		if (current_tween < tweens.Length && tweens[current_tween] == null) {
			tweens[current_tween] = new Pixbuf (Colorspace.Rgb, false, 8, Allocation.Width, Allocation.Height);
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
					this.FromPixbuf = GetScaled (this.Pixbuf);
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

	public SlideView (Pixbuf background, Photo [] photos) : base ()
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
		
