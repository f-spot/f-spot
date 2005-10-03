using System;
using Gtk;
using System.Threading;

namespace FSpot {
	public class ColorDialog : GladeDialog {
		Gdk.Pixbuf ScaledPixbuf;
		Gdk.Pixbuf AdjustedPixbuf;

#if USE_THREAD		
		Delay expose_timeout;
#endif
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

#if USE_THREAD
		Thread thread;
#endif
		
		FSpot.Histogram hist;
		
		private void Adjust ()
		{
			if (brightness_scale == null)
				return;
			
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
							    Cms.Intent.Perceptual, 0x0000);
			
			lock (AdjustedPixbuf) {
				PixbufUtils.ColorAdjust (ScaledPixbuf,
							 AdjustedPixbuf,
							 next_transform);
#if USE_THREAD
				expose_timeout.Start ();
#else
				this.QueueDraw ();
#endif
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
		
		public void HandleDestroyed (object sender, EventArgs arg)
		{
			view.PhotoChanged -= HandlePhotoChanged;
#if USE_THREAD
			expose_timeout.Stop ();
#endif
		}
		
		public void RangeChanged (object sender, EventArgs args)
		{
			if (!view.Item.IsValid)
				return;

#if USE_THREAD
			if (thread != null && thread.IsAlive)
				thread.Abort ();
			
			
			thread = new Thread (new ThreadStart (Adjust));
			thread.Start ();
#else
			Adjust ();
#endif
		}
		
		public void Save ()
		{
			if (!view.Item.IsValid)
				return;

			Console.WriteLine ("Saving....");
			Photo photo = (Photo)view.Item.Current;
			FSpot.ImageFile img = FSpot.ImageFile.Create (photo.DefaultVersionPath);

			if (img is FSpot.JpegFile) {
				FSpot.JpegFile jimg = img as JpegFile;
				
				bool created_version = false;
				uint version = photo.DefaultVersionId;
				if (version == Photo.OriginalVersionId) {
					version = photo.CreateDefaultModifiedVersion (photo.DefaultVersionId, false);
					created_version = true;
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
					PixbufUtils.SaveJpeg (final, version_path, 95, jimg.ExifData);
					ThumbnailGenerator.Create (version_path).Dispose ();
					photo.DefaultVersionId = version;
					((PhotoQuery)view.Query).Commit (view.Item.Index);
				} catch (System.Exception e) {
					string msg = Mono.Posix.Catalog.GetString ("Error saving adjusted photo");
					string desc = String.Format (Mono.Posix.Catalog.GetString ("Received exception \"{0}\". Unable to save image {1}"),
								     e.Message, photo.Name);
					
					HigMessageDialog md = new HigMessageDialog ((Gtk.Window)Dialog.Toplevel, DialogFlags.DestroyWithParent, 
										    Gtk.MessageType.Error, ButtonsType.Ok, 
										    msg,
										    desc);
					md.Run ();
					md.Destroy ();
					
					if (created_version)
						photo.DeleteVersion (version);
				}
			} else {
				string msg = Mono.Posix.Catalog.GetString ("Error saving adjusted photo");
				string desc = String.Format (Mono.Posix.Catalog.GetString ("F-Spot {0} cannot yet edit images of this type"), 
							     FSpot.Defines.VERSION);
				
				HigMessageDialog md = new HigMessageDialog ((Gtk.Window)Dialog.Toplevel, DialogFlags.DestroyWithParent, 
									    Gtk.MessageType.Error, ButtonsType.Ok, 
									    msg,
									    desc);

				md.Run ();
				md.Destroy ();
			}
			this.Dialog.Sensitive = false;
			this.Dialog.Destroy ();
		}
		
		public void Cancel ()
		{
			view.Transform = null;
			view.QueueDraw ();
			view.PhotoChanged -= HandlePhotoChanged;
			this.Dialog.Destroy ();
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
			if (view.Item.IsValid) {
				FSpot.ImageFile img = FSpot.ImageFile.Create (((Photo)view.Item.Current).DefaultVersionPath);
				AdjustedPixbuf = img.Load (150, 150);
				ScaledPixbuf = AdjustedPixbuf.Copy ();			
#if false
				Cms.Profile srgb = Cms.Profile.CreateSRgb ();
				Cms.Profile lab = Cms.Profile.CreateLab ();
				Cms.Profile [] list = new Cms.Profile [] { srgb, lab };

			        Cms.Transform t = new Cms.Transform (list, 
								     PixbufUtils.PixbufCmsFormat (AdjustedPixbuf),
								     PixbufUtils.PixbufCmsFormat (AdjustedPixbuf),
								     Cms.Intent.Perceptual, 0x0000);

				PixbufUtils.ColorAdjust (AdjustedPixbuf,
							 ScaledPixbuf,
							 t);
#endif
				RangeChanged (null, null);
			}
		}

		private void HandleResetClicked (object sender, EventArgs args)
		{
			brightness_scale.Value = 1.0;
			contrast_scale.Value = 1.0;
			hue_scale.Value = 0.0;			
			sat_scale.Value = 0.0;
			source_spinbutton.Value = 6500;
			dest_spinbutton.Value = 6500;

			brightness_spinbutton.Adjustment.ChangeValue ();
			contrast_spinbutton.Adjustment.ChangeValue ();
			hue_spinbutton.Adjustment.ChangeValue ();
			sat_spinbutton.Adjustment.ChangeValue ();
			source_spinbutton.Adjustment.ChangeValue ();
			dest_spinbutton.Adjustment.ChangeValue ();
		}
		
		private void HandleCancelClicked (object sender, EventArgs args)
		{
			Cancel ();
		}
		
		public ColorDialog (FSpot.PhotoQuery query, int item)
		{
			view = new FSpot.PhotoImageView (query);
			view_scrolled.Add (view);
			view.Show ();
			view.Item.Index = item;

			this.CreateDialog ("external_color_dialog");
			AttachInterface ();
		}

		public ColorDialog (FSpot.PhotoImageView view)       
		{
			this.view = view;
			this.CreateDialog ("inline_color_dialog");
			AttachInterface ();
		}

		private void AttachInterface ()
		{
			view.PhotoChanged += HandlePhotoChanged;
			hist = new FSpot.Histogram ();
#if USE_THREAD
			expose_timeout = new FSpot.Delay (new GLib.IdleHandler (this.QueueDraw));
#endif
			this.Dialog.Destroyed += HandleDestroyed;

#if true
			Gdk.Color c = this.Dialog.Style.Backgrounds [(int)Gtk.StateType.Active];
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

			HandlePhotoChanged (view);
		}
	}
}
