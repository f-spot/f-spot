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
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Specialized;
using System.Web;
using Mono.Unix;
using Gtk;

using FSpot;
using FSpot.Core;
using FSpot.Filters;
using FSpot.Widgets;
using Hyena;
using FSpot.UI.Dialog;

using Gnome.Keyring;
using SmugMugNet;

namespace FSpot.Exporters.SmugMug {
	public class SmugMugExport : FSpot.Extensions.IExporter {
		public SmugMugExport () {}
		public void Run (IBrowsableCollection selection)
		{
			builder = new GtkBeans.Builder (null, "smugmug_export_dialog.ui", null);
			builder.Autoconnect (this);

            gallery_optionmenu = new Gtk.OptionMenu ();
            album_optionmenu = new Gtk.OptionMenu ();

            (edit_button.Parent as Gtk.HBox).PackStart (gallery_optionmenu);
            (album_button.Parent as Gtk.HBox).PackStart (album_optionmenu);
            (edit_button.Parent as Gtk.HBox).ReorderChild (gallery_optionmenu, 1);
            (album_button.Parent as Gtk.HBox).ReorderChild (album_optionmenu, 1);

            gallery_optionmenu.Show ();
            album_optionmenu.Show ();

			this.items = selection.Items;
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

		ArrayList accounts;
		private SmugMugAccount account;
		private Album album;

		private string dialog_name = "smugmug_export_dialog";
		private GtkBeans.Builder builder;

		// Widgets
		[GtkBeans.Builder.Object] Gtk.Dialog dialog;
		Gtk.OptionMenu gallery_optionmenu;
		Gtk.OptionMenu album_optionmenu;

		[GtkBeans.Builder.Object] Gtk.CheckButton browser_check;
		[GtkBeans.Builder.Object] Gtk.CheckButton scale_check;

		[GtkBeans.Builder.Object] Gtk.SpinButton size_spin;

		[GtkBeans.Builder.Object] Gtk.Button album_button;
		[GtkBeans.Builder.Object] Gtk.Button edit_button;

		[GtkBeans.Builder.Object] Gtk.Button export_button;

		[GtkBeans.Builder.Object] Gtk.ScrolledWindow thumb_scrolledwindow;

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
				album = (Album) account.SmugMug.GetAlbums() [Math.Max (0, album_optionmenu.History)];
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

					progress_dialog.Message = String.Format (Catalog.GetString ("Uploading picture \"{0}\" ({1} of {2})"),
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
					progress_dialog.Message = String.Format (Mono.Unix.Catalog.GetString ("Error Uploading To Gallery: {0}"),
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
			Gtk.Menu menu = new Gtk.Menu ();
			this.account = changed_account;
			int pos = -1;

			accounts = manager.GetAccounts ();
			if (accounts == null || accounts.Count == 0) {
				Gtk.MenuItem item = new Gtk.MenuItem (Mono.Unix.Catalog.GetString ("(No Gallery)"));
				menu.Append (item);
				gallery_optionmenu.Sensitive = false;
				edit_button.Sensitive = false;
			} else {
				int i = 0;
				foreach (SmugMugAccount account in accounts) {
					if (account == changed_account)
						pos = i;

					Gtk.MenuItem item = new Gtk.MenuItem (account.Username);
					menu.Append (item);
					i++;
				}
				gallery_optionmenu.Sensitive = true;
				edit_button.Sensitive = true;
			}

			menu.ShowAll ();
			gallery_optionmenu.Menu = menu;
			gallery_optionmenu.SetHistory ((uint)pos);
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
						account = (SmugMugAccount) accounts [gallery_optionmenu.History];
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
			SmugMugAccount account = (SmugMugAccount) accounts [gallery_optionmenu.History];
			PopulateAlbumOptionMenu (account.SmugMug);

			// make the newly created album selected
			Album[] albums = account.SmugMug.GetAlbums();
			for (int i=0; i < albums.Length; i++) {
				if (((Album)albums[i]).Title == title) {
					album_optionmenu.SetHistory((uint)i);
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

			Gtk.Menu menu = new Gtk.Menu ();

			bool disconnected = smugmug == null || !account.Connected || albums == null;

			if (disconnected || albums.Length == 0) {
				string msg = disconnected ? Mono.Unix.Catalog.GetString ("(Not Connected)")
					: Mono.Unix.Catalog.GetString ("(No Albums)");

				Gtk.MenuItem item = new Gtk.MenuItem (msg);
				menu.Append (item);

				export_button.Sensitive = false;
				album_optionmenu.Sensitive = false;
				album_button.Sensitive = false;
			} else {
				foreach (Album album in albums) {
					System.Text.StringBuilder label_builder = new System.Text.StringBuilder ();

					label_builder.Append (album.Title);

					Gtk.MenuItem item = new Gtk.MenuItem (label_builder.ToString ());
					((Gtk.Label)item.Child).UseUnderline = false;
					menu.Append (item);
				}

				export_button.Sensitive = items.Length > 0;
				album_optionmenu.Sensitive = true;
				album_button.Sensitive = true;
			}

			menu.ShowAll ();
			album_optionmenu.Menu = menu;
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
