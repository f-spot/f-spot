using System;
using Gtk;
using System.Threading;

namespace FSpot {
	public class ColorDialog {
		Gdk.Pixbuf ScaledPixbuf;
		Gdk.Pixbuf AdjustedPixbuf;
		
		Delay expose_timeout;

		[Glade.Widget] private Gtk.Dialog external_color_dialog;
		[Glade.Widget] private Gtk.Dialog inline_color_dialog;
		
		[Glade.Widget] private Gtk.SpinButton source_spinbutton;
		[Glade.Widget] private Gtk.SpinButton dest_spinbutton;

		[Glade.Widget] private Gtk.HScale brightness_scale;
		[Glade.Widget] private Gtk.HScale contrast_scale;
		[Glade.Widget] private Gtk.HScale hue_scale;
		[Glade.Widget] private Gtk.HScale sat_scale;
		
		[Glade.Widget] private Gtk.SpinButton brightness_spinbutton;
		[Glade.Widget] private Gtk.SpinButton contrast_spinbutton;
		[Glade.Widget] private Gtk.SpinButton hue_spinbutton;
		[Glade.Widget] private Gtk.SpinButton sat_spinbutton;
		
		[Glade.Widget] private Gtk.ScrolledWindow view_scrolled;
		[Glade.Widget] private Gtk.Image histogram_image;
		
		private FSpot.PhotoImageView view;

		Cms.Transform next_transform;

		Thread thread;
		
		FSpot.Histogram hist;
		
		private void Adjust ()
		{
			Cms.Profile srgb = Cms.Profile.CreateSRgb ();
			Cms.Profile bchsw = Cms.Profile.CreateAbstract (10, brightness_scale.Value,
									contrast_scale.Value,
									hue_scale.Value, 
									sat_scale.Value,
									source_spinbutton.ValueAsInt, 
									dest_spinbutton.ValueAsInt);
			
			Cms.Profile [] list = new Cms.Profile [] { srgb, bchsw, srgb };
			next_transform = new Cms.Transform (list, 
							    PixbufUtils.PixbufCmsFormat (AdjustedPixbuf),
							    PixbufUtils.PixbufCmsFormat (AdjustedPixbuf),
							    Cms.Intent.Perceptual, 0x0100);
			
			lock (AdjustedPixbuf) {
				PixbufUtils.ColorAdjust (ScaledPixbuf,
							 AdjustedPixbuf,
							 brightness_scale.Value,
							 contrast_scale.Value,
							 hue_scale.Value,
							 sat_scale.Value,
							 source_spinbutton.ValueAsInt,
							 dest_spinbutton.ValueAsInt);
				expose_timeout.Start ();
			}
		}
			
		public bool QueueDraw ()
		{
			lock (AdjustedPixbuf) {
				if (view.Transform != null)
					view.Transform.Dispose ();
				
				view.Transform = next_transform;
				view.QueueDraw ();
				
				hist.FillValues (AdjustedPixbuf);
				histogram_image.QueueDraw ();
			}
			return false;
		}
		
		public void HandleDestroyEvent (object sender, DestroyEventArgs arg)
		{
			expose_timeout.Stop ();
		}
		
		public void RangeChanged (object sender, EventArgs args)
		{
			if (!view.CurrentPhotoValid ())
				return;

			if (thread != null && thread.IsAlive)
				thread.Abort ();
			
			thread = new Thread (new ThreadStart (Adjust));
			thread.Start ();
		}
		
		public void Save ()
		{
			if (!view.CurrentPhotoValid ())
				return;

			Console.WriteLine ("Saving....");
			Photo photo = view.Query.Photos[view.CurrentPhoto];
			Exif.ExifData data = new Exif.ExifData (photo.DefaultVersionPath);
			
			uint version = photo.DefaultVersionId;
			if (version == Photo.OriginalVersionId) {
				version = photo.CreateDefaultModifiedVersion (photo.DefaultVersionId, false);
			}
			
			Gdk.Pixbuf orig = view.CompletePixbuf ();
			
			Gdk.Pixbuf final = new Gdk.Pixbuf (Gdk.Colorspace.Rgb,
							   false, 8,
							   orig.Width, 
							   orig.Height);
			
			PixbufUtils.ColorAdjust (orig,
						 final,
						 view.Transform);
			
			try {
				string version_path = photo.GetVersionPath (version);

				PixbufUtils.SaveJpeg (final, version_path, 95, data);
				ThumbnailGenerator.Create (version_path).Dispose ();
				photo.DefaultVersionId = version;
				view.Query.Commit (view.CurrentPhoto);
			} catch (GLib.GException ex) {
				// FIXME error dialog.
				Console.WriteLine (ex.ToString ());
			}
			
			Console.WriteLine ("Saving....");
			color_dialog.Sensitive = false;
			color_dialog.Destroy ();
		}
		
		public void Cancel ()
		{
			view.Transform = null;
			view.QueueDraw ();
			view.PhotoChanged -= HandlePhotoChanged;
			color_dialog.Destroy ();
		}
		
		private void HandleOkClicked (object sender, EventArgs args)
		{
			Save ();
			view.Transform = null;
			view.QueueDraw ();
			view.PhotoChanged -= HandlePhotoChanged;
		}

		private void HandlePhotoChanged (PhotoImageView view)
		{
			if (view.CurrentPhotoValid ()) {
				AdjustedPixbuf = PhotoLoader.LoadAtMaxSize (view.Query.Photos [view.CurrentPhoto], 300, 300);
				ScaledPixbuf = AdjustedPixbuf.Copy ();			
				RangeChanged (null, null);
			}
		}
		
		private void HandleCancelClicked (object sender, EventArgs args)
		{
			Cancel ();
		}
		
		private Gtk.Dialog color_dialog
		{
			get {
				if (external_color_dialog != null)
					return external_color_dialog;
				else
					return inline_color_dialog;
			}
		}
		
		public ColorDialog (FSpot.PhotoQuery query, int item)
		{
			view = new FSpot.PhotoImageView (query);
			view_scrolled.Add (view);
			view.Show ();
			view.CurrentPhoto = item;

			AttachInterface ("external_color_dialog");
		}

		public ColorDialog (FSpot.PhotoImageView view)       
		{
			this.view = view;
			AttachInterface ("inline_color_dialog");
		}

		private void AttachInterface (string ui_path)
		{
			Glade.XML xml = new Glade.XML (null, "f-spot.glade", ui_path, null);
			/*
			if (!view.CurrentPhotoValid ())
				return;
			*/
			HandlePhotoChanged (view);
			view.PhotoChanged += HandlePhotoChanged;
			xml.Autoconnect (this);


			hist = new FSpot.Histogram ();
			expose_timeout = new FSpot.Delay (new GLib.IdleHandler (this.QueueDraw));


			#if true
			Gdk.Color c = color_dialog.Style.Backgrounds [(int)Gtk.StateType.Active];
			hist.Color [0] = (byte) (c.Red / 0xff);
			hist.Color [1] = (byte) (c.Green / 0xff);
			hist.Color [2] = (byte) (c.Blue / 0xff);
			hist.Color [3] = 0xff;
			#endif

			histogram_image.Pixbuf = hist.GeneratePixbuf ();

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
}
