using System;
using System.Net;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Web;
using Mono.Unix;

using FSpot;
using FSpot.Core;
using FSpot.Filters;
using FSpot.Widgets;
using FSpot.Utils;
using FSpot.UI.Dialog;
using FSpot.Extensions;

using Hyena;
using Hyena.Widgets;

namespace FSpot.Exporters.Gallery
{
	public class GalleryExport : IExporter {
		public GalleryExport () { }

		public void Run (IBrowsableCollection selection)
		{
			var builder = new GtkBeans.Builder (null, "gallery_export_dialog.ui", null);
			builder.Autoconnect (this);
			export_dialog = new Gtk.Dialog (builder.GetRawObject ("gallery_export_dialog"));

			album_optionmenu = new Gtk.OptionMenu ();
			(album_button.Parent as Gtk.HBox).PackStart (album_optionmenu);
			(album_button.Parent as Gtk.HBox).ReorderChild (album_optionmenu, 1);
			album_optionmenu.Show ();

			gallery_optionmenu = new Gtk.OptionMenu ();
			(edit_button.Parent as Gtk.HBox).PackStart (gallery_optionmenu);
			(edit_button.Parent as Gtk.HBox).ReorderChild (gallery_optionmenu, 1);
			gallery_optionmenu.Show ();

			this.items = selection.Items;
			Array.Sort<IPhoto> (this.items, new IPhotoComparer.CompareDateName());
			album_button.Sensitive = false;
			var view = new TrayView (selection);
			view.DisplayDates = false;
			view.DisplayTags = false;

			export_dialog.Modal = false;
			export_dialog.TransientFor = null;

			thumb_scrolledwindow.Add (view);
			view.Show ();
			export_dialog.Show ();

			GalleryAccountManager manager = GalleryAccountManager.GetInstance ();
			manager.AccountListChanged += PopulateGalleryOptionMenu;
			PopulateGalleryOptionMenu (manager, null);

			if (edit_button != null)
				edit_button.Clicked += HandleEditGallery;

			export_dialog.Response += HandleResponse;
			connect = true;
			HandleSizeActive (null, null);
			Connect ();

			LoadPreference (SCALE_KEY);
			LoadPreference (SIZE_KEY);
			LoadPreference (BROWSER_KEY);
			LoadPreference (META_KEY);
		}

		public const string EXPORT_SERVICE = "gallery/";
		public const string SCALE_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "scale";
		public const string SIZE_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "size";
		public const string BROWSER_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "browser";
		public const string META_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "meta";
		public const string LIGHTTPD_WORKAROUND_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "lighttpd_workaround";

		private bool scale;
		private int size;
		private bool browser;
		private bool meta;
		private bool connect = false;

		IPhoto[] items;
		int photo_index;
		ThreadProgressDialog progress_dialog;

		ArrayList accounts;
		private GalleryAccount account;
		private Album album;

		// Widgets
		[GtkBeans.Builder.Object] Gtk.Dialog export_dialog;
		Gtk.OptionMenu gallery_optionmenu;
		Gtk.OptionMenu album_optionmenu;

		[GtkBeans.Builder.Object] Gtk.CheckButton browser_check;
		[GtkBeans.Builder.Object] Gtk.CheckButton scale_check;
		[GtkBeans.Builder.Object] Gtk.CheckButton meta_check;

		[GtkBeans.Builder.Object] Gtk.SpinButton size_spin;

		[GtkBeans.Builder.Object] Gtk.Button album_button;
		[GtkBeans.Builder.Object] Gtk.Button edit_button;

		[GtkBeans.Builder.Object] Gtk.Button export_button;

		[GtkBeans.Builder.Object] Gtk.ScrolledWindow thumb_scrolledwindow;

		System.Threading.Thread command_thread;


		private void HandleResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId != Gtk.ResponseType.Ok) {
				export_dialog.Destroy ();
				return;
			}

			if (scale_check != null) {
				scale = scale_check.Active;
				size = size_spin.ValueAsInt;
			} else
				scale = false;

			browser = browser_check.Active;
			meta = meta_check.Active;

			if (account != null) {
				//System.Console.WriteLine ("history = {0}", album_optionmenu.History);
				album = (Album) account.Gallery.Albums [Math.Max (0, album_optionmenu.History)];
				photo_index = 0;

				export_dialog.Destroy ();

				command_thread = new System.Threading.Thread (new System.Threading.ThreadStart (this.Upload));
				command_thread.Name = Catalog.GetString ("Uploading Pictures");

				progress_dialog = new ThreadProgressDialog (command_thread, items.Length);
				progress_dialog.Start ();

				// Save these settings for next time
				Preferences.Set (SCALE_KEY, scale);
				Preferences.Set (SIZE_KEY, size);
				Preferences.Set (BROWSER_KEY, browser);
				Preferences.Set (META_KEY, meta);
			}
		}

		private void HandleProgressChanged (ProgressItem item)
		{
			//System.Console.WriteLine ("Changed value = {0}", item.Value);
			progress_dialog.Fraction = (photo_index - 1.0 + item.Value) / (double) items.Length;
		}

		public void HandleSizeActive (object sender, EventArgs args)
		{
			size_spin.Sensitive = scale_check.Active;
		}


		private void Upload ()
		{
				account.Gallery.Progress = new ProgressItem ();
				account.Gallery.Progress.Changed += HandleProgressChanged;

				Log.Debug ("Starting upload");

				FilterSet filters = new FilterSet ();
				if (account.Version == GalleryVersion.Version1)
					filters.Add (new WhiteListFilter (new string []{".jpg", ".jpeg", ".png", ".gif"}));
				if (scale)
					filters.Add (new ResizeFilter ((uint) size));

				while (photo_index < items.Length) {
					IPhoto item = items [photo_index];

					Log.DebugFormat ("uploading {0}", photo_index);

					progress_dialog.Message = System.String.Format (Catalog.GetString ("Uploading picture \"{0}\""), item.Name);
					progress_dialog.Fraction = photo_index / (double) items.Length;
					photo_index++;

					progress_dialog.ProgressText = System.String.Format (Catalog.GetString ("{0} of {1}"), photo_index, items.Length);


					FilterRequest req = new FilterRequest (item.DefaultVersion.Uri);

					filters.Convert (req);
					try {
						int id = album.Add (item, req.Current.LocalPath);

						if (item != null && item is Photo && App.Instance.Database != null && id != 0) {
							App.Instance.Database.Exports.Create ((item as Photo).Id, (item as Photo).DefaultVersionId,
										      ExportStore.Gallery2ExportType,
										      String.Format("{0}:{1}",album.Gallery.Uri.ToString (), id.ToString ()));
						}
					} catch (System.Exception e) {
						progress_dialog.Message = String.Format (Catalog.GetString ("Error uploading picture \"{0}\" to Gallery: {1}"), item.Name, e.Message);
						progress_dialog.ProgressText = Catalog.GetString ("Error");
						Log.Exception (e);

						if (progress_dialog.PerformRetrySkip ()) {
							photo_index--;
						}
					}
			}

			progress_dialog.Message = Catalog.GetString ("Done Sending Photos");
			progress_dialog.Fraction = 1.0;
			progress_dialog.ProgressText = Catalog.GetString ("Upload Complete");
			progress_dialog.ButtonLabel = Gtk.Stock.Ok;

			if (browser) {
				GtkBeans.Global.ShowUri (export_dialog.Screen, album.GetUrl());
			}
		}

		private void PopulateGalleryOptionMenu (GalleryAccountManager manager, GalleryAccount changed_account)
		{
			Gtk.Menu menu = new Gtk.Menu ();
			this.account = changed_account;
			int pos = -1;

			accounts = manager.GetAccounts ();
			if (accounts == null || accounts.Count == 0) {
				Gtk.MenuItem item = new Gtk.MenuItem (Catalog.GetString ("(No Gallery)"));
				menu.Append (item);
				gallery_optionmenu.Sensitive = false;
				edit_button.Sensitive = false;
			} else {
				int i = 0;
				foreach (GalleryAccount account in accounts) {
					if (account == changed_account)
						pos = i;

					Gtk.MenuItem item = new Gtk.MenuItem (account.Name);
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

		private void Connect (GalleryAccount selected)
		{
			try {
				if (accounts.Count != 0 && connect) {
					if (selected == null)
						account = (GalleryAccount) accounts [gallery_optionmenu.History];
					else
						account = selected;

					if (!account.Connected)
						account.Connect ();

					PopulateAlbumOptionMenu (account.Gallery);
					album_button.Sensitive = true;
				}
			} catch (System.Exception ex) {
				if (selected != null)
					account = selected;

				Log.Exception (ex);
				PopulateAlbumOptionMenu (account.Gallery);
				album_button.Sensitive = false;

				new AccountDialog (export_dialog, account, true);
			}
		}

		private void HandleAccountSelected (object sender, System.EventArgs args)
		{
			Connect ();
		}

		public void HandleAlbumAdded (string title) {
			GalleryAccount account = (GalleryAccount) accounts [gallery_optionmenu.History];
			PopulateAlbumOptionMenu (account.Gallery);

			// make the newly created album selected
			ArrayList albums = account.Gallery.Albums;
			for (int i=0; i < albums.Count; i++) {
				if (((Album)albums[i]).Title == title) {
					album_optionmenu.SetHistory((uint)i);
				}
			}
		}

		private void PopulateAlbumOptionMenu (Gallery gallery)
		{
			System.Collections.ArrayList albums = null;
			if (gallery != null) {
				//gallery.FetchAlbumsPrune ();
				try {
					gallery.FetchAlbums ();
					albums = gallery.Albums;
				} catch (GalleryCommandException e) {
					gallery.PopupException (e, export_dialog);
					return;
				}
			}

			Gtk.Menu menu = new Gtk.Menu ();

			bool disconnected = gallery == null || !account.Connected || albums == null;

			if (disconnected || albums.Count == 0) {
				string msg = disconnected ? Catalog.GetString ("(Not Connected)")
					: Catalog.GetString ("(No Albums)");

				Gtk.MenuItem item = new Gtk.MenuItem (msg);
				menu.Append (item);

				export_button.Sensitive = false;
				album_optionmenu.Sensitive = false;
				album_button.Sensitive = false;

				if (disconnected)
					album_button.Sensitive = false;
			} else {
				foreach (Album album in albums) {
					System.Text.StringBuilder label_builder = new System.Text.StringBuilder ();

					for (int i=0; i < album.Parents.Count; i++) {
						label_builder.Append ("  ");
					}
					label_builder.Append (album.Title);

					Gtk.MenuItem item = new Gtk.MenuItem (label_builder.ToString ());
					((Gtk.Label)item.Child).UseUnderline = false;
					menu.Append (item);

				        AlbumPermission add_permission = album.Perms & AlbumPermission.Add;

					if (add_permission == 0)
						item.Sensitive = false;
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
			new AccountDialog (export_dialog);
		}

		public void HandleEditGallery (object sender, System.EventArgs args)
		{
			new AccountDialog (export_dialog, account, false);
		}

		public void HandleAddAlbum (object sender, System.EventArgs args)
		{
			if (account == null)
				throw new GalleryException (Catalog.GetString ("No account selected"));

			new GalleryAddAlbum (this, account.Gallery);
		}

		void LoadPreference (string key)
		{
			switch (key) {
			case SCALE_KEY:
				if (scale_check.Active != Preferences.Get<bool> (key))
					scale_check.Active = Preferences.Get<bool> (key);
				break;

			case SIZE_KEY:
				size_spin.Value = (double) Preferences.Get<int> (key);
				break;

			case BROWSER_KEY:
				if (browser_check.Active != Preferences.Get<bool> (key))
					browser_check.Active = Preferences.Get<bool> (key);
				break;

			case META_KEY:
				if (meta_check.Active != Preferences.Get<bool> (key))
					meta_check.Active = Preferences.Get<bool> (key);
				break;
			}
		}
	}
}
