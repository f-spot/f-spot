using System;
using System.Net;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Web;
using Mono.Unix;
using Hyena;
using Hyena.Widgets;
using FSpot;
using FSpot.Core;
using FSpot.Filters;
using FSpot.Widgets;
using FSpot.Imaging;
using FSpot.UI.Dialog;
using Gnome.Keyring;
using Mono.Google;
using Mono.Google.Picasa;

namespace FSpot.Exporters.PicasaWeb
{
	public class GoogleAddAlbum {
		[Glade.Widget] Gtk.Dialog dialog;
		[Glade.Widget] Gtk.OptionMenu album_optionmenu;

		[Glade.Widget] Gtk.Entry title_entry;
		[Glade.Widget] Gtk.Entry description_entry;
		[Glade.Widget] Gtk.CheckButton public_check;

		[Glade.Widget] Gtk.Button add_button;
		[Glade.Widget] Gtk.Button cancel_button;

		private Glade.XML xml;
		private string dialog_name = "google_add_album_dialog";

		private GoogleExport export;
		private Mono.Google.Picasa.PicasaWeb picasa;
		private string description;
		private string title;
		private bool public_album;

		public GoogleAddAlbum (GoogleExport export, Mono.Google.Picasa.PicasaWeb picasa)
		{
			xml = new Glade.XML (null, "PicasaWebExport.glade", dialog_name, "f-spot");
			xml.Autoconnect (this);

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

			if (title == String.Empty)
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
							      String.Format (Catalog.GetString ("The following error was encountered while attempting to create an album: {0}"), e.Message));
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
					dialog = (Gtk.Dialog) xml.GetWidget (dialog_name);

				return dialog;
			}
		}
	}
}
