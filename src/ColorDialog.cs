using System;
using Gtk;
using Gnome;
using System.Threading;

public class ColorDialog {
	Gdk.Pixbuf OrigPixbuf;
	Gdk.Pixbuf ScaledPixbuf;
	Gdk.Pixbuf AdjustedPixbuf;

	uint timeout;

	[Glade.Widget] private SpinButton source_spinbutton;
	[Glade.Widget] private SpinButton dest_spinbutton;

	[Glade.Widget] private HScale brightness_scale;
	[Glade.Widget] private HScale contrast_scale;
	[Glade.Widget] private HScale hue_scale;
	[Glade.Widget] private HScale sat_scale;

	[Glade.Widget] private Gtk.Image color_image;

	Thread thread;

	private void Adjust ()
	{
	        PixbufUtils.ColorAdjust (ScaledPixbuf,
					 AdjustedPixbuf,
					 brightness_scale.Value,
					 contrast_scale.Value,
					 hue_scale.Value,
					 sat_scale.Value,
					 source_spinbutton.ValueAsInt,
					 dest_spinbutton.ValueAsInt);
		
		lock (AdjustedPixbuf) {
			if (timeout == 0)
				timeout = GLib.Idle.Add (new GLib.IdleHandler (QueueDraw));
		}
	}

	public bool QueueDraw ()
	{
		lock (AdjustedPixbuf) {
			color_image.QueueDraw ();
			timeout = 0;
		}
		return false;
	}
	
	public void HandleDestroyEvent (object sender, DestroyEventArgs arg)
	{
		lock (AdjustedPixbuf) {
			if (timeout != 0)
				GLib.Source.Remove (timeout);
		}
	}

	public void RangeChanged (object sender, EventArgs args)
	{
		if (thread != null && thread.IsAlive)
			thread.Abort ();

		thread = new Thread (new ThreadStart (Adjust));
		thread.Start ();
	}

	public void HandleSizeAllocate (object sender, SizeAllocatedArgs args)
	{
		if (ScaledPixbuf == null || 
		    (OrigPixbuf != null &&
		     ScaledPixbuf.Width != color_image.Allocation.Width &&
		     ScaledPixbuf.Height != color_image.Allocation.Height)) {
			ScaledPixbuf = PixbufUtils.ScaleToMaxSize (OrigPixbuf, 
								   color_image.Allocation.Width,
								   color_image.Allocation.Height);

			color_image.Pixbuf = AdjustedPixbuf = ScaledPixbuf.Copy ();
			Adjust ();
		}
	}

	public ColorDialog (Gdk.Pixbuf pixbuf)       
	{
		Glade.XML xml = new Glade.XML (null, "f-spot.glade", "color_dialog", null);
		OrigPixbuf = pixbuf;

		xml.Autoconnect (this);

		brightness_scale.ValueChanged += RangeChanged;
		contrast_scale.ValueChanged += RangeChanged;
		hue_scale.ValueChanged += RangeChanged;
		sat_scale.ValueChanged += RangeChanged;
		source_spinbutton.ValueChanged += RangeChanged;
		dest_spinbutton.ValueChanged += RangeChanged;
		
		color_image.SizeAllocated += HandleSizeAllocate;
		color_image.DestroyEvent += HandleDestroyEvent;
		color_image.SetSizeRequest (320, 200);
	}
}
