using System;
using Gtk;
using Gnome;
using System.Threading;

public class ColorDialog {
	PhotoQuery query;
	int item;

	Gdk.Pixbuf OrigPixbuf;
	Gdk.Pixbuf ScaledPixbuf;
	Gdk.Pixbuf AdjustedPixbuf;

	uint timeout;

	[Glade.Widget] private Dialog color_dialog;

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
	
	public void Save ()
	{
		Console.WriteLine ("Saving....");
		Photo photo = query.Photos[item];

		uint version = photo.DefaultVersionId;
		if (version == Photo.OriginalVersionId) {
			version = photo.CreateDefaultModifiedVersion (photo.DefaultVersionId, false);
		}

		Gdk.Pixbuf final = new Gdk.Pixbuf (Gdk.Colorspace.Rgb,
						   false, 8,
						   OrigPixbuf.Width, 
						   OrigPixbuf.Height);

	        PixbufUtils.ColorAdjust (OrigPixbuf,
					 final,
					 brightness_scale.Value,
					 contrast_scale.Value,
					 hue_scale.Value,
					 sat_scale.Value,
					 source_spinbutton.ValueAsInt,
					 dest_spinbutton.ValueAsInt);

		try {
			string version_path = photo.GetVersionPath (version);

			final.Savev (version_path, "jpeg", null, null);
			PhotoStore.GenerateThumbnail (version_path);
			photo.DefaultVersionId = version;
			query.Commit (item);
		} catch (GLib.GException ex) {
			// FIXME error dialog.
			Console.WriteLine ("error {0}", ex);
		}

		Console.WriteLine ("Saving....");
		color_dialog.Sensitive = false;
		color_dialog.Destroy ();
	}

	public void Cancel ()
	{
		color_dialog.Destroy ();
	}

        private void HandleOkClicked (object sender, EventArgs args)
	{
		Save ();
	}
	
	private void HandleCancelClicked (object sender, EventArgs args)
	{
		Cancel ();
	}
	
	public ColorDialog (PhotoQuery query, int item, Gdk.Pixbuf pixbuf)       
	{
		Glade.XML xml = new Glade.XML (null, "f-spot.glade", "color_dialog", null);
		OrigPixbuf = pixbuf;
		this.query = query;
		this.item = item;

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
