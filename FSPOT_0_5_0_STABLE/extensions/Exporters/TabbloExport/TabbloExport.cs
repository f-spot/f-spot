//
// FSpotTabbloExport.TabbloExport
//
// Authors:
//	Wojciech Dzierzanowski (wojciech.dzierzanowski@gmail.com)
//
// (C) Copyright 2008 Wojciech Dzierzanowski
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using Mono.Tabblo;
using Mono.Unix;

using System;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Threading;

using FSpot.Utils;

namespace FSpotTabbloExport {
	/// <summary>
	/// </summary>
	public class TabbloExport : FSpot.Extensions.IExporter {

		private readonly Preferences preferences;
		private readonly Connection connection;

		private FSpot.IBrowsableCollection photos;

		private const string DialogName = "tabblo_export_dialog";
		[Glade.Widget] Gtk.Dialog dialog;
		[Glade.Widget] Gtk.Entry username_entry;
		[Glade.Widget] Gtk.Entry password_entry;
		[Glade.Widget] Gtk.Button export_button;
		[Glade.Widget] Gtk.Button cancel_button;
		[Glade.Widget] Gtk.ScrolledWindow thumb_scrolled_window;

		private FSpot.ThreadProgressDialog progress_dialog;

		// Keyring constants.
		private const string KeyringItemName = "Tabblo Account";
		private const string KeyringItemApp = "FSpotTabbloExport";
		private const string KeyringItemNameAttr = "name";
		private const string KeyringItemUsernameAttr = "username";
		private const string KeyringItemAppAttr = "application";


		public TabbloExport ()
		{
			preferences = new Preferences ();
			connection = new Connection (preferences);
		}


		public void Run (FSpot.IBrowsableCollection photos)
		{
			if (null == photos) {
				throw new ArgumentNullException ("photos");
			}

			this.photos = photos;

			Glade.XML glade_xml = new Glade.XML (
					null, "TabbloExport.glade", DialogName,
					"f-spot");
			glade_xml.Autoconnect (this);

			dialog = (Gtk.Dialog) glade_xml.GetWidget (DialogName);

			FSpot.Widgets.IconView icon_view =
					new FSpot.Widgets.IconView (photos);
			icon_view.DisplayDates = false;
			icon_view.DisplayTags = false;

			username_entry.Changed += HandleAccountDataChanged;
			password_entry.Changed += HandleAccountDataChanged;
			ReadAccountData ();
			HandleAccountDataChanged (null, null);

			dialog.Modal = false;
			dialog.TransientFor = null;

			dialog.Response += HandleResponse;

			thumb_scrolled_window.Add (icon_view);
			icon_view.Show ();
			dialog.Show ();
		}


		private void HandleAccountDataChanged (object sender,
		                                       EventArgs args)
		{
			preferences.SetUsername (username_entry.Text);
			preferences.SetPassword (password_entry.Text);

			export_button.Sensitive =
					preferences.Username.Length > 0
					&& preferences.Password.Length > 0;
		}


		private void HandleResponse (object sender,
		                             Gtk.ResponseArgs args)
		{
			dialog.Destroy ();

			if (Gtk.ResponseType.Ok != args.ResponseId) {
				Log.DebugFormat ("Tabblo export was canceled.");
				return;
			}

			WriteAccountData ();

			Log.DebugFormat ("Starting Tabblo export");

			Thread upload_thread =
					new Thread (new ThreadStart (Upload));
			progress_dialog = new FSpot.ThreadProgressDialog (
					upload_thread, photos.Items.Length);
			progress_dialog.Start ();
		}


		private void Upload ()
		{
			if (null == connection) Log.DebugFormat ("No connection");

			Picture [] pictures = GetPicturesForUpload ();

			FSpotUploadProgress fup = new FSpotUploadProgress (
					pictures, progress_dialog);
			connection.UploadProgressHandler += fup.HandleProgress;

			ServicePointManager.CertificatePolicy =
					new UserDecisionCertificatePolicy ();

			try {
				foreach (Picture picture in pictures) {
					picture.Upload (connection);
				}

				progress_dialog.Message = Catalog.GetString (
						"Done sending photos");
				progress_dialog.ProgressText = Catalog
						.GetString ("Upload complete");
				progress_dialog.Fraction = 1;
				progress_dialog.ButtonLabel = Gtk.Stock.Ok;

			} catch (TabbloException e) {
				progress_dialog.Message = Catalog.GetString (
						"Error uploading to Tabblo: ")
						+ e.Message;
				progress_dialog.ProgressText =
						Catalog.GetString ("Error");
				// FIXME:  Retry logic?
//				  progressDialog.PerformRetrySkip ();
				Log.DebugFormat ("Error uploading:\n" + e);
			} finally {
				connection.UploadProgressHandler -=
						fup.HandleProgress;
			}
		}


		private Picture [] GetPicturesForUpload ()
		{
			if (null == photos) Log.DebugFormat ("No photos");
			if (null == preferences) Log.DebugFormat ("No preferences");

			Picture [] pictures = new Picture [photos.Items.Length];

			for (int i = 0; i < pictures.Length; ++i) {
				FSpot.IBrowsableItem photo = photos.Items [i];

				// FIXME: GnomeVFS is deprecated, we should use
				// GIO instead.  However, I don't know how to
				// call `GLib.Content.TypeGuess ()'.
				string path = photo.DefaultVersionUri.LocalPath;
				string mime_type = Gnome.Vfs.MimeType
						.GetMimeTypeForUri (path);

				pictures [i] = new Picture (photo.Name,
						photo.DefaultVersionUri,
						mime_type,
						preferences.Privacy);
			}

			return pictures;
		}


		private void ReadAccountData ()
		{
			Hashtable attrs = new Hashtable ();
			attrs [KeyringItemNameAttr] = KeyringItemName;
			attrs [KeyringItemAppAttr] = KeyringItemApp;

			try {
				Gnome.Keyring.ItemType type = Gnome.Keyring
						.ItemType.GenericSecret;
				Gnome.Keyring.ItemData [] items =
						Gnome.Keyring.Ring.Find (
								type, attrs);
				if (items.Length > 1)
					Log.Warning ("More than one " + KeyringItemName + "found in keyring");

				if (1 <= items.Length) {
					Log.DebugFormat (KeyringItemName + " data found in " + "keyring");
					attrs =	items [0].Attributes;
					username_entry.Text = (string) attrs [
						KeyringItemUsernameAttr];
					password_entry.Text = items [0].Secret;
				}

			} catch (Gnome.Keyring.KeyringException e) {
				Log.DebugFormat ("Error while reading account data:\n" + e);
			}
		}


		private void WriteAccountData ()
		{
			try {
				string keyring = Gnome.Keyring
						.Ring.GetDefaultKeyring ();

				Hashtable attrs = new Hashtable ();
				attrs [KeyringItemNameAttr] = KeyringItemName;
				attrs [KeyringItemAppAttr] = KeyringItemApp;

				Gnome.Keyring.ItemType type = Gnome.Keyring
						.ItemType.GenericSecret;

				try {
					Gnome.Keyring.ItemData [] items = Gnome
							.Keyring.Ring.Find (
									type,
									attrs);

					foreach (Gnome.Keyring.ItemData item
							in items) {
						Gnome.Keyring.Ring.DeleteItem (
								keyring,
								item.ItemID);
					}
				} catch (Gnome.Keyring.KeyringException e) {
					Log.DebugFormat ("Error while deleting old account data:\n" + e);
				}

				attrs [KeyringItemUsernameAttr] =
						preferences.Username;

				Gnome.Keyring.Ring.CreateItem (keyring, type,
						KeyringItemName, attrs,
						preferences.Password, true);

			} catch (Gnome.Keyring.KeyringException e) {
				Log.DebugFormat ("Error while writing account data:\n" + e);
			}
		}
	}
}
