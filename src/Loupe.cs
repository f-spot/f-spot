using Gtk;
using System;

namespace FSpot {
	public class Loupe : Gtk.Window {
		PhotoImageView view;
		Image image;
		VBox box; 
		Gdk.Rectangle region;

		public void SetSamplePoint (Gdk.Point p)
		{
			region.X = p.X;
			region.Y = p.Y;
			region.Width = 256;
			region.Height = 256;

			UpdateSample ();
		}

		public void UpdateSample ()
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
			QueueDraw ();
		}
		
		private void HandleButtonPressEvent (object obj, ButtonPressEventArgs args)
		{
			Destroy ();
		}

		private void HandleDestroyed (object sender, System.EventArgs args)
		{
			view.MotionNotifyEvent -= HandleImageViewMotion;
		}

		public Loupe (PhotoImageView view) : base ("my window")
		{ 
			this.view = view;
			box = new VBox ();
			this.Add (box);
			image = new Image ();
			box.PackStart (image);
			box.ShowAll ();

			this.ModifyFg (Gtk.StateType.Normal, new Gdk.Color (127, 127, 127));
			this.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (0, 0, 0));
			
			view.MotionNotifyEvent += HandleImageViewMotion;
			ButtonPressEvent += HandleButtonPressEvent;

			ShowAll ();
			SetSamplePoint (Gdk.Point.Zero);
		}
	}
}

