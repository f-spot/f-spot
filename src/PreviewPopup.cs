using System;

namespace FSpot {
	public class PreviewPopup : Gtk.Window {
		private IconView view;
		private Gtk.Image image;
		private Gtk.Label label;

		private ThumbnailCache preview_cache = new ThumbnailCache (50);

		private int item;
		public int Item {
			get {
				return item;
			}
			set {
				if (value != item) {
					item = value;
					UpdateImage ();
				}
				UpdatePosition ();
			}
		}

		private void UpdateImage ()
		{
			Photo photo = view.Collection.Photos [Item];
			
			string orig_path = photo.DefaultVersionPath;
			Gdk.Pixbuf pixbuf = preview_cache.GetThumbnailForPath (orig_path);
			if (pixbuf == null) {
				// A bizarre pixbuf = hack to try to deal with cinematic displays, etc.
				int preview_size = ((this.Screen.Width + this.Screen.Height)/2)/3;
				try {
					pixbuf = FSpot.PhotoLoader.LoadAtMaxSize (photo, preview_size, preview_size);                                      
				} catch (Exception e) {
					pixbuf = null;
				}

				if (pixbuf != null) {
					preview_cache.AddThumbnail (orig_path, pixbuf);
					image.Pixbuf = pixbuf;
				} else {
					image.Pixbuf = PixbufUtils.ErrorPixbuf;
				}
			} else {
				image.Pixbuf = pixbuf;
				pixbuf.Dispose ();
			}

			string desc = "";
			if (photo.Description.Length > 0)
				desc = photo.Description + "\n";

			desc += photo.Time.ToString () + "   " + photo.Name;			
			label.Text = desc;
		}

	
		private void UpdatePosition ()
		{
			int x, y;
			Gdk.Rectangle bounds = view.CellBounds (this.Item);

			Gtk.Requisition requisition = this.SizeRequest ();
			this.Resize (requisition.Width, requisition.Height);

			view.GdkWindow.GetOrigin (out x, out y);

			// Acount for scrolling
			bounds.X -= (int)view.Hadjustment.Value;
			bounds.Y -= (int)view.Vadjustment.Value;

			// calculate the cell center
			x += bounds.X + (bounds.Width / 2);
			y += bounds.Y + (bounds.Height / 2);
			
			// find the window's x location limiting it to the screen
			x = Math.Max (0, x - requisition.Width / 2);
			x = Math.Min (x, this.Screen.Width - requisition.Width);

			// find the window's y location offset above or below depending on space
#if USE_OFFSET_PREVIEW
			int margin = (int) (bounds.Height * .6);
			if (y - requisition.Height - margin < 0)
				y += margin;
			else
				y = y - requisition.Height - margin;
#else 
			y = Math.Max (0, y - requisition.Height / 2);
			y = Math.Min (y, this.Screen.Height - requisition.Height);
#endif			

			this.Move (x, y);
		}
		
		private void UpdateItem (int x, int y)
		{
			int item = view.CellAtPosition (x, y);
			if (item >= 0) {
				this.Item = item;
				UpdatePosition ();
				this.Show ();
			} else {
				this.Hide ();
			}
		}
		
	        private void UpdateItem ()
		{
			int x, y;
			view.GetPointer (out x, out y);
			x += (int) view.Hadjustment.Value;
			y += (int) view.Vadjustment.Value;
			UpdateItem (x, y);
			
		}

		private void HandleIconViewMotion (object sender, Gtk.MotionNotifyEventArgs args)
		{
			if ((args.Event.State & Gdk.ModifierType.Mod1Mask) == 0) {
				this.Hide ();
				return;
			}
			
			int x = (int) args.Event.X;
			int y = (int) args.Event.Y;
			view.GrabFocus ();
			UpdateItem (x, y);
		}

		private void HandleIconViewKeyPress (object sender, Gtk.KeyPressEventArgs args)
		{
			switch (args.Event.Key) {
			case Gdk.Key.Alt_L:
			case Gdk.Key.Alt_R:
				UpdateItem ();
				break;
			}
		}

		private void HandleIconViewKeyRelease (object sender, Gtk.KeyReleaseEventArgs args)
		{
			switch (args.Event.Key) {
			case Gdk.Key.Alt_L:
			case Gdk.Key.Alt_R:
				this.Hide ();
				break;
			}
		}
		
		private void HandleIconViewDestroy (object sender, Gtk.DestroyEventArgs args)
		{
			this.Destroy ();
		}

		protected override void OnDestroyed ()
		{
			this.preview_cache.Dispose ();
		}

		protected override bool OnMotionNotifyEvent (Gdk.EventMotion args)
		{
			//
			// We look for motion events on the popup window so that
			// if the pointer manages to get over the window we can
			// Update the image properly and/or get out of the way.
			//
			UpdateItem ();
			return false;
		}

		public PreviewPopup (IconView view) : base (Gtk.WindowType.Popup)
		{
			Gtk.VBox vbox = new Gtk.VBox ();
			this.Add (vbox);
			this.AddEvents ((int) Gdk.EventMask.PointerMotionMask);
			this.Decorated = false;
			this.SetPosition (Gtk.WindowPosition.None);

			this.view = view;
			view.MotionNotifyEvent += HandleIconViewMotion;
			view.KeyPressEvent += HandleIconViewKeyPress;
			view.KeyReleaseEvent += HandleIconViewKeyRelease;
			view.DestroyEvent += HandleIconViewDestroy;

			image = new Gtk.Image ();
			image.CanFocus = false;
			
			label = new Gtk.Label ("");
			label.CanFocus = false;
			label.ModifyFg (Gtk.StateType.Normal, new Gdk.Color (127, 127, 127));
			label.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (0, 0, 0));

			this.ModifyFg (Gtk.StateType.Normal, new Gdk.Color (127, 127, 127));
			this.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (0, 0, 0));

			vbox.PackStart (image, true, true, 0);
			vbox.PackStart (label, true, false, 0);
			vbox.ShowAll ();
		}
	}
}
