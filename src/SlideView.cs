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

	int width = 640;
	int height = 480;

	public void Play () 
	{
		this.Pixbuf = GetScaled (photos[current].DefaultVersionPath);
		LoadNextImage ();
		StartTimer ();
	}

	private Pixbuf Blend (Pixbuf prev, Pixbuf next, double percent)
	{ 
		
		return null;
	}

	private Pixbuf GetScaled (string path)
	{
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

#if true
		orig.Composite (scaled, scale_x, scale_y, 
				scale_width, scale_height,
				scale_x, scale_y, scale, scale,
				Gdk.InterpType.Bilinear,
				255);
#endif
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
		this.FromPixbuf = next;
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
			timer = GLib.Timeout.Add (1000, new TimeoutHandler (HandleTimer));
	}
	
	public void Pause () 
	{
		StopTimer ();
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

		DestroyEvent += new DestroyEventHandler (HandleDestroyEvent);
	}
}
		
