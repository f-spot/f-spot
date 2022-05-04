//
// GalleryAddAlbum.cs
//
// Author:
//   Paul Lange <palango@gmx.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Paul Lange
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Resources.Lang;

using Hyena.Widgets;

namespace FSpot.Exporters.Gallery
{
	public class GalleryAddAlbum
	{
		[GtkBeans.Builder.Object] Gtk.Dialog add_album_dialog;
		Gtk.ComboBox album_optionmenu;

#pragma warning disable 649
		[GtkBeans.Builder.Object] Gtk.Entry name_entry;
		[GtkBeans.Builder.Object] Gtk.Entry description_entry;
		[GtkBeans.Builder.Object] Gtk.Entry title_entry;

		[GtkBeans.Builder.Object] Gtk.Button add_button;
#pragma warning restore 649

		GalleryExport export;
		Gallery gallery;
		string parent;
		string name;
		string description;
		string title;

		public GalleryAddAlbum (GalleryExport export, Gallery gallery)
		{
			var builder = new GtkBeans.Builder (null, "gallery_add_album_dialog.ui", null);
			builder.Autoconnect (this);
			add_album_dialog = new Gtk.Dialog (builder.GetRawObject ("gallery_add_album_dialog"));
			add_album_dialog.Modal = true;

			album_optionmenu = new Gtk.ComboBox ();
			(name_entry.Parent as Gtk.Table).Attach (album_optionmenu, 1, 2, 1, 2);
			album_optionmenu.Show ();

			this.export = export;
			this.gallery = gallery;
			PopulateAlbums ();

			add_album_dialog.Response += HandleAddResponse;

			name_entry.Changed += HandleChanged;
			description_entry.Changed += HandleChanged;
			title_entry.Changed += HandleChanged;
			HandleChanged (null, null);
		}

		void PopulateAlbums ()
		{
			var menu = new Gtk.Menu ();
			if (gallery.Version == GalleryVersion.Version1) {
				var top_item = new Gtk.MenuItem (Strings.ParenTopLevelParen);
				menu.Append (top_item);
			}

			foreach (Album album in gallery.Albums) {
				var label_builder = new System.Text.StringBuilder ();

				for (int i = 0; i < album.Parents.Count; i++) {
					label_builder.Append ("  ");
				}

				label_builder.Append (album.Title);
				album_optionmenu.AppendText (label_builder.ToString ());
			}

			album_optionmenu.Sensitive = true;
			menu.ShowAll ();
		}

		void HandleChanged (object sender, EventArgs args)
		{
			if (gallery.Version == GalleryVersion.Version1)
				if (gallery.Albums.Count == 0 || album_optionmenu.Active <= 0)
					parent = string.Empty;
				else
					parent = ((Album)gallery.Albums[album_optionmenu.Active - 1]).Name;
			else
				if (gallery.Albums.Count == 0 || album_optionmenu.Active < 0)
				parent = string.Empty;
			else
				parent = ((Album)gallery.Albums[album_optionmenu.Active]).Name;

			name = name_entry.Text;
			description = description_entry.Text;
			title = title_entry.Text;

			if (name == string.Empty || title == string.Empty)
				add_button.Sensitive = false;
			else
				add_button.Sensitive = true;
		}

		[GLib.ConnectBefore]
		protected void HandleAddResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId == Gtk.ResponseType.Ok) {
				if (!System.Text.RegularExpressions.Regex.IsMatch (name, "^[A-Za-z0-9_-]+$")) {
					var md =
						new HigMessageDialog (add_album_dialog,
									  Gtk.DialogFlags.Modal |
									  Gtk.DialogFlags.DestroyWithParent,
									  Gtk.MessageType.Error, Gtk.ButtonsType.Ok,
									  Strings.InvalidGalleryName,
									  Strings.TheGalleryNameContainsInvalidCharactersOnlyLettersNumbersUnderscoreAllowed);
					md.Run ();
					md.Destroy ();
					return;
				}
				try {
					gallery.NewAlbum (parent, name, title, description);
					export.HandleAlbumAdded (title);
				} catch (GalleryCommandException e) {
					gallery.PopupException (e, add_album_dialog);
					return;
				}
			}
			add_album_dialog.Destroy ();
		}
	}
}
