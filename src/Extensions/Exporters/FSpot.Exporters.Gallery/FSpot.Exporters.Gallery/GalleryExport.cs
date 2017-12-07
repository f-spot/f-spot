//
// GalleryExport.cs
//
// Author:
//   Lorenzo Milesi <maxxer@yetopen.it>
//   Larry Ewing <lewing@novell.com>
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2004-2009 Novell, Inc.
// Copyright (C) 2008 Lorenzo Milesi
// Copyright (C) 2004-2006 Larry Ewing
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

using System;
using System.Collections.Generic;

using Mono.Unix;

using FSpot.Core;
using FSpot.Database;
using FSpot.Filters;
using FSpot.Settings;
using FSpot.Widgets;
using FSpot.UI.Dialog;
using FSpot.Extensions;

using Hyena;
using System.Linq;

namespace FSpot.Exporters.Gallery
{
	public class GalleryExport : IExporter
	{
		public GalleryExport ()
		{
		}

		public void Run (IBrowsableCollection selection)
		{
			var builder = new GtkBeans.Builder (null, "gallery_export_dialog.ui", null);
			builder.Autoconnect (this);
			export_dialog = new Gtk.Dialog (builder.GetRawObject ("gallery_export_dialog"));

			album_optionmenu = new Gtk.ComboBox ();
			(album_button.Parent as Gtk.HBox).PackStart (album_optionmenu);
			(album_button.Parent as Gtk.HBox).ReorderChild (album_optionmenu, 1);
			album_optionmenu.Show ();

			gallery_optionmenu = new Gtk.ComboBox ();
			(edit_button.Parent as Gtk.HBox).PackStart (gallery_optionmenu);
			(edit_button.Parent as Gtk.HBox).ReorderChild (gallery_optionmenu, 1);
			gallery_optionmenu.Show ();

			this.items = selection.Items.ToArray ();
			Array.Sort<IPhoto> (this.items, new IPhotoComparer.CompareDateName ());
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
		List<GalleryAccount> accounts;
		private GalleryAccount account;
		private Album album;

		// Widgets
		[GtkBeans.Builder.Object] Gtk.Dialog export_dialog;
		Gtk.ComboBox gallery_optionmenu;
		Gtk.ComboBox album_optionmenu;

#pragma warning disable 649
		[GtkBeans.Builder.Object] Gtk.CheckButton browser_check;
		[GtkBeans.Builder.Object] Gtk.CheckButton scale_check;
		[GtkBeans.Builder.Object] Gtk.CheckButton meta_check;
		[GtkBeans.Builder.Object] Gtk.SpinButton size_spin;
		[GtkBeans.Builder.Object] Gtk.Button album_button;
		[GtkBeans.Builder.Object] Gtk.Button edit_button;
		[GtkBeans.Builder.Object] Gtk.Button export_button;
		[GtkBeans.Builder.Object] Gtk.ScrolledWindow thumb_scrolledwindow;
#pragma warning restore 649

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
				album = account.Gallery.Albums [Math.Max (0, album_optionmenu.Active)];
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
			progress_dialog.Fraction = (photo_index - 1.0 + item.Value) / (double)items.Length;
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
				filters.Add (new ResizeFilter ((uint)size));

			while (photo_index < items.Length) {
				IPhoto item = items [photo_index];

				Log.DebugFormat ("uploading {0}", photo_index);

				progress_dialog.Message = string.Format (Catalog.GetString ("Uploading picture \"{0}\""), item.Name);
				progress_dialog.Fraction = photo_index / (double)items.Length;
				photo_index++;

				progress_dialog.ProgressText = string.Format (Catalog.GetString ("{0} of {1}"), photo_index, items.Length);


				FilterRequest req = new FilterRequest (item.DefaultVersion.Uri);

				filters.Convert (req);
				try {
					int id = album.Add (item, req.Current.LocalPath);

					if (item != null && item is Photo && App.Instance.Database != null && id != 0)
							App.Instance.Database.Exports.Create ((item as Photo).Id, (item as Photo).DefaultVersionId,
										      ExportStore.Gallery2ExportType,
										      string.Format("{0}:{1}",album.Gallery.Uri, id.ToString ()));
				} catch (Exception e) {
					progress_dialog.Message = string.Format (Catalog.GetString ("Error uploading picture \"{0}\" to Gallery: {1}"), item.Name, e.Message);
					progress_dialog.ProgressText = Catalog.GetString ("Error");
					Log.Exception (e);

					if (progress_dialog.PerformRetrySkip ())
							photo_index--;
				}
			}

			progress_dialog.Message = Catalog.GetString ("Done Sending Photos");
			progress_dialog.Fraction = 1.0;
			progress_dialog.ProgressText = Catalog.GetString ("Upload Complete");
			progress_dialog.ButtonLabel = Gtk.Stock.Ok;

			if (browser)
				GtkBeans.Global.ShowUri (export_dialog.Screen, album.GetUrl());
		}

		private void PopulateGalleryOptionMenu (GalleryAccountManager manager, GalleryAccount changed_account)
		{
			this.account = changed_account;
			int pos = -1;

			accounts = manager.GetAccounts ();
			if (accounts == null || accounts.Count == 0) {
				gallery_optionmenu.AppendText (Catalog.GetString ("(No Gallery)"));

				gallery_optionmenu.Sensitive = false;
				edit_button.Sensitive = false;
			} else {
				int i = 0;
				foreach (GalleryAccount account in accounts) {
					if (account == changed_account)
						pos = i;

					gallery_optionmenu.AppendText (account.Name);
					i++;
				}
				gallery_optionmenu.Sensitive = true;
				edit_button.Sensitive = true;
			}

			gallery_optionmenu.Active = pos;
		}

		private void Connect (GalleryAccount selected = null)
		{
			try {
				if (accounts.Count != 0 && connect) {
					if (selected == null)
					    if (gallery_optionmenu.Active != -1)
						    account = accounts [gallery_optionmenu.Active];
						else
						    account = accounts [0];
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

		public void HandleAlbumAdded (string title)
		{
			GalleryAccount account = accounts [gallery_optionmenu.Active];
			PopulateAlbumOptionMenu (account.Gallery);

			// make the newly created album selected
			List<Album> albums = account.Gallery.Albums;
			for (int i=0; i < albums.Count; i++) {
				if (((Album)albums [i]).Title == title)
					album_optionmenu.Active = i;
			}
		}

		private void PopulateAlbumOptionMenu (Gallery gallery)
		{
			List<Album> albums = null;
			if (gallery != null)
				//gallery.FetchAlbumsPrune ();
				try {
					gallery.FetchAlbums ();
					albums = gallery.Albums;
				} catch (GalleryCommandException e) {
					gallery.PopupException (e, export_dialog);
					return;
				}

			bool disconnected = gallery == null || !account.Connected || albums == null;

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
				foreach (Album album in albums) {
					System.Text.StringBuilder label_builder = new System.Text.StringBuilder ();

					for (int i=0; i < album.Parents.Count; i++) {
						label_builder.Append ("  ");
					}
					label_builder.Append (album.Title);

					album_optionmenu.AppendText (label_builder.ToString ());
				}

				export_button.Sensitive = items.Length > 0;
				album_optionmenu.Sensitive = true;
				album_button.Sensitive = true;
			}
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
				size_spin.Value = (double)Preferences.Get<int> (key);
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
