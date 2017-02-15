//
// PreviewPopup.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2004-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Ruben Vermeersch
// Copyright (C) 2004-2006 Larry Ewing
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Cairo;
using Gdk;

using FSpot.Core;
using FSpot.Utils;
using FSpot.Gui;

namespace FSpot.Widgets
{
	public class PreviewPopup : Gtk.Window
	{
		readonly CollectionGridView view;
		readonly Gtk.Image image;
		readonly Gtk.Label label;

		bool show_histogram;
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

		readonly Histogram hist;
		DisposableCache<string, Pixbuf> preview_cache = new DisposableCache<string, Pixbuf> (50);

		int item = -1;
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

		void AddHistogram (Pixbuf pixbuf)
		{
			if (show_histogram) {
				Pixbuf image = hist.Generate (pixbuf);
				double scalex = 0.5;
				double scaley = 0.5;

				int width = (int)(image.Width * scalex);
				int height = (int)(image.Height * scaley);

				image.Composite (pixbuf,
						 pixbuf.Width - width - 10, pixbuf.Height - height - 10,
						 width, height,
						 pixbuf.Width - width - 10, pixbuf.Height - height - 10,
						 scalex, scaley,
						 InterpType.Bilinear, 200);
			}
		}

		protected override void OnRealized ()
		{
			bool composited = CompositeUtils.IsComposited (Screen) && CompositeUtils.SetRgbaColormap (this);
			AppPaintable = composited;
			base.OnRealized ();
		}


		protected override bool OnExposeEvent (EventExpose args)
		{
			int round = 12;
			Context g = CairoHelper.Create (GdkWindow);
			g.Operator = Operator.Source;
			g.SetSource (new SolidPattern (new Cairo.Color (0, 0, 0, 0)));
			g.Paint ();
			g.Operator = Operator.Over;
			g.SetSource (new SolidPattern (new Cairo.Color (0, 0, 0, .7)));
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
			g.Dispose ();
			return base.OnExposeEvent (args);
		}

		void UpdateImage ()
		{
			IPhoto item = view.Collection [Item];

			string orig_path = item.DefaultVersion.Uri.LocalPath;

			Pixbuf pixbuf = FSpot.Utils.PixbufUtils.ShallowCopy (preview_cache.Get (orig_path + show_histogram));
			if (pixbuf == null) {
				// A bizarre pixbuf = hack to try to deal with cinematic displays, etc.
				int preview_size = ((Screen.Width + Screen.Height)/2)/3;
				try {
					pixbuf = PhotoLoader.LoadAtMaxSize (item, preview_size, preview_size);
				} catch (Exception) {
					pixbuf = null;
				}

				if (pixbuf != null) {
					preview_cache.Add (orig_path + show_histogram, pixbuf);
					AddHistogram (pixbuf);
					image.Pixbuf = pixbuf;
				} else {
					image.Pixbuf = PixbufUtils.ErrorPixbuf;
				}
			} else {
				image.Pixbuf = pixbuf;
				pixbuf.Dispose ();
			}

			string desc = string.Empty;
			if (!string.IsNullOrEmpty (item.Description))
				desc = item.Description + Environment.NewLine;

			desc += item.Time + "   " + item.Name;
			label.Text = desc;
		}


		void UpdatePosition ()
		{
			int x, y;
			Gdk.Rectangle bounds = view.CellBounds (Item);

			Gtk.Requisition requisition = SizeRequest ();
			Resize (requisition.Width, requisition.Height);

			view.GdkWindow.GetOrigin (out x, out y);

			// Acount for scrolling
			bounds.X -= (int)view.Hadjustment.Value;
			bounds.Y -= (int)view.Vadjustment.Value;

			// calculate the cell center
			x += bounds.X + (bounds.Width / 2);
			y += bounds.Y + (bounds.Height / 2);

			// find the window's x location limiting it to the screen
			x = Math.Max (0, x - requisition.Width / 2);
			x = Math.Min (x, Screen.Width - requisition.Width);

			// find the window's y location offset above or below depending on space
			y = Math.Max (0, y - requisition.Height / 2);
			y = Math.Min (y, Screen.Height - requisition.Height);

			Move (x, y);
		}

		void UpdateItem (int x, int y)
		{
			int itemAtPosition = view.CellAtPosition (x, y);
			if (itemAtPosition >= 0) {
				Item = itemAtPosition;
				Show ();
			} else {
				Hide ();
			}
		}

	        void UpdateItem ()
		{
			int x, y;
			view.GetPointer (out x, out y);
			x += (int) view.Hadjustment.Value;
			y += (int) view.Vadjustment.Value;
			UpdateItem (x, y);

		}

		void HandleIconViewMotion (object sender, Gtk.MotionNotifyEventArgs args)
		{
			if (!Visible)
				return;

			int x = (int) args.Event.X;
			int y = (int) args.Event.Y;
			view.GrabFocus ();
			UpdateItem (x, y);
		}

		void HandleIconViewKeyPress (object sender, Gtk.KeyPressEventArgs args)
		{
			switch (args.Event.Key) {
			case Key.v:
				ShowHistogram = false;
				UpdateItem ();
				args.RetVal = true;
				break;
			case Key.V:
				ShowHistogram = true;
				UpdateItem ();
				args.RetVal = true;
				break;
			}
		}

		void HandleKeyRelease (object sender, Gtk.KeyReleaseEventArgs args)
		{
			switch (args.Event.Key) {
			case Key.v:
			case Key.V:
			case Key.h:
				Hide ();
				break;
			}
		}

		void HandleButtonPress (object sender, Gtk.ButtonPressEventArgs args)
		{
			Hide ();
		}

		void HandleIconViewDestroy (object sender, Gtk.DestroyEventArgs args)
		{
			Destroy ();
		}

		void HandleDestroyed (object sender, EventArgs args)
		{
		}

		protected override bool OnMotionNotifyEvent (EventMotion args)
		{
			// We look for motion events on the popup window so that
			// if the pointer manages to get over the window we can
			// Update the image properly and/or get out of the way.
			UpdateItem ();
			return false;
		}

		public PreviewPopup (SelectionCollectionGridView view) : base (Gtk.WindowType.Toplevel)
		{
			var vbox = new Gtk.VBox ();
			Add (vbox);
			AddEvents ((int) (EventMask.PointerMotionMask |
					       EventMask.KeyReleaseMask |
					       EventMask.ButtonPressMask));

			Decorated = false;
			SkipTaskbarHint = true;
			SkipPagerHint = true;
			SetPosition (Gtk.WindowPosition.None);

			KeyReleaseEvent += HandleKeyRelease;
			ButtonPressEvent += HandleButtonPress;
			Destroyed += HandleDestroyed;

			this.view = view;
			view.MotionNotifyEvent += HandleIconViewMotion;
			view.KeyPressEvent += HandleIconViewKeyPress;
			view.KeyReleaseEvent += HandleKeyRelease;
			view.DestroyEvent += HandleIconViewDestroy;

			BorderWidth = 6;

			hist = new Histogram ();
			hist.RedColorHint = 127;
			hist.GreenColorHint = 127;
			hist.BlueColorHint = 127;
			hist.BackgroundColorHint = 0xff;

			image = new Gtk.Image ();
			image.CanFocus = false;


			label = new Gtk.Label (string.Empty);
			label.CanFocus = false;
			label.ModifyFg (Gtk.StateType.Normal, new Gdk.Color (127, 127, 127));
			label.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (0, 0, 0));

			ModifyFg (Gtk.StateType.Normal, new Gdk.Color (127, 127, 127));
			ModifyBg (Gtk.StateType.Normal, new Gdk.Color (0, 0, 0));

			vbox.PackStart (image, true, true, 0);
			vbox.PackStart (label, true, false, 0);
			vbox.ShowAll ();
		}

		public override void Dispose()
		{
			Dispose(true);
			base.Dispose (); // SuppressFinalize is called by base class
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing) {
				// free managed resources
				if (preview_cache != null) {
					preview_cache.Dispose ();
					preview_cache = null;
				}
			}
			// free unmanaged resources
		}
	}
}
