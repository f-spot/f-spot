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

	[Glade.Widget] private SpinButton brightness_spinbutton;
	[Glade.Widget] private SpinButton contrast_spinbutton;
	[Glade.Widget] private SpinButton hue_spinbutton;
	[Glade.Widget] private SpinButton sat_spinbutton;


	[Glade.Widget] private Gtk.Image color_image;
	[Glade.Widget] private Gtk.Image histogram_image;

	Thread thread;

	FSpot.Histogram hist;

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
			hist.FillValues (AdjustedPixbuf);
			histogram_image.QueueDraw ();
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
			hist.FillValues (AdjustedPixbuf);
			histogram_image.Pixbuf = hist.GeneratePixbuf ();
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

		color_image.SizeAllocated += HandleSizeAllocate;
		color_image.DestroyEvent += HandleDestroyEvent;
		color_image.SetSizeRequest (320, 200);
		
		hist = new FSpot.Histogram ();
#if true
		Gdk.Color c = color_dialog.Style.Backgrounds [(int)Gtk.StateType.Active];
		hist.Color [0] = (byte) (c.Red / 0xff);
		hist.Color [1] = (byte) (c.Green / 0xff);
		hist.Color [2] = (byte) (c.Blue / 0xff);
		hist.Color [3] = 0xff;
#endif
		brightness_spinbutton.Adjustment = brightness_scale.Adjustment;
		contrast_spinbutton.Adjustment = contrast_scale.Adjustment;
		hue_spinbutton.Adjustment = hue_scale.Adjustment;
		sat_spinbutton.Adjustment = sat_scale.Adjustment;

		brightness_spinbutton.Adjustment.Change ();
		contrast_spinbutton.Adjustment.Change ();
		hue_spinbutton.Adjustment.Change ();
		sat_spinbutton.Adjustment.Change ();
		brightness_spinbutton.Adjustment.ChangeValue ();
		contrast_spinbutton.Adjustment.ChangeValue ();
		hue_spinbutton.Adjustment.ChangeValue ();
		sat_spinbutton.Adjustment.ChangeValue ();
		
		brightness_scale.ValueChanged += RangeChanged;
		contrast_scale.ValueChanged += RangeChanged;
		hue_scale.ValueChanged += RangeChanged;
		sat_scale.ValueChanged += RangeChanged;
		source_spinbutton.ValueChanged += RangeChanged;
		dest_spinbutton.ValueChanged += RangeChanged;
	}
}
