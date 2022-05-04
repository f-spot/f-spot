//
// GoogleAddAlbum.cs
//
// Author:
//   Paul Lange <palango@gmx.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Paul Lange
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Mono.Unix;

using Hyena.Widgets;

using Mono.Google.Picasa;

namespace FSpot.Exporters.PicasaWeb
{
	public class GoogleAddAlbum
	{
#pragma warning disable 649
		[GtkBeans.Builder.Object] Gtk.Dialog dialog;

		[GtkBeans.Builder.Object] Gtk.Entry title_entry;
		[GtkBeans.Builder.Object] Gtk.Entry description_entry;
		[GtkBeans.Builder.Object] Gtk.CheckButton public_check;

		[GtkBeans.Builder.Object] Gtk.Button add_button;
#pragma warning restore 649

		private GtkBeans.Builder builder;
		private string dialog_name = "google_add_album_dialog";

		private GoogleExport export;
		private Mono.Google.Picasa.PicasaWeb picasa;
		private string description;
		private string title;
		private bool public_album;

		public GoogleAddAlbum (GoogleExport export, Mono.Google.Picasa.PicasaWeb picasa)
		{
			builder = new GtkBeans.Builder (null, "google_add_album_dialog.ui", null);
			builder.Autoconnect (this);

			this.export = export;
			this.picasa = picasa;

			Dialog.Response += HandleAddResponse;

			description_entry.Changed += HandleChanged;
			title_entry.Changed += HandleChanged;
			HandleChanged (null, null);
		}

		private void HandleChanged (object sender, EventArgs args)
		{
			description = description_entry.Text;
			title = title_entry.Text;
			public_album = public_check.Active;

			if (title == string.Empty)
				add_button.Sensitive = false;
			else
				add_button.Sensitive = true;
		}

		[GLib.ConnectBefore]
		protected void HandleAddResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId == Gtk.ResponseType.Ok) {
				public_album = public_check.Active;

				try {
					picasa.CreateAlbum (System.Web.HttpUtility.HtmlEncode (title), description, public_album ? AlbumAccess.Public : AlbumAccess.Private);
				} catch (System.Exception e) {
					HigMessageDialog md =
					new HigMessageDialog (Dialog,
							      Gtk.DialogFlags.Modal |
							      Gtk.DialogFlags.DestroyWithParent,
								      Gtk.MessageType.Error, Gtk.ButtonsType.Ok,
							      Catalog.GetString ("Error while creating Album"),
							      string.Format (Catalog.GetString ("The following error was encountered while attempting to create an album: {0}"), e.Message));
					md.Run ();
					md.Destroy ();
					return;
				}
				export.HandleAlbumAdded (title);
			}
			Dialog.Destroy ();
		}

		private Gtk.Dialog Dialog {
			get {
				if (dialog == null)
					dialog = new Gtk.Dialog (builder.GetRawObject (dialog_name));

				return dialog;
			}
		}
	}
}
