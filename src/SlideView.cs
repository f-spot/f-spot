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
	uint timer = 0;
	uint transition_idle = 0;

	public void Play () 
	{
		this.FromPixbuf = GetScaled (photos[current].DefaultVersionPath);
		LoadNextImage ();
		StartTimer ();
	}

#if true
	private Pixbuf Blend (Pixbuf current, Pixbuf prev, Pixbuf next, double percent)
	{ 
		int width = Allocation.width;
		int height = Allocation.height;
		
		prev.CopyArea (0, 0, width, height, current, 0, 0);
		next.Composite (current, 0,0, width, height, 0, 0, 1, 1,
				Gdk.InterpType.Bilinear, (int)(255 * percent + 0.5));
		return current;
	}
#else
	private Pixbuf Blend (Pixbuf current, Pixbuf prev, Pixbuf next, double percent)
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
#endif

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

	private bool LoadNextImage ()
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
	
	public bool HandleTimer ()
	{	
		StopTweenIdle ();
		Console.WriteLine ("current_tween = " + current_tween);
			
		while (current_tween--  > 0) {
			this.FromPixbuf = tweens[current_tween];
			GdkWindow.ProcessUpdates (false);

			/*
			 * if some event occured that cleared the timer
			 * while we were drawing get out fast
			 */
			if (timer == 0)
				return false;
		}

		this.FromPixbuf = next;

		if (!LoadNextImage ()) {
			timer = 0;
			return false;
		}
		
		return true;			
	}

	private bool HandleTweenIdle ()
	{
		Pixbuf prev = this.Pixbuf;

		switch (current_tween) {
		case 9:
			tweens[current_tween] = Blend (tweens[current_tween], prev, next, .2);
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
			break;
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
			Console.WriteLine ("stopped tween_idle");
		}
		tween_idle = 0;
	
	}
	
	private void StopTimer ()
	{	
		if (timer != 0) {
			GLib.Source.Remove (timer);
			Console.WriteLine ("stopped timer");
		}
		timer = 0;
	}
	
	private void StartTimer ()
	{
		if (timer == 0)
			timer = GLib.Timeout.Add (2000, new TimeoutHandler (HandleTimer));
	}
	
	public void Pause () 
	{
		StopTimer ();
	}	

	private void HandleSizeAllocate (object sender, SizeAllocatedArgs args)
	{	
		if (Pixbuf == null)
			return;
	
		for (int i = 0; i < tweens.Length; i++) {
			tweens[i] = new Pixbuf (Colorspace.Rgb, false, 8, Allocation.width, Allocation.height);
		}

		/*
		 * The size has changed so we need to reload the images.
		 */
		if (Pixbuf.Width != Allocation.width || Pixbuf.Height != Allocation.height) {
			this.FromPixbuf = GetScaled (photos[current].DefaultVersionPath);
			if (current < photos.Length - 1)
				next = GetScaled (photos[current + 1].DefaultVersionPath);
		}
	}

	private void HandleDestroyed (object sender, EventArgs args)
	{
		StopTweenIdle ();
		StopTimer ();			
		
	}

	public SlideView (Photo [] photos) : base ()
	{
		this.photos = photos;

		SizeAllocated += new SizeAllocatedHandler (HandleSizeAllocate);
		Destroyed += new EventHandler (HandleDestroyed);
	}
}
		
