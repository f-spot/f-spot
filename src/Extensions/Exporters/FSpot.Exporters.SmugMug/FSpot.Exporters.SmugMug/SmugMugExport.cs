//
// SmugMugExport.cs
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
 * SmugMugExport.cs
 *
 * Authors:
 *   Thomas Van Machelen <thomas.vanmachelen@gmail.com>
 *
 * Based on PicasaWebExport code from Stephane Delcroix.
 *
 * Copyright (C) 2006 Thomas Van Machelen
 */

using System;
using System.IO;
using System.Collections.Generic;

using Mono.Unix;

using FSpot;
using FSpot.Core;
using FSpot.Database;
using FSpot.Filters;
using FSpot.Settings;
using FSpot.Widgets;
using FSpot.UI.Dialog;

using Hyena;

using SmugMugNet;
using System.Linq;

namespace FSpot.Exporters.SmugMug
{
	public class SmugMugExport : FSpot.Extensions.IExporter
	{
		public SmugMugExport () {}
		public void Run (IBrowsableCollection selection)
		{
			builder = new GtkBeans.Builder (null, "smugmug_export_dialog.ui", null);
			builder.Autoconnect (this);

            gallery_optionmenu = Gtk.ComboBox.NewText();
            album_optionmenu = Gtk.ComboBox.NewText();

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
			Dialog.Close += HandleCloseEvent;

			thumb_scrolledwindow.Add (view);
			view.Show ();
			Dialog.Show ();

			SmugMugAccountManager manager = SmugMugAccountManager.GetInstance ();
			manager.AccountListChanged += PopulateSmugMugOptionMenu;
			PopulateSmugMugOptionMenu (manager, null);

			if (edit_button != null)
				edit_button.Clicked += HandleEditGallery;

			Dialog.Response += HandleResponse;
			connect = true;
			HandleSizeActive (null, null);
			Connect ();

			LoadPreference (SCALE_KEY);
			LoadPreference (SIZE_KEY);
			LoadPreference (BROWSER_KEY);
		}

		private bool scale;
		private int size;
		private bool browser;
		private bool connect = false;

		private long approx_size = 0;
		private long sent_bytes = 0;

		IPhoto [] items;
		int photo_index;
		ThreadProgressDialog progress_dialog;

		List<SmugMugAccount> accounts;
		private SmugMugAccount account;
		private Album album;

		private string dialog_name = "smugmug_export_dialog";
		private GtkBeans.Builder builder;

		// Widgets
		[GtkBeans.Builder.Object] Gtk.Dialog dialog;
		Gtk.ComboBox gallery_optionmenu;
		Gtk.ComboBox album_optionmenu;

#pragma warning disable 649
		[GtkBeans.Builder.Object] Gtk.CheckButton browser_check;
		[GtkBeans.Builder.Object] Gtk.CheckButton scale_check;

		[GtkBeans.Builder.Object] Gtk.SpinButton size_spin;

		[GtkBeans.Builder.Object] Gtk.Button album_button;
		[GtkBeans.Builder.Object] Gtk.Button edit_button;

		[GtkBeans.Builder.Object] Gtk.Button export_button;

		[GtkBeans.Builder.Object] Gtk.ScrolledWindow thumb_scrolledwindow;
#pragma warning restore 649

		System.Threading.Thread command_thread;

		public const string EXPORT_SERVICE = "smugmug/";
		public const string SCALE_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "scale";
		public const string SIZE_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "size";
		public const string BROWSER_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "browser";

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

			if (account != null) {
				album = (Album) account.SmugMug.GetAlbums() [Math.Max (0, album_optionmenu.Active)];
				photo_index = 0;

				Dialog.Destroy ();

				command_thread = new System.Threading.Thread (new System.Threading.ThreadStart (this.Upload));
				command_thread.Name = Mono.Unix.Catalog.GetString ("Uploading Pictures");

				progress_dialog = new ThreadProgressDialog (command_thread, items.Length);
				progress_dialog.Start ();

				// Save these settings for next time
				Preferences.Set (SCALE_KEY, scale);
				Preferences.Set (SIZE_KEY, size);
				Preferences.Set (BROWSER_KEY, browser);
			}
		}

		public void HandleSizeActive (object sender, EventArgs args)
		{
			size_spin.Sensitive = scale_check.Active;
		}

		private void Upload ()
		{
			sent_bytes = 0;
			approx_size = 0;

			System.Uri album_uri = null;

			Log.Debug ("Starting Upload to Smugmug, album " + album.Title + " - " + album.AlbumID);

			FilterSet filters = new FilterSet ();
			filters.Add (new JpegFilter ());

			if (scale)
				filters.Add (new ResizeFilter ((uint)size));

			while (photo_index < items.Length) {
				try {
					IPhoto item = items[photo_index];

					FileInfo file_info;
					Log.Debug ("uploading " + photo_index);

					progress_dialog.Message = string.Format (Catalog.GetString ("Uploading picture \"{0}\" ({1} of {2})"),
										 item.Name, photo_index+1, items.Length);
					progress_dialog.ProgressText = string.Empty;
					progress_dialog.Fraction = ((photo_index) / (double) items.Length);
					photo_index++;

					FilterRequest request = new FilterRequest (item.DefaultVersion.Uri);

					filters.Convert (request);

					file_info = new FileInfo (request.Current.LocalPath);

					if (approx_size == 0) //first image
						approx_size = file_info.Length * items.Length;
					else
						approx_size = sent_bytes * items.Length / (photo_index - 1);

					int image_id = account.SmugMug.Upload (request.Current.LocalPath, album.AlbumID);
					if (App.Instance.Database != null && item is Photo && image_id >= 0)
						App.Instance.Database.Exports.Create ((item as Photo).Id,
									      (item as Photo).DefaultVersionId,
									      ExportStore.SmugMugExportType,
									      account.SmugMug.GetAlbumUrl (image_id).ToString ());

					sent_bytes += file_info.Length;

					if (album_uri == null)
						album_uri = account.SmugMug.GetAlbumUrl (image_id);
				} catch (System.Exception e) {
					progress_dialog.Message = string.Format (Mono.Unix.Catalog.GetString ("Error Uploading To Gallery: {0}"),
										 e.Message);
					progress_dialog.ProgressText = Mono.Unix.Catalog.GetString ("Error");
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
			progress_dialog.ProgressText = Mono.Unix.Catalog.GetString ("Upload Complete");
			progress_dialog.ButtonLabel = Gtk.Stock.Ok;

			if (browser && album_uri != null) {
				GtkBeans.Global.ShowUri (Dialog.Screen, album_uri.ToString ());
			}
		}

		private void PopulateSmugMugOptionMenu (SmugMugAccountManager manager, SmugMugAccount changed_account)
		{
			this.account = changed_account;
			int pos = 0;

			accounts = manager.GetAccounts ();
			if (accounts == null || accounts.Count == 0) {
                gallery_optionmenu.AppendText (Mono.Unix.Catalog.GetString ("(No Gallery)"));

				gallery_optionmenu.Sensitive = false;
				edit_button.Sensitive = false;
			} else {
				int i = 0;
				foreach (SmugMugAccount account in accounts) {
					if (account == changed_account)
						pos = i;

                    gallery_optionmenu.AppendText(account.Username);

					i++;
				}
				gallery_optionmenu.Sensitive = true;
				edit_button.Sensitive = true;
			}

            gallery_optionmenu.Active = pos;
		}

		private void Connect ()
		{
			Connect (null);
		}

		private void Connect (SmugMugAccount selected)
		{
			Connect (selected, null);
		}

		private void Connect (SmugMugAccount selected, string text)
		{
			try {
				if (accounts.Count != 0 && connect) {
					if (selected == null)
						account = (SmugMugAccount) accounts [gallery_optionmenu.Active];
					else
						account = selected;

					if (!account.Connected)
						account.Connect ();

					PopulateAlbumOptionMenu (account.SmugMug);
				}
			} catch (System.Exception) {
				Log.Warning ("Can not connect to SmugMug. Bad username? Password? Network connection?");
				if (selected != null)
					account = selected;

				PopulateAlbumOptionMenu (account.SmugMug);

				album_button.Sensitive = false;

				new SmugMugAccountDialog (this.Dialog, account);
			}
		}

		private void HandleAccountSelected (object sender, System.EventArgs args)
		{
			Connect ();
		}

		public void HandleAlbumAdded (string title) {
			SmugMugAccount account = (SmugMugAccount) accounts [gallery_optionmenu.Active];
			PopulateAlbumOptionMenu (account.SmugMug);

			// make the newly created album selected
			Album[] albums = account.SmugMug.GetAlbums();
			for (int i=0; i < albums.Length; i++) {
				if (((Album)albums[i]).Title == title) {
                    album_optionmenu.Active = 1;
				}
			}
		}

		private void PopulateAlbumOptionMenu (SmugMugApi smugmug)
		{
			Album[] albums = null;
			if (smugmug != null) {
				try {
					albums = smugmug.GetAlbums();
				} catch (Exception) {
					Log.Debug ("Can't get the albums");
					smugmug = null;
				}
			}

			bool disconnected = smugmug == null || !account.Connected || albums == null;

			if (disconnected || albums.Length == 0) {
				string msg = disconnected ? Mono.Unix.Catalog.GetString ("(Not Connected)")
					: Mono.Unix.Catalog.GetString ("(No Albums)");

                album_optionmenu.AppendText(msg);

				export_button.Sensitive = false;
				album_optionmenu.Sensitive = false;
				album_button.Sensitive = false;
			} else {
				foreach (Album album in albums) {
					System.Text.StringBuilder label_builder = new System.Text.StringBuilder ();

					label_builder.Append (album.Title);

                    album_optionmenu.AppendText (label_builder.ToString());
				}

				export_button.Sensitive = items.Length > 0;
				album_optionmenu.Sensitive = true;
				album_button.Sensitive = true;
			}

			album_optionmenu.Active = 0;
		}

		public void HandleAddGallery (object sender, System.EventArgs args)
		{
			new SmugMugAccountDialog (this.Dialog);
		}

		public void HandleEditGallery (object sender, System.EventArgs args)
		{
			new SmugMugAccountDialog (this.Dialog, account);
		}

		public void HandleAddAlbum (object sender, System.EventArgs args)
		{
			if (account == null)
				throw new Exception (Catalog.GetString ("No account selected"));

			new SmugMugAddAlbum (this, account.SmugMug);
		}

		void LoadPreference (string key)
		{
			switch (key) {
			case SCALE_KEY:
				if (scale_check.Active != Preferences.Get<bool> (key)) {
					scale_check.Active = Preferences.Get<bool> (key);
				}
				break;

			case SIZE_KEY:
				size_spin.Value = (double) Preferences.Get<int> (key);
				break;

			case BROWSER_KEY:
				if (browser_check.Active != Preferences.Get<bool> (key))
					browser_check.Active = Preferences.Get<bool> (key);
				break;
			}
		}

		protected void HandleCloseEvent (object sender, System.EventArgs args)
		{
			account.SmugMug.Logout ();
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
