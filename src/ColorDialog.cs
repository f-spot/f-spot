using System;
using Gtk;
using Cms;
using Mono.Unix;
using System.Threading;
using FSpot.Widgets;

namespace FSpot {
	public class ColorDialog : GladeDialog {
		protected static ColorDialog instance = null;
		
		Gdk.Pixbuf ScaledPixbuf;
		Gdk.Pixbuf AdjustedPixbuf;
		
#if USE_THREAD		
		Delay expose_timeout;
#endif
		[Glade.Widget] private Gtk.HScale exposure_scale;
		[Glade.Widget] private Gtk.HScale temp_scale;
		[Glade.Widget] private Gtk.HScale temptint_scale;
		[Glade.Widget] private Gtk.HScale brightness_scale;
		[Glade.Widget] private Gtk.HScale contrast_scale;
		[Glade.Widget] private Gtk.HScale hue_scale;
		[Glade.Widget] private Gtk.HScale sat_scale;
		
		[Glade.Widget] private Gtk.SpinButton exposure_spinbutton;
		[Glade.Widget] private Gtk.SpinButton temp_spinbutton;
		[Glade.Widget] private Gtk.SpinButton temptint_spinbutton;
		[Glade.Widget] private Gtk.SpinButton brightness_spinbutton;
		[Glade.Widget] private Gtk.SpinButton contrast_spinbutton;
		[Glade.Widget] private Gtk.SpinButton hue_spinbutton;
		[Glade.Widget] private Gtk.SpinButton sat_spinbutton;
		
		[Glade.Widget] private Gtk.ScrolledWindow view_scrolled;
		[Glade.Widget] private Gtk.Image histogram_image;
		
		[Glade.Widget] private Gtk.CheckButton white_check;
		[Glade.Widget] private Gtk.CheckButton exposure_check;
		
		[Glade.Widget] Gtk.Button ok_button;
		[Glade.Widget] Gtk.VBox   control_vbox;
		
		private FSpot.PhotoImageView view;

		Cms.Transform next_transform;
		Cms.Profile image_profile = Cms.Profile.CreateStandardRgb ();
		Cms.Profile adjustment_profile;

#if USE_THREAD
		Thread thread;
#endif
		
		FSpot.Histogram hist;
		

		private void Adjust ()
		{

			if (brightness_scale == null)
				return;
			
			if (AdjustedPixbuf == null)
				return;

			Cms.Profile display_profile = Cms.Profile.GetScreenProfile (view.Screen);
			Cms.Profile [] list;
			
			if (display_profile == null)
				display_profile = Cms.Profile.CreateStandardRgb ();
			
			if (!Changed || AdjustedPixbuf.HasAlpha) {
				if (AdjustedPixbuf.HasAlpha)
					System.Console.WriteLine ("Cannot currently adjust images with an alpha channel");

				list = new Cms.Profile [] { image_profile, display_profile };

				next_transform = new Cms.Transform (list, 
								    PixbufUtils.PixbufCmsFormat (AdjustedPixbuf),
								    PixbufUtils.PixbufCmsFormat (AdjustedPixbuf),
								    Cms.Intent.Perceptual, 0x0000);
			} else {
					
				using (adjustment_profile = AdjustmentProfile ()) {
					list = new Cms.Profile [] { image_profile, adjustment_profile, display_profile };
					
					next_transform = new Cms.Transform (list, 
									    PixbufUtils.PixbufCmsFormat (AdjustedPixbuf),
									    PixbufUtils.PixbufCmsFormat (AdjustedPixbuf),
									    Cms.Intent.Perceptual, 0x0000);
				}
			}
			
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

		public static ColorDialog Instance {
			get { return instance; }
		}
		
		public bool UseWhiteSettings {
			get {
				if (white_check != null)
					return white_check.Active;
				else
					return true;
			}
		}

		public bool UseExposureSettings {
			get {
				if (exposure_check != null)
					return exposure_check.Active;
				else 
					return true;
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
			
			Cancel ();
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
			if (!Changed) {
				this.Dialog.Destroy ();
				return;
			}

			if (!view.Item.IsValid)
				return;

			Console.WriteLine ("Saving....");

			Photo photo = (Photo)view.Item.Current;
			try {
				bool create_version = photo.DefaultVersionId == Photo.OriginalVersionId;
				
				Gdk.Pixbuf orig = view.CompletePixbuf ();
				Gdk.Pixbuf final = new Gdk.Pixbuf (Gdk.Colorspace.Rgb,
								   false, 8,
								   orig.Width, 
								   orig.Height);
				
				Cms.Profile abs = AdjustmentProfile ();
				
				// FIXME this shouldn't use the screen as the destination profile.
				Cms.Profile destination = Cms.Profile.GetScreenProfile (view.Screen);
				if (destination == null)
					destination = Cms.Profile.CreateStandardRgb ();
				
				Cms.Profile [] list = new Cms.Profile [] { image_profile, abs, destination };
				Cms.Transform transform = new Cms.Transform (list,
									     PixbufUtils.PixbufCmsFormat (orig),
									     PixbufUtils.PixbufCmsFormat (final),
									     Cms.Intent.Perceptual, 0x0000);
				
				PixbufUtils.ColorAdjust (orig,
							 final,
							 transform);
				
				photo.SaveVersion (final, create_version);
				((PhotoQuery)view.Query).Commit (view.Item.Index);
				final.Dispose ();
			} catch (System.Exception e) {
				string msg = Catalog.GetString ("Error saving adjusted photo");
				string desc = String.Format (Catalog.GetString ("Received exception \"{0}\". Unable to save photo {1}"),
							     e.Message, photo.Name);
				
				HigMessageDialog md = new HigMessageDialog ((Gtk.Window)Dialog.Toplevel,
									    DialogFlags.DestroyWithParent, 
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
			this.Dialog.Destroy ();
			view.Transform = null;
			view.PhotoChanged -= HandlePhotoChanged;
			view.QueueDraw ();
			System.Console.WriteLine ("clearing window");
			instance = null;
		}
		
		private void HandleOkClicked (object sender, EventArgs args)
		{
			Save ();
			view.Transform = null;
			view.QueueDraw ();

			view.PhotoChanged -= HandlePhotoChanged;
		}

		private void HandleProfileSelected (object sender, EventArgs args)
		{
		       
			
		}

		private void HandlePhotoChanged (PhotoImageView view)
		{
			try {
				if (!view.Item.IsValid)
 					throw new Exception ("Invalid Image");
				
				using (FSpot.ImageFile img = FSpot.ImageFile.Create (((Photo)view.Item.Current).DefaultVersionUri)) {
 					try {
 						image_profile = img.GetProfile ();
 					} catch (System.Exception e) {
 						image_profile = null;
 						System.Console.WriteLine (e);
 					}

					// FIXME fall back to rgb for now
					if (image_profile == null)
						image_profile = Cms.Profile.CreateStandardRgb ();
				
					AdjustedPixbuf = img.Load (256, 256);
					ScaledPixbuf = AdjustedPixbuf.Copy ();			
				}

				if (AdjustedPixbuf.HasAlpha)
					throw new Exception ("Unsupported Alpha Channel");

 				control_vbox.Sensitive = true;
 				ok_button.Sensitive = true;

  				RangeChanged (null, null);
  			} catch (System.Exception) {
 				control_vbox.Sensitive = false;
 				ok_button.Sensitive = false;
 				AdjustedPixbuf = null;
 				ScaledPixbuf = null;
  				image_profile = null;
  			}	
		}

		private const double e = 0.0;
		private const double b = 0.0;
		private const double c = 0.0;
		private const double h = 0.0;
		private const double s = 0.0;
		private const double t = 5000;
		private const double tt = 0.0;

		private Cms.Profile AdjustmentProfile ()
		{
			Cms.Profile profile;
			Cms.ColorCIEXYZ src_wp;
			Cms.ColorCIEXYZ dest_wp;

			double exposure = e;
			double brightness = b;
			double contrast = c;
			double hue = h;
			double saturation = s;
			
			if (UseWhiteSettings) {
				//src_wp = image_profile.MediaWhitePoint;
				src_wp = Cms.ColorCIExyY.WhitePointFromTemperature ((int)t).ToXYZ ();
				dest_wp = Cms.ColorCIExyY.WhitePointFromTemperature ((int)temp_scale.Value).ToXYZ ();
				Cms.ColorCIELab dest_lab = dest_wp.ToLab (src_wp);
				dest_lab.a += temptint_scale.Value;
				//System.Console.WriteLine ("after {0}", dest_lab);
				dest_wp = dest_lab.ToXYZ (src_wp);
			} else {
				src_wp = Cms.ColorCIExyY.WhitePointFromTemperature ((int)t).ToXYZ ();
				dest_wp = src_wp;
			}

			if (UseExposureSettings) {
				exposure = exposure_scale.Value;
				brightness = brightness_scale.Value;
				contrast = contrast_scale.Value;
				hue = hue_scale.Value;
				saturation = sat_scale.Value;
			}

			profile = Cms.Profile.CreateAbstract (20, 
							      Math.Pow (Math.Sqrt (2.0), exposure),
							      brightness,
							      contrast,
							      hue,
							      saturation,
							      null,
							      src_wp.ToxyY (),
							      dest_wp.ToxyY ());

			return profile;
		}

		private bool Changed {
			get {
				bool changed = false;
				changed |= (exposure_scale.Value != e);
			        changed |= (brightness_scale.Value != b);
			        changed |= (contrast_scale.Value != c);
			        changed |= (hue_scale.Value != h);
			        changed |= (sat_scale.Value != s);
				changed |= (temp_scale.Value != t);
				changed |= (temptint_scale.Value != tt);

				return changed;
			}
		}
		
		private void ResetWhiteBalance ()
		{
			temp_scale.Adjustment.Value = t;
			temptint_scale.Adjustment.Value = tt;
		}

		private void ResetCorrections ()
		{
			exposure_scale.Adjustment.Value = e;
			brightness_scale.Adjustment.Value = b;
			contrast_scale.Adjustment.Value = c;
			hue_scale.Adjustment.Value = h;			
			sat_scale.Adjustment.Value = s;
		}

		private void HandleResetClicked (object sender, EventArgs args)
		{
			ResetCorrections ();
			ResetWhiteBalance ();
			//brightness_scale.Adjustment.ChangeValue ();
		}
		
		private void HandleWPResetClicked (object sender, EventArgs args)
		{
			ResetWhiteBalance ();
			//brightness_scale.Adjustment.ChangeValue ();
		}

		private void HandleExposureResetClicked (object sender, EventArgs args)
		{
			ResetCorrections ();
			//brightness_scale.Adjustment.ChangeValue ();
		}

		private void HandleCancelClicked (object sender, EventArgs args)
		{
			Cancel ();
		}
		
		public static void Close ()
		{
			if (instance != null) {
				instance.Cancel ();
			}
		}
		
		public static void CreateForView (FSpot.PhotoImageView view)
		{
			Close ();
			
			instance = new ColorDialog (view);
		}
		
		// FIXME is this the riht place for this, shouldn't mainwindow
		// treat fullscreen as a mode and handle this part itself?
		public static void SwitchViews (FSpot.PhotoImageView view) {
			if (instance != null) {
				if (instance.view.Item.Current == view.Item.Current) {
					instance.view.Transform = null;
					instance.SetView (view);
					instance.Adjust ();
				} else {
					CreateForView (view);
				}
			}
		}
		
		protected ColorDialog (FSpot.PhotoQuery query, int item)
		{
			view = new FSpot.PhotoImageView (query);
			view_scrolled.Add (view);
			view.Show ();
			view.Item.Index = item;
			
			this.CreateDialog ("external_color_dialog");
			AttachInterface ();
		}
		
		protected ColorDialog (FSpot.PhotoImageView view)       
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

			exposure_spinbutton.Adjustment = exposure_scale.Adjustment;
			temp_spinbutton.Adjustment = temp_scale.Adjustment;
			temptint_spinbutton.Adjustment = temptint_scale.Adjustment;
			brightness_spinbutton.Adjustment = brightness_scale.Adjustment;
			contrast_spinbutton.Adjustment = contrast_scale.Adjustment;
			hue_spinbutton.Adjustment = hue_scale.Adjustment;
			sat_spinbutton.Adjustment = sat_scale.Adjustment;
			
			temp_spinbutton.Adjustment.ChangeValue ();
			temptint_spinbutton.Adjustment.ChangeValue ();
			brightness_spinbutton.Adjustment.ChangeValue ();
			contrast_spinbutton.Adjustment.ChangeValue ();
			hue_spinbutton.Adjustment.ChangeValue ();
			sat_spinbutton.Adjustment.ChangeValue ();
			hue_spinbutton.Adjustment.ChangeValue ();
			sat_spinbutton.Adjustment.ChangeValue ();
			
			exposure_scale.ValueChanged += RangeChanged;
			temp_scale.ValueChanged += RangeChanged;
			temptint_scale.ValueChanged += RangeChanged;
			brightness_scale.ValueChanged += RangeChanged;
			contrast_scale.ValueChanged += RangeChanged;
			hue_scale.ValueChanged += RangeChanged;
			sat_scale.ValueChanged += RangeChanged;

			HandlePhotoChanged (view);
		}

		private void SetView (FSpot.PhotoImageView view)
		{
			this.view.PhotoChanged -= HandlePhotoChanged;
			this.view = view;
			this.view.PhotoChanged += HandlePhotoChanged;

			
			if (view.Toplevel is FullScreenView && Dialog.IsRealized)
				CompositeUtils.SetWinOpacity (Dialog, 0.5);
			else 
				CompositeUtils.SetWinOpacity (Dialog, 1.0);

		}
	}
}
