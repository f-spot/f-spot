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

	int current = 0;	
	uint timer = 0;

	public void Play () 
	{
		this.FromPixbuf = GetScaled (photos[current].DefaultVersionPath);
		LoadNextImage ();
		StartTimer ();
	}

#if false
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
			return true;
		} else {
			current = 0;
			return false;
		}
	}
	
	public bool HandleTimer ()
	{	
		Pixbuf prev = this.Pixbuf;
		Pixbuf current = new Pixbuf (Colorspace.Rgb, false, 8, Allocation.width, Allocation.height);

		try {
			this.FromPixbuf = Blend (current, prev, next, .1);
			GdkWindow.ProcessUpdates (false);
			this.FromPixbuf = Blend (current, prev, next, .2);
			GdkWindow.ProcessUpdates (false);
			this.FromPixbuf = Blend (current, prev, next, .3);
			GdkWindow.ProcessUpdates (false);
			this.FromPixbuf = Blend (current, prev, next, .4);
			GdkWindow.ProcessUpdates (false);
			this.FromPixbuf = Blend (current, prev, next, .5);
			GdkWindow.ProcessUpdates (false);
			this.FromPixbuf = Blend (current, prev, next, .6);
			GdkWindow.ProcessUpdates (false);
			this.FromPixbuf = Blend (current, prev, next, .7);
			GdkWindow.ProcessUpdates (false);
			this.FromPixbuf = Blend (current, prev, next, .8);
			GdkWindow.ProcessUpdates (false);
			this.FromPixbuf = Blend (current, prev, next, .9);
			GdkWindow.ProcessUpdates (false);
			this.FromPixbuf = Blend (current, prev, next, .97);
			GdkWindow.ProcessUpdates (false);
			this.FromPixbuf = Blend (current, prev, next, .99);
			GdkWindow.ProcessUpdates (false);
			this.FromPixbuf = next;
			GdkWindow.ProcessUpdates (false);
		} catch {
			timer = 0;
			return false;
		}
			

		if (!LoadNextImage ()) {
			timer = 0;
			return false;
		}
		return true;			
	}

	private void StopTimer ()
	{	
		if (timer != 0)
			GLib.Source.Remove (timer);
	
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
		/*
		 * The size has changed so we need to reload the images.
		 */
		if (Pixbuf.Width != Allocation.width || Pixbuf.Height != Allocation.height) {
			this.FromPixbuf = GetScaled (photos[current].DefaultVersionPath);
			if (current < photos.Length - 1)
				next = GetScaled (photos[current + 1].DefaultVersionPath);
		}
	}

	private void HandleDestroyEvent (object sender, DestroyEventArgs args)
	{
		StopTimer ();			

		next.Dispose ();
		next = null;
		
		photos = null;		
	}

	public SlideView (Photo [] photos) : base ()
	{
		this.photos = photos;

		SizeAllocated += new SizeAllocatedHandler (HandleSizeAllocate);
		DestroyEvent += new DestroyEventHandler (HandleDestroyEvent);
	}
}
		
