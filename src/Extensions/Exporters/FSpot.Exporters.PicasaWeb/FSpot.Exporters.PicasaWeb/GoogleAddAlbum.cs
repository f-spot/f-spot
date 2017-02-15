//
// GoogleAddAlbum.cs
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

using Mono.Google.Picasa;

namespace FSpot.Exporters.PicasaWeb
{
	public class GoogleAddAlbum
	{
		[GtkBeans.Builder.Object] Gtk.Dialog dialog;

		[GtkBeans.Builder.Object] Gtk.Entry title_entry;
		[GtkBeans.Builder.Object] Gtk.Entry description_entry;
		[GtkBeans.Builder.Object] Gtk.CheckButton public_check;

		[GtkBeans.Builder.Object] Gtk.Button add_button;

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
