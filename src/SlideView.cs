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

	int current = 0;	
	uint flip_timer = 0;
	uint transition_timer = 0;

	public bool Running {
		get {
			return flip_timer != 0 || transition_timer != 0;
		}
	}

	public void Play () 
	{
		StopTweenIdle ();
		this.FromPixbuf = GetScaled (photos[current].DefaultVersionPath);
		if (PreloadNextImage ())
			StartFlipTimer ();
	}

	private Pixbuf Blend (Pixbuf current, Pixbuf prev, Pixbuf next, double percent)
	{ 
		int width = Allocation.width;
		int height = Allocation.height;

		prev.CopyArea (0, 0, width, height, current, 0, 0);
		next.Composite (current, 0,0, width, height, 0, 0, 1, 1,
				Gdk.InterpType.Bilinear, (int)(255 * percent + 0.5));
		return current;
	}

	private Pixbuf FadeBlack (Pixbuf current, Pixbuf prev, Pixbuf next, double percent)
	{ 
		int width = Allocation.width;
		int height = Allocation.height;

		current.Fill (0);		

		if (percent < 0.5)
			prev.Composite (current, 0,0, width, height, 0, 0, 1, 1,
					Gdk.InterpType.Bilinear, (int)(255 * (1 - percent * 2) + 0.5));
		else
			next.Composite (current, 0,0, width, height, 0, 0, 1, 1,
					Gdk.InterpType.Bilinear, (int)(255 * (percent * 2 - 1) + 0.5));
		return current;
	}

	private Pixbuf GetScaled (string path)
	{
		int width = Allocation.width;
		int height = Allocation.height;

		Pixbuf orig;
		Pixbuf scaled = new Pixbuf (Colorspace.Rgb, false, 8, width, height);
		scaled.Fill (0);
		
		if (width < 10 || height < 10)
			return scaled;	

		try {
		orig = new Pixbuf (path);
		} catch {
			Console.WriteLine ("Error loading file " + path);
			orig = null;
		}

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

	private bool PreloadNextImage ()
	{
		Pixbuf orig;
	
		current ++;
		if (current < photos.Length) {
			next = GetScaled (photos [current].DefaultVersionPath);
	
			StartTweenIdle ();
			return true;
		} else {
			current = 0;
			return false;
		}
	}
	
	private bool LoadPrevImage () 
	{
		if (current-- > 0)
			this.FromPixbuf = GetScaled (photos [current].DefaultVersionPath);
		else 
			current = 0;


		Console.WriteLine (current);
		return current > 0;		
	}

	private bool HandleFlipTimer ()
	{	
		StopTweenIdle ();
	
		StartTransitionTimer ();
			
		flip_timer = 0;
		return false;
	}

	public bool HandleTransitionTimer ()
	{			
		if (current_tween--  > 0) {

			this.FromPixbuf = tweens[current_tween];
			GdkWindow.ProcessUpdates (false);
			
			return true;
		} else {
			this.FromPixbuf = next;

			if (PreloadNextImage ());
				StartFlipTimer ();
		
		}
		
		transition_timer = 0;
		return false;			
	}

	private bool HandleTweenIdle ()
	{
		Pixbuf prev = this.Pixbuf;
	
		if (current_tween < tweens.Length && tweens[current_tween] == null) {
			tweens[current_tween] = new Pixbuf (Colorspace.Rgb, false, 8, Allocation.width, Allocation.height);
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
			transition_timer = GLib.Timeout.Add (50, new TimeoutHandler (HandleTransitionTimer));
	}

	private void StopTranstionTimer ()
	{
		if (transition_timer != 0) {
			GLib.Source.Remove (transition_timer);
		}
		transition_timer = 0;
	}
	
	private void StopFlipTimer ()
	{	
		if (flip_timer != 0) {
			GLib.Source.Remove (flip_timer);
		}
		flip_timer = 0;
	}
	
	private void StartFlipTimer ()
	{
		if (flip_timer == 0)
			flip_timer = GLib.Timeout.Add (2000, new TimeoutHandler (HandleFlipTimer));
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

	public bool Forward ()
	{
		this.FromPixbuf = next;
		return PreloadNextImage ();
	}
	
	public void Back ()
	{
		LoadPrevImage ();
	}

	private void HandleSizeAllocate (object sender, SizeAllocatedArgs args)
	{	
		if (Pixbuf == null)
			return;
	
		/*
		 * The size has changed so we need to reload the images.
		 */
		if (Pixbuf.Width != Allocation.width || Pixbuf.Height != Allocation.height) {
			this.FromPixbuf = GetScaled (photos[current].DefaultVersionPath);
			
			bool playing = (flip_timer != 0 || transition_timer != 0);
			Stop ();

			/* clear the tween images */
			for (int i = 0; i < tweens.Length; i++)
				tweens[i] = null;

			if (playing && current < photos.Length - 1)
				Play ();
		}
	}

	private void HandleDestroyed (object sender, EventArgs args)
	{
		Stop ();
	}

	public SlideView (Photo [] photos) : base ()
	{
		this.photos = photos;

		SizeAllocated += new SizeAllocatedHandler (HandleSizeAllocate);
		Destroyed += new EventHandler (HandleDestroyed);
	}
}
		
