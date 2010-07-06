/*
 *
 * Author(s)
 *
 *   Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details
 *
 */

using System;
using Cairo;
using Gdk;
using FSpot.Widgets;
using FSpot.Utils;

namespace FSpot {
	public class PreviewPopup : Gtk.Window {
		private IconView view;
		private Gtk.Image image;
		private Gtk.Label label;

		private bool show_histogram;
		public bool ShowHistogram {
			get {
				return show_histogram;
			}
			set {
				if (value != show_histogram)
					item = -1;
				show_histogram = value;
			}
		}
					
		private FSpot.Histogram hist;
		private DisposableCache<string, Pixbuf> preview_cache = new DisposableCache<string, Pixbuf> (50);

		private int item = -1;
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

		private void AddHistogram (Gdk.Pixbuf pixbuf)
		{
			if (show_histogram) {
				Gdk.Pixbuf image = hist.Generate (pixbuf);
				double scalex = 0.5;
				double scaley = 0.5;
				
				int width = (int)(image.Width * scalex);
				int height = (int)(image.Height * scaley);
				
				image.Composite (pixbuf, 
						 pixbuf.Width - width - 10, pixbuf.Height - height - 10,
						 width, height, 
						 pixbuf.Width - width - 10, pixbuf.Height - height - 10,
						 scalex, scaley, 
						 Gdk.InterpType.Bilinear, 200);
			}
		}

		protected override void OnRealized ()
		{
			bool composited = CompositeUtils.IsComposited (Screen) && CompositeUtils.SetRgbaColormap (this);
			AppPaintable = composited;
			base.OnRealized ();
		}


		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			int round = 12;
			Context g = Gdk.CairoHelper.Create (GdkWindow);
			g.Operator = Operator.Source;
			g.Source = new SolidPattern (new Cairo.Color (0, 0, 0, 0));
			g.Paint ();
			g.Operator = Operator.Over;
#if true
			g.Source = new SolidPattern (new Cairo.Color (0, 0, 0, .7));
			g.MoveTo (round, 0);
			//g.LineTo (Allocation.Width - round, 0);
			g.Arc (Allocation.Width - round, round, round, - Math.PI * 0.5, 0);
			//g.LineTo (Allocation.Width, Allocation.Height - round);
			g.Arc (Allocation.Width - round, Allocation.Height - round, round, 0, Math.PI * 0.5);
			//g.LineTo (round, Allocation.Height);
			g.Arc (round, Allocation.Height - round, round, Math.PI * 0.5, Math.PI);
			g.Arc (round, round, round, Math.PI, Math.PI * 1.5);
			g.ClosePath ();
			g.Fill ();
#endif
			((IDisposable)g).Dispose ();
			return base.OnExposeEvent (args);
		}

		private void UpdateImage ()
		{
			FSpot.IBrowsableItem item = view.Collection [Item];
			
			string orig_path = item.DefaultVersion.Uri.LocalPath;

			Gdk.Pixbuf pixbuf = FSpot.Utils.PixbufUtils.ShallowCopy (preview_cache.Get (orig_path + show_histogram.ToString ()));
			if (pixbuf == null) {
				// A bizarre pixbuf = hack to try to deal with cinematic displays, etc.
				int preview_size = ((this.Screen.Width + this.Screen.Height)/2)/3;
				try {
					pixbuf = FSpot.PhotoLoader.LoadAtMaxSize (item, preview_size, preview_size);
				} catch (Exception) {
					pixbuf = null;
				}

				if (pixbuf != null) {
					preview_cache.Add (orig_path + show_histogram.ToString (), pixbuf);
					AddHistogram (pixbuf);
					image.Pixbuf = pixbuf;
				} else {
					image.Pixbuf = PixbufUtils.ErrorPixbuf;
				}
			} else {
				image.Pixbuf = pixbuf;
				pixbuf.Dispose ();
			}

			string desc = String.Empty;
			if (item.Description != null && item.Description.Length > 0)
				desc = item.Description + Environment.NewLine;

			desc += item.Time.ToString () + "   " + item.Name;			
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
				Show ();
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
			if (!this.Visible)
				return;

			int x = (int) args.Event.X;
			int y = (int) args.Event.Y;
			view.GrabFocus ();
			UpdateItem (x, y);
		}

		private void HandleIconViewKeyPress (object sender, Gtk.KeyPressEventArgs args)
		{
			switch (args.Event.Key) {
			case Gdk.Key.v:
				ShowHistogram = false;
				UpdateItem ();
				args.RetVal = true;
				break;
			case Gdk.Key.V:
				ShowHistogram = true;
				UpdateItem ();
				args.RetVal = true;
				break;
			}
		}

		private void HandleKeyRelease (object sender, Gtk.KeyReleaseEventArgs args)
		{
			switch (args.Event.Key) {
			case Gdk.Key.v:
			case Gdk.Key.V:
			case Gdk.Key.h:
				this.Hide ();
				break;
			}
		}
		
		private void HandleButtonPress (object sender, Gtk.ButtonPressEventArgs args)
		{
			this.Hide ();
		}

		private void HandleIconViewDestroy (object sender, Gtk.DestroyEventArgs args)
		{
			this.Destroy ();
		}

		private void HandleDestroyed (object sender, System.EventArgs args)
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

		public PreviewPopup (IconView view) : base (Gtk.WindowType.Toplevel)
		{	
			Gtk.VBox vbox = new Gtk.VBox ();
			this.Add (vbox);
			this.AddEvents ((int) (Gdk.EventMask.PointerMotionMask | 
					       Gdk.EventMask.KeyReleaseMask | 
					       Gdk.EventMask.ButtonPressMask));

			this.Decorated = false;
			this.SkipTaskbarHint = true;
			this.SkipPagerHint = true;
			this.SetPosition (Gtk.WindowPosition.None);
			
			this.KeyReleaseEvent += HandleKeyRelease;
			this.ButtonPressEvent += HandleButtonPress;
			this.Destroyed += HandleDestroyed;

			this.view = view;
			view.MotionNotifyEvent += HandleIconViewMotion;
			view.KeyPressEvent += HandleIconViewKeyPress;
			view.KeyReleaseEvent += HandleKeyRelease;
			view.DestroyEvent += HandleIconViewDestroy;

			this.BorderWidth = 6;

			hist = new FSpot.Histogram ();
			hist.RedColorHint = 127;
			hist.GreenColorHint = 127;
			hist.BlueColorHint = 127;
			hist.BackgroundColorHint = 0xff;

			image = new Gtk.Image ();
			image.CanFocus = false;


			label = new Gtk.Label (String.Empty);
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
