//
// PicasaWebExport.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2006-2009 Novell, Inc.
// Copyright (C) 2006-2009 Stephane Delcroix
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

/*
 * PicasaWebExport.cs
 *
 * Authors:
 *   Stephane Delcroix <stephane@delcroix.org>
 *
 * Copyright (C) 2006 Stephane Delcroix
 */
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Mono.Unix;

using Hyena;

using FSpot;
using FSpot.Core;
using FSpot.Database;
using FSpot.Filters;
using FSpot.Settings;
using FSpot.Widgets;
using FSpot.UI.Dialog;

using Mono.Google;
using Mono.Google.Picasa;
using System.Linq;

namespace FSpot.Exporters.PicasaWeb
{
	public class GoogleExport : FSpot.Extensions.IExporter
	{
		public GoogleExport ()
		{
		}

		public void Run (IBrowsableCollection selection)
		{
			builder = new GtkBeans.Builder (null, "google_export_dialog.ui", null);
			builder.Autoconnect (this);

			gallery_optionmenu = Gtk.ComboBox.NewText ();
			album_optionmenu = Gtk.ComboBox.NewText ();

			(edit_button.Parent as Gtk.HBox).PackStart (gallery_optionmenu);
			(album_button.Parent as Gtk.HBox).PackStart (album_optionmenu);
			(edit_button.Parent as Gtk.HBox).ReorderChild (gallery_optionmenu, 1);
			(album_button.Parent as Gtk.HBox).ReorderChild (album_optionmenu, 1);
		
			gallery_optionmenu.Show ();
			album_optionmenu.Show ();

			this.items = selection.Items.ToArray ();
			album_button.Sensitive = false;
			var view = new TrayView (selection);
			view.DisplayDates = false;
			view.DisplayTags = false;

			Dialog.Modal = false;
			Dialog.TransientFor = null;

			thumb_scrolledwindow.Add (view);
			view.Show ();
			Dialog.Show ();

			GoogleAccountManager manager = GoogleAccountManager.GetInstance ();
			manager.AccountListChanged += PopulateGoogleOptionMenu;
			PopulateGoogleOptionMenu (manager, null);
			album_optionmenu.Changed += HandleAlbumOptionMenuChanged;

			if (edit_button != null)
				edit_button.Clicked += HandleEditGallery;

			Dialog.Response += HandleResponse;
			connect = true;
			HandleSizeActive (null, null);
			Connect ();

			LoadPreference (SCALE_KEY);
			LoadPreference (SIZE_KEY);
			LoadPreference (BROWSER_KEY);
			LoadPreference (TAG_KEY);
		}

		private bool scale;
		private int size;
		private bool browser;
		private bool export_tag;
		private bool connect = false;
		private long approx_size = 0;
		private long sent_bytes = 0;
		IPhoto[] items;
		int photo_index;
		ThreadProgressDialog progress_dialog;
		List<GoogleAccount> accounts;
		private GoogleAccount account;
		private PicasaAlbum album;
		private PicasaAlbumCollection albums = null;
		private GtkBeans.Builder builder;
		private string dialog_name = "google_export_dialog";
		public const string EXPORT_SERVICE = "picasaweb/";
		public const string SCALE_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "scale";
		public const string SIZE_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "size";
		public const string BROWSER_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "browser";
		public const string TAG_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "tag";

		// widgets
		[GtkBeans.Builder.Object]
		Gtk.Dialog dialog;
		Gtk.ComboBox gallery_optionmenu;
		Gtk.ComboBox album_optionmenu;
#pragma warning disable 649
		[GtkBeans.Builder.Object]
		Gtk.Label status_label;
		[GtkBeans.Builder.Object]
		Gtk.Label album_status_label;
		[GtkBeans.Builder.Object]
		Gtk.CheckButton browser_check;
		[GtkBeans.Builder.Object]
		Gtk.CheckButton scale_check;
		[GtkBeans.Builder.Object]
		Gtk.CheckButton tag_check;
		[GtkBeans.Builder.Object]
		Gtk.SpinButton size_spin;
		[GtkBeans.Builder.Object]
		Gtk.Button album_button;
		[GtkBeans.Builder.Object]
		Gtk.Button edit_button;
		[GtkBeans.Builder.Object]
		Gtk.Button export_button;
		[GtkBeans.Builder.Object]
		Gtk.ScrolledWindow thumb_scrolledwindow;
#pragma warning restore 649
		System.Threading.Thread command_thread;

		private void HandleResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId != Gtk.ResponseType.Ok) {
				Dialog.Destroy ();
				return;
			}

			if (scale_check != null) {
				scale = scale_check.Active;
				size = size_spin.ValueAsInt;
			} else
				scale = false;

			browser = browser_check.Active;
			export_tag = tag_check.Active;

			if (account != null) {
				album = (PicasaAlbum)account.Picasa.GetAlbums () [Math.Max (0, album_optionmenu.Active)];
				photo_index = 0;

				Dialog.Destroy ();

				command_thread = new System.Threading.Thread (new System.Threading.ThreadStart (this.Upload));
				command_thread.Name = Catalog.GetString ("Uploading Pictures");

				progress_dialog = new ThreadProgressDialog (command_thread, items.Length);
				progress_dialog.Start ();

				// Save these settings for next time
				Preferences.Set (SCALE_KEY, scale);
				Preferences.Set (SIZE_KEY, size);
				Preferences.Set (BROWSER_KEY, browser);
//				Preferences.Set (Preferences.EXPORT_GALLERY_META, meta);
				Preferences.Set (TAG_KEY, export_tag);
			}
		}

		public void HandleSizeActive (object sender, EventArgs args)
		{
			size_spin.Sensitive = scale_check.Active;
		}

		void HandleUploadProgress (object o, UploadProgressEventArgs args)
		{
			if (approx_size == 0)
				progress_dialog.ProgressText = string.Format (Catalog.GetString ("{0} Sent"), GLib.Format.SizeForDisplay (args.BytesSent));
			else
				progress_dialog.ProgressText = string.Format (Catalog.GetString ("{0} of approx. {1}"), GLib.Format.SizeForDisplay (sent_bytes + args.BytesSent), GLib.Format.SizeForDisplay (approx_size));
			progress_dialog.Fraction = ((photo_index - 1) / (double)items.Length) + (args.BytesSent / (args.BytesTotal * (double)items.Length));
		}

		class DateComparer : IComparer
		{
			public int Compare (object left, object right)
			{
				return DateTime.Compare ((left as IPhoto).Time, (right as IPhoto).Time);
			}
		}

		void Upload ()
		{
			album.UploadProgress += HandleUploadProgress;
			sent_bytes = 0;
			approx_size = 0;

			Log.Debug ("Starting Upload to Picasa");

			FilterSet filters = new FilterSet ();
			filters.Add (new JpegFilter ());

			if (scale)
				filters.Add (new ResizeFilter ((uint)size));

			Array.Sort (items, new DateComparer ());

			while (photo_index < items.Length) {
				try {
					IPhoto item = items [photo_index];

					FileInfo file_info;
					Log.Debug ("Picasa uploading " + photo_index);

					progress_dialog.Message = string.Format (Catalog.GetString ("Uploading picture \"{0}\" ({1} of {2})"),
										 item.Name, photo_index + 1, items.Length);
					photo_index++;

					PicasaPicture picture;
					using (FilterRequest request = new FilterRequest (item.DefaultVersion.Uri)) {
						filters.Convert (request);
						file_info = new FileInfo (request.Current.LocalPath);

						if (approx_size == 0) //first image
							approx_size = file_info.Length * items.Length;
						else
							approx_size = sent_bytes * items.Length / (photo_index - 1);

						picture = album.UploadPicture (request.Current.LocalPath, Path.ChangeExtension (item.Name, "jpg"), item.Description);
						sent_bytes += file_info.Length;
					}
					if (App.Instance.Database != null && item is Photo)
						App.Instance.Database.Exports.Create ((item as Photo).Id,
									      (item as Photo).DefaultVersionId,
									      ExportStore.PicasaExportType,
									      picture.Link);

					//tagging
					if (item.Tags != null && export_tag)
						foreach (Tag tag in item.Tags) {
							picture.AddTag (tag.Name);
						}
				} catch (System.Threading.ThreadAbortException te) {
					Log.Exception (te);
					System.Threading.Thread.ResetAbort ();
				} catch (System.Exception e) {
					progress_dialog.Message = string.Format (Catalog.GetString ("Error Uploading To Gallery: {0}"),
										 e.Message);
					progress_dialog.ProgressText = Catalog.GetString ("Error");
					Log.DebugException (e);

					if (progress_dialog.PerformRetrySkip ()) {
						photo_index--;
						if (photo_index == 0)
							approx_size = 0;
					}
				}
			}

			progress_dialog.Message = Catalog.GetString ("Done Sending Photos");
			progress_dialog.Fraction = 1.0;
			progress_dialog.ProgressText = Catalog.GetString ("Upload Complete");
			progress_dialog.ButtonLabel = Gtk.Stock.Ok;

			if (browser)
				GtkBeans.Global.ShowUri (Dialog.Screen, album.Link);
		}

		private void PopulateGoogleOptionMenu (GoogleAccountManager manager, GoogleAccount changed_account)
		{
			this.account = changed_account;
			int pos = -1;

			accounts = manager.GetAccounts ();
			if (accounts == null || accounts.Count == 0) {

				if (accounts == null)
					Log.Debug ("accounts == null");
				else
					Log.Debug ("accounts != null");

				Log.DebugFormat ("accounts.Count = {0}", accounts.Count);

				gallery_optionmenu.AppendText (Catalog.GetString ("(No Gallery)"));
				gallery_optionmenu.Sensitive = false;
				edit_button.Sensitive = false;

				pos = 0;
			} else {
				int i = 0;
				pos = 0;
				foreach (GoogleAccount account in accounts) {
					if (account == changed_account)
						pos = i;

					gallery_optionmenu.AppendText (account.Username);
					i++;
				}
				gallery_optionmenu.Sensitive = true;
				edit_button.Sensitive = true;
			}

			Log.DebugFormat ("Setting gallery_optionmenu.Active = {0}", pos);
			gallery_optionmenu.Active = pos;
		}

		private void Connect ()
		{
			Connect (null);
		}

		private void Connect (GoogleAccount selected)
		{
			Connect (selected, null, null);
		}

		private void Connect (GoogleAccount selected, string token, string text)
		{
			try {
				if (accounts.Count != 0 && connect) {
					if (selected == null)
						account = (GoogleAccount)accounts [gallery_optionmenu.Active];
					else
						account = selected;

					if (!account.Connected)
						account.Connect ();

					PopulateAlbumOptionMenu (account.Picasa);

					long qu = account.Picasa.QuotaUsed;
					long ql = account.Picasa.QuotaLimit;

					StringBuilder sb = new StringBuilder ("<small>");
					sb.Append (string.Format (Catalog.GetString ("Available space: {0}, {1}% used out of {2}"),
								GLib.Format.SizeForDisplay (ql - qu),
								(100 * qu / ql),
								GLib.Format.SizeForDisplay (ql)));
					sb.Append ("</small>");
					status_label.Text = sb.ToString ();
					status_label.UseMarkup = true;

					album_button.Sensitive = true;
				}
			} catch (CaptchaException exc) {
				Log.Debug ("Your Google account is locked");
				if (selected != null)
					account = selected;

				PopulateAlbumOptionMenu (account.Picasa);
				album_button.Sensitive = false;

				new GoogleAccountDialog (this.Dialog, account, false, exc);

				Log.Warning ("Your Google account is locked, you can unlock it by visiting: {0}", CaptchaException.UnlockCaptchaURL);

			} catch (System.Exception) {
				Log.Warning ("Can not connect to Picasa. Bad username? password? network connection?");
				if (selected != null)
					account = selected;

				PopulateAlbumOptionMenu (account.Picasa);

				status_label.Text = string.Empty;
				album_button.Sensitive = false;

				new GoogleAccountDialog (this.Dialog, account, true, null);
			}
		}

		private void HandleAccountSelected (object sender, System.EventArgs args)
		{
			Connect ();
		}

		public void HandleAlbumAdded (string title)
		{
			GoogleAccount account = (GoogleAccount)accounts [gallery_optionmenu.Active];
			PopulateAlbumOptionMenu (account.Picasa);

			// make the newly created album selected
//			PicasaAlbumCollection albums = account.Picasa.GetAlbums();
			for (int i=0; i < albums.Count; i++) {
				if (((PicasaAlbum)albums [i]).Title == title)
					album_optionmenu.Active = i;
			}
		}

		private void PopulateAlbumOptionMenu (Mono.Google.Picasa.PicasaWeb picasa)
		{
			if (picasa != null)
				try {
					albums = picasa.GetAlbums();
				} catch {
					Log.Warning ("Picasa: can't get the albums");
					albums = null;
					picasa = null;
				}

			bool disconnected = picasa == null || !account.Connected || albums == null;

			if (disconnected || albums.Count == 0) {
				string msg = disconnected ? Catalog.GetString ("(Not Connected)")
					: Catalog.GetString ("(No Albums)");

				album_optionmenu.AppendText (msg);

				export_button.Sensitive = false;
				album_optionmenu.Sensitive = false;
				album_button.Sensitive = false;

				if (disconnected)
					album_button.Sensitive = false;
			} else {
				foreach (PicasaAlbum album in albums.AllValues) {
					System.Text.StringBuilder label_builder = new System.Text.StringBuilder ();

					label_builder.Append (album.Title);
					label_builder.Append (" (" + album.PicturesCount + ")");

					album_optionmenu.AppendText (label_builder.ToString ());
				}

				export_button.Sensitive = items.Length > 0;
				album_optionmenu.Sensitive = true;
				album_button.Sensitive = true;
			}
		}

		public void HandleAlbumOptionMenuChanged (object sender, System.EventArgs args)
		{
			if (albums == null || albums.Count == 0)
				return;

			PicasaAlbum a = albums [album_optionmenu.Active];
			export_button.Sensitive = a.PicturesRemaining >= items.Length;
			if (album_status_label.Visible = !export_button.Sensitive) {
				StringBuilder sb = new StringBuilder ("<small>");
				sb.Append (string.Format (Catalog.GetString ("The selected album has a limit of {0} pictures,\n" +
								"which would be passed with the current selection of {1} images"),
								a.PicturesCount + a.PicturesRemaining, items.Length));
				sb.Append ("</small>");
				album_status_label.Text = string.Format (sb.ToString ());
				album_status_label.UseMarkup = true;
			} else
				album_status_label.Text = string.Empty;
		}

		public void HandleAddGallery (object sender, System.EventArgs args)
		{
			new GoogleAccountDialog (this.Dialog);
		}

		public void HandleEditGallery (object sender, System.EventArgs args)
		{
			new GoogleAccountDialog (this.Dialog, account, false, null);
		}

		public void HandleAddAlbum (object sender, System.EventArgs args)
		{
			if (account == null)
				throw new Exception (Catalog.GetString ("No account selected"));

			new GoogleAddAlbum (this, account.Picasa);
		}

		void LoadPreference (string key)
		{
			switch (key) {
			case SCALE_KEY:
				if (scale_check.Active != Preferences.Get<bool> (key))
					scale_check.Active = Preferences.Get<bool> (key);
				break;

			case SIZE_KEY:
				size_spin.Value = (double)Preferences.Get<int> (key);
				break;

			case BROWSER_KEY:
				if (browser_check.Active != Preferences.Get<bool> (key))
					browser_check.Active = Preferences.Get<bool> (key);
				break;

			case TAG_KEY:
				if (tag_check.Active != Preferences.Get<bool> (key))
					tag_check.Active = Preferences.Get<bool> (key);
				break;
			}
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
