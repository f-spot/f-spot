//
// Sharpener.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2009-2010 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
// Copyright (C) 2010 Ruben Vermeersch
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

using Gtk;

using Mono.Unix;

using FSpot.UI.Dialog;

using Hyena.Widgets;

namespace FSpot.Widgets
{
	public class Sharpener : Loupe
	{
		Gtk.SpinButton amount_spin = new Gtk.SpinButton (0.5, 100.0, .01);
		Gtk.SpinButton radius_spin = new Gtk.SpinButton (5.0, 50.0, .01);
		Gtk.SpinButton threshold_spin = new Gtk.SpinButton (0.0, 50.0, .01);
		Gtk.Dialog dialog;
		ThreadProgressDialog progressDialog;
		bool okClicked;

		public Sharpener (PhotoImageView view) : base (view)
		{
		}

		protected override void UpdateSample ()
		{
			if (!okClicked) {
				base.UpdateSample ();
    
				if (overlay != null)
					overlay.Dispose ();
    
				overlay = null;
				if (source != null)
					overlay = PixbufUtils.UnsharpMask (source,
    								   radius_spin.Value,
    								   amount_spin.Value,
    								   threshold_spin.Value,
                                       null);
			}
		}

		void HandleSettingsChanged (object sender, EventArgs args)
		{
			UpdateSample ();
		}

		public void doSharpening ()
		{
			progressDialog.Fraction = 0.0;
			// FIXME: This should probably be translated
			progressDialog.Message = "Photo is being sharpened";

			okClicked = true;
			Photo photo = view.Item.Current as Photo;

			if (photo == null)
				return;

			try {
				Gdk.Pixbuf orig = view.Pixbuf;
				Gdk.Pixbuf final = PixbufUtils.UnsharpMask (orig,
                                        radius_spin.Value,
                                        amount_spin.Value,
                                        threshold_spin.Value,
                                        progressDialog);

				bool create_version = photo.DefaultVersion.IsProtected;

				photo.SaveVersion (final, create_version);
				photo.Changes.DataChanged = true;
				App.Instance.Database.Photos.Commit (photo);
			} catch (Exception e) {
				string msg = Catalog.GetString ("Error saving sharpened photo");
				string desc = string.Format (Catalog.GetString ("Received exception \"{0}\". Unable to save photo {1}"),
                                 e.Message, photo.Name);

				HigMessageDialog md = new HigMessageDialog (this, DialogFlags.DestroyWithParent,
                                        Gtk.MessageType.Error,
                                        ButtonsType.Ok,
                                        msg,
                                        desc);
				md.Run ();
				md.Destroy ();
			}

			progressDialog.Fraction = 1.0;
			// FIXME: This should probably be translated
			progressDialog.Message = "Sharpening complete!";
			progressDialog.ButtonLabel = Gtk.Stock.Ok;

			Destroy ();
		}

		void HandleOkClicked (object sender, EventArgs args)
		{
			Hide ();
			dialog.Hide ();

			System.Threading.Thread command_thread = new System.Threading.Thread (new System.Threading.ThreadStart (doSharpening));
			command_thread.Name = "Sharpening";

			progressDialog = new ThreadProgressDialog (command_thread, 1);
			progressDialog.Start ();
		}

		public void HandleCancelClicked (object sender, EventArgs args)
		{
			Destroy ();
		}

		public void HandleLoupeDestroyed (object sender, EventArgs args)
		{
			dialog.Destroy ();
		}

		protected override void BuildUI ()
		{
			base.BuildUI ();

			string title = Catalog.GetString ("Sharpen");
			dialog = new Gtk.Dialog (title, (Gtk.Window)this,
						 DialogFlags.DestroyWithParent, new object [0]);
			dialog.BorderWidth = 12;
			dialog.VBox.Spacing = 6;

			Gtk.Table table = new Gtk.Table (3, 2, false);
			table.ColumnSpacing = 6;
			table.RowSpacing = 6;

			table.Attach (SetFancyStyle (new Gtk.Label (Catalog.GetString ("Amount:"))), 0, 1, 0, 1);
			table.Attach (SetFancyStyle (new Gtk.Label (Catalog.GetString ("Radius:"))), 0, 1, 1, 2);
			table.Attach (SetFancyStyle (new Gtk.Label (Catalog.GetString ("Threshold:"))), 0, 1, 2, 3);

			SetFancyStyle (amount_spin = new Gtk.SpinButton (0.00, 100.0, .01));
			SetFancyStyle (radius_spin = new Gtk.SpinButton (1.0, 50.0, .01));
			SetFancyStyle (threshold_spin = new Gtk.SpinButton (0.0, 50.0, .01));
			amount_spin.Value = .5;
			radius_spin.Value = 5;
			threshold_spin.Value = 0.0;

			amount_spin.ValueChanged += HandleSettingsChanged;
			radius_spin.ValueChanged += HandleSettingsChanged;
			threshold_spin.ValueChanged += HandleSettingsChanged;

			table.Attach (amount_spin, 1, 2, 0, 1);
			table.Attach (radius_spin, 1, 2, 1, 2);
			table.Attach (threshold_spin, 1, 2, 2, 3);

			Gtk.Button cancel_button = new Gtk.Button (Gtk.Stock.Cancel);
			cancel_button.Clicked += HandleCancelClicked;
			dialog.AddActionWidget (cancel_button, Gtk.ResponseType.Cancel);

			Gtk.Button ok_button = new Gtk.Button (Gtk.Stock.Ok);
			ok_button.Clicked += HandleOkClicked;
			dialog.AddActionWidget (ok_button, Gtk.ResponseType.Cancel);

			dialog.DeleteEvent += HandleCancelClicked;

			Destroyed += HandleLoupeDestroyed;

			table.ShowAll ();
			dialog.VBox.PackStart (table);
			dialog.ShowAll ();
		}

		void HandleDeleteEvent (object o, DeleteEventArgs args)
		{

		}
	}
}
