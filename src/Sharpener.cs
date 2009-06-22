//
// FSpot.Widgets.Sharpener.cs
//
// Author(s):
//	Larry Ewing  <lewing@novell.com:
//
// Copyright (c) 2005-2009 Novell, Inc.
//
// This is free software. See COPYING for details.
//
using Cairo;

using Gtk;
using Gdk;
using System;
using System.Runtime.InteropServices;
using Mono.Unix;
using FSpot.Widgets;
using FSpot.UI.Dialog;

namespace FSpot.Widgets {
	public class Sharpener : Loupe {
		Gtk.SpinButton amount_spin = new Gtk.SpinButton (0.5, 100.0, .01);
		Gtk.SpinButton radius_spin = new Gtk.SpinButton (5.0, 50.0, .01);
		Gtk.SpinButton threshold_spin = new Gtk.SpinButton (0.0, 50.0, .01);
		Gtk.Dialog dialog;
		
		public Sharpener (PhotoImageView view) : base (view)
		{	
		}

		protected override void UpdateSample ()
		{
			base.UpdateSample ();

			if (overlay != null)
				overlay.Dispose ();

			overlay = null;
			if (source != null)
				overlay = PixbufUtils.UnsharpMask (source, 
								   radius_spin.Value, 
								   amount_spin.Value, 
								   threshold_spin.Value);
		}

		private void HandleSettingsChanged (object sender, EventArgs args)
		{
			UpdateSample ();
		}
		
		private void HandleOkClicked (object sender, EventArgs args)
		{
			Photo photo = view.Item.Current as Photo;

			if (photo == null)
				return;
			
			try {
				Gdk.Pixbuf orig = view.Pixbuf;
				Gdk.Pixbuf final = PixbufUtils.UnsharpMask (orig,
									    radius_spin.Value,
									    amount_spin.Value,
									    threshold_spin.Value);
				
				bool create_version = photo.DefaultVersion.IsProtected;

				photo.SaveVersion (final, create_version);
				photo.Changes.DataChanged = true;
				Core.Database.Photos.Commit (photo);
			} catch (System.Exception e) {
				string msg = Catalog.GetString ("Error saving sharpened photo");
				string desc = String.Format (Catalog.GetString ("Received exception \"{0}\". Unable to save photo {1}"),
							     e.Message, photo.Name);
				
				HigMessageDialog md = new HigMessageDialog (this, DialogFlags.DestroyWithParent, 
									    Gtk.MessageType.Error,
									    ButtonsType.Ok, 
									    msg,
									    desc);
				md.Run ();
				md.Destroy ();
			}
			
			Destroy ();
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
			dialog = new Gtk.Dialog (title, (Gtk.Window) this,
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

			Destroyed += HandleLoupeDestroyed;
			
			table.ShowAll ();
			dialog.VBox.PackStart (table);
			dialog.ShowAll ();
		}
	}
}

