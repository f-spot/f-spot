using Gtk;
using System;

namespace FSpot {
	public class Sharpener : Loupe {
		Gtk.SpinButton amount_spin = new Gtk.SpinButton (0.5, 100.0, .01);
		Gtk.SpinButton radius_spin = new Gtk.SpinButton (5.0, 50.0, .01);
		Gtk.SpinButton threshold_spin = new Gtk.SpinButton (0.0, 50.0, .01);
		
		public Sharpener (PhotoImageView view) : base (view)
		{	
		}

		protected override void UpdateSample ()
		{
			if (view.Pixbuf == null) {
				System.Console.WriteLine ("no image");
				image.Pixbuf = null;
				return;
			}
			
			Gdk.Pixbuf old = image.Pixbuf;
			Gdk.Pixbuf sample = new Gdk.Pixbuf (view.Pixbuf, region.X, region.Y, region.Width, region.Height);
			if (sample != null) {
				image.Pixbuf = PixbufUtils.UnsharpMask (sample, radius_spin.Value, amount_spin.Value, threshold_spin.Value);
				sample.Dispose ();
			} else 
				image.Pixbuf = null;

			if (old != null)
				old.Dispose ();
		}

		private void HandleSettingsChanged (object sender, EventArgs args)
		{
			UpdateSample ();
		}
		
		private void HandleOkClicked ()
		{
			Photo photo = view.Item.Current as Photo;

			if (photo == null)
				return;
			
			Gdk.Pixbuf orig = view.Pixbuf;
			Gdk.Pixbuf final = PixbufUtils.UnsharpMask (orig, radius_spin.Value, amount_spin.Value, threshold_spin.Value);
			
			bool create_version = photo.DefaultVersionId == Photo.OriginalVersionId;
			
			try {
				photo.SaveVersion (final, create_version);
			} catch (System.Exception e) {
				string msg = Mono.Posix.Catalog.GetString ("Error saving sharpened photo");
				string desc = String.Format (Mono.Posix.Catalog.GetString ("Received exception \"{0}\". Unable to save image {1}"),
							     e.Message, photo.Name);
				
				HigMessageDialog md = new HigMessageDialog (this, DialogFlags.DestroyWithParent, 
									    Gtk.MessageType.Error, ButtonsType.Ok, 
									    msg,
									    desc);
				md.Run ();
				md.Destroy ();
			}

		}

		protected override void BuildUI ()
		{
			base.BuildUI ();

			this.BorderWidth = 12;
			box.Spacing = 6;

			this.Title = Mono.Posix.Catalog.GetString ("Sharpen");
			Gtk.Table table = new Gtk.Table (3, 2, false);
			table.ColumnSpacing = 6;
			table.RowSpacing = 6;
			
			table.Attach (SetFancyStyle (new Gtk.Label (Mono.Posix.Catalog.GetString ("Amount:"))), 0, 1, 0, 1);
			table.Attach (SetFancyStyle (new Gtk.Label (Mono.Posix.Catalog.GetString ("Radius:"))), 0, 1, 1, 2);
			table.Attach (SetFancyStyle (new Gtk.Label (Mono.Posix.Catalog.GetString ("Threshold:"))), 0, 1, 2, 3);
			
			SetFancyStyle (amount_spin = new Gtk.SpinButton (0.5, 100.0, .01));
			SetFancyStyle (radius_spin = new Gtk.SpinButton (5.0, 50.0, .01));
			SetFancyStyle (threshold_spin = new Gtk.SpinButton (0.0, 50.0, .01));
			
			amount_spin.ValueChanged += HandleSettingsChanged;
			radius_spin.ValueChanged += HandleSettingsChanged;
			threshold_spin.ValueChanged += HandleSettingsChanged;

			table.Attach (amount_spin, 1, 2, 0, 1);
			table.Attach (radius_spin, 1, 2, 1, 2);
			table.Attach (threshold_spin, 1, 2, 2, 3);
			
			table.ShowAll ();
		        box.PackStart (table);
		}

	}

	public class Loupe : Gtk.Window {
		protected PhotoImageView view;
		protected Image image;
		protected VBox box; 
		protected Gdk.Rectangle region;

		public Loupe (PhotoImageView view) : base ("my window")
		{ 
			this.view = view;
			BuildUI ();
		}

		public void SetSamplePoint (Gdk.Point p)
		{
			region.X = p.X;
			region.Y = p.Y;
			region.Width = 256;
			region.Height = 256;
			region.Offset (- Math.Min (region.X, 128), - Math.Min (region.Y, 128));

			UpdateSample ();
		}

		protected virtual void UpdateSample ()
		{
			if (view.Pixbuf == null) {
				System.Console.WriteLine ("no image");
				image.Pixbuf = null;
				return;
			}
			
			Gdk.Pixbuf old = image.Pixbuf;
			image.FromPixbuf = new Gdk.Pixbuf (view.Pixbuf, region.X, region.Y, region.Width, region.Height);

			if (old != null)
				old.Dispose ();
		}

		[GLib.ConnectBefore]
		private void HandleImageViewMotion (object sender, MotionNotifyEventArgs args)
		{
			Gdk.Point coords;
			coords = new Gdk.Point ((int) args.Event.X, (int) args.Event.Y);
			
			SetSamplePoint (view.WindowCoordsToImage (coords));
			//QueueDraw ();
		}
		
		private void HandleDestroyed (object sender, System.EventArgs args)
		{
			view.MotionNotifyEvent -= HandleImageViewMotion;
		}

		protected Widget SetFancyStyle (Widget widget)
		{
			widget.ModifyFg (Gtk.StateType.Normal, new Gdk.Color (127, 127, 127));
			widget.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (0, 0, 0));
			return widget;
		}
		
		protected virtual void BuildUI ()
		{
			box = new VBox ();
			this.Add (box);
			image = new Image ();
			box.PackStart (image);
			box.ShowAll ();

			SetFancyStyle (this);
			
			TransientFor = (Gtk.Window) view.Toplevel;
			view.MotionNotifyEvent += HandleImageViewMotion;

			SetSamplePoint (Gdk.Point.Zero);
			box.ShowAll ();
		}
	}
}

