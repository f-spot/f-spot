//
// GalleryAddAlbum.cs
//
// Author:
//   Paul Lange <palango@gmx.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Paul Lange
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

using Mono.Unix;

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

		private GalleryExport export;
		private Gallery gallery;
		private string parent;
		private string name;
		private string description;
		private string title;

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

		private void PopulateAlbums ()
		{
			Gtk.Menu menu = new Gtk.Menu ();
			if (gallery.Version == GalleryVersion.Version1) {
				Gtk.MenuItem top_item = new Gtk.MenuItem (Catalog.GetString ("(TopLevel)"));
				menu.Append (top_item);
			}

			foreach (Album album in gallery.Albums) {
				System.Text.StringBuilder label_builder = new System.Text.StringBuilder ();

				for (int i=0; i < album.Parents.Count; i++) {
					label_builder.Append ("  ");
				}

				label_builder.Append (album.Title);
				album_optionmenu.AppendText(label_builder.ToString());
			}

			album_optionmenu.Sensitive = true;
			menu.ShowAll ();
		}

		private void HandleChanged (object sender, EventArgs args)
		{
			if (gallery.Version == GalleryVersion.Version1)
				if (gallery.Albums.Count == 0 || album_optionmenu.Active <= 0)
					parent = string.Empty;
				else
					parent = ((Album) gallery.Albums [album_optionmenu.Active-1]).Name;
			else
				if (gallery.Albums.Count == 0 || album_optionmenu.Active < 0)
					parent = string.Empty;
				else
					parent = ((Album) gallery.Albums [album_optionmenu.Active]).Name;

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
					HigMessageDialog md =
						new HigMessageDialog (add_album_dialog,
								      Gtk.DialogFlags.Modal |
								      Gtk.DialogFlags.DestroyWithParent,
								      Gtk.MessageType.Error, Gtk.ButtonsType.Ok,
								      Catalog.GetString ("Invalid Gallery name"),
								      Catalog.GetString ("The gallery name contains invalid characters.\nOnly letters, numbers, - and _ are allowed"));
					md.Run ();
					md.Destroy ();
					return;
				}
				try {
					gallery.NewAlbum (parent, name, title, description);
					export.HandleAlbumAdded (title);
				} catch (GalleryCommandException e) {
					gallery.PopupException(e, add_album_dialog);
					return;
				}
			}
			add_album_dialog.Destroy ();
		}
	}
}
