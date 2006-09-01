/*
 * PicasaWebExport.cs
 *
 * Authors:
 *   Stephane Delcroix <stephane@delcroix.org>
 *
 * Copyright (C) 2006 Stephane Delcroix
 */

using System;
using System.Net;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Web;
using Mono.Unix;

using Gnome.Keyring;

using Mono.Google;
using Mono.Google.Picasa;

namespace FSpot {
	public class GoogleAccount {

		private string username;
		private string password;
		private bool connected;
		private GoogleConnection connection;
		private PicasaWeb picasa;

		public GoogleAccount (string username, string password)
		{
			this.username = username;
			this.password = password;
		}

		public PicasaWeb Connect ()
		{
			System.Console.WriteLine ("GoogleAccount.Connect()");
			GoogleConnection conn = new GoogleConnection (GoogleService.Picasa);
			ServicePointManager.CertificatePolicy = new NoCheckCertificatePolicy ();
			conn.Authenticate(username, password);
			connection = conn;
			PicasaWeb picasa = new PicasaWeb(conn);
			this.picasa = picasa;
			return picasa;
		}

		private void MarkChanged()
		{
			connection = null;
			connected = false;
		}

		public bool Connected {
			get {
				return (connection != null);
			}
		}

		public string Username {
			get {
				return username;
			}
			set {
				if (username != value) {
					username = value;
					MarkChanged ();
				}
			}
		}

		public string Password {
			get {
				return password;
			}
			set {
				if (password != value) {
					password = value;
					MarkChanged ();
				}
			}
		}

		public PicasaWeb Picasa {
			get {
				return picasa;
			}
		}
	}

	
	public class GoogleAccountManager 
	{
		private static GoogleAccountManager instance;
		private const string keyring_item_name = "Google Account";
		ArrayList accounts;

		public delegate void AccountListChangedHandler (GoogleAccountManager manager, GoogleAccount changed_account);
		public event AccountListChangedHandler AccountListChanged;
		
		public static GoogleAccountManager GetInstance ()
		{
			if (instance == null) {
				instance = new GoogleAccountManager ();
			}

			return instance;
		}

		private GoogleAccountManager ()
		{
			accounts = new ArrayList ();
			ReadAccounts ();
		}	

		public void MarkChanged ()
		{
			MarkChanged (true, null);
		}

		public void MarkChanged (bool write, GoogleAccount changed_account)
		{
			if (write)
				WriteAccounts ();

			if (AccountListChanged != null)
				AccountListChanged (this, changed_account);
		}

		public ArrayList GetAccounts ()
		{
			return accounts;
		}

		public void AddAccount (GoogleAccount account)
		{
			AddAccount (account, true);
		}

		public void AddAccount (GoogleAccount account, bool write)
		{
			accounts.Add (account);
			MarkChanged (write, account);
		}

		public void RemoveAccount (GoogleAccount account)
		{
			string keyring = Ring.GetDefaultKeyring();
			Hashtable request_attributes = new Hashtable();
			request_attributes["name"] = keyring_item_name;
			request_attributes["username"] = account.Username;
			try {
				foreach(ItemData result in Ring.Find(ItemType.GenericSecret, request_attributes)) {
					Ring.DeleteItem(keyring, result.ItemID);
				}
			} catch (Exception e) {
				Console.WriteLine(e);
			}
			accounts.Remove (account);
			MarkChanged ();
		}

		public void WriteAccounts ()
		{
			string keyring = Ring.GetDefaultKeyring();
			foreach (GoogleAccount account in accounts) {
				Hashtable update_request_attributes = new Hashtable();
				update_request_attributes["name"] = keyring_item_name;
				update_request_attributes["username"] = account.Username;

				Ring.CreateItem(keyring, ItemType.GenericSecret, keyring_item_name, update_request_attributes, account.Password, true);
			}
		}

		private void ReadAccounts ()
		{

			Hashtable request_attributes = new Hashtable();
			request_attributes["name"] = keyring_item_name;
			try {
				foreach(ItemData result in Ring.Find(ItemType.GenericSecret, request_attributes)) {
					if(!result.Attributes.ContainsKey("name") || !result.Attributes.ContainsKey("username") ||
						(result.Attributes["name"] as string) != keyring_item_name) 
						continue;
					
					string username = (string)result.Attributes["username"];
					string password = result.Secret;
	
					if (username == null || username == String.Empty || password == null || password == String.Empty)
						throw new ApplicationException ("Invalid username/password in keyring");
	
					GoogleAccount account = new GoogleAccount(username, password);
					if (account != null)
						AddAccount (account, false);

				}
			} catch (Exception e) {
				Console.Error.WriteLine(e);
			}

			MarkChanged ();
		}
	}
	
	public class GoogleAccountDialog : GladeDialog {
		public GoogleAccountDialog (Gtk.Window parent) : this (parent, null, false) { 
			Dialog.Response += HandleAddResponse;
			add_button.Sensitive = false;
		}
		
		public GoogleAccountDialog (Gtk.Window parent, GoogleAccount account, bool show_error) :  base ("google_add_dialog")
		{
			this.Dialog.Modal = false;
			this.Dialog.TransientFor = parent;
			this.Dialog.DefaultResponse = Gtk.ResponseType.Ok;
			
			this.account = account;

			status_area.Visible = show_error;

			if (account != null) {
				password_entry.Text = account.Password;
				username_entry.Text = account.Username;
				add_button.Label = Gtk.Stock.Ok;
				Dialog.Response += HandleEditResponse;
			}

			if (remove_button != null)
				remove_button.Visible = account != null;

			this.Dialog.Show ();

			password_entry.Changed += HandleChanged;
			username_entry.Changed += HandleChanged;
			HandleChanged (null, null);
		}

		private void HandleChanged (object sender, System.EventArgs args)
		{
			password = password_entry.Text;
			username = username_entry.Text;

			if (password == "" || username == "")
				add_button.Sensitive = false;
			else
				add_button.Sensitive = true;

		}
		
		[GLib.ConnectBefore]
		protected void HandleAddResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId == Gtk.ResponseType.Ok) {
				GoogleAccount account = new GoogleAccount (username, password);
				GoogleAccountManager.GetInstance ().AddAccount (account);
			}
			Dialog.Destroy ();
		}

		protected void HandleEditResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId == Gtk.ResponseType.Ok) {
				account.Username = username;
				account.Password = password;
				GoogleAccountManager.GetInstance ().MarkChanged (true, account);
			} else if (args.ResponseId == Gtk.ResponseType.Reject) {
				// NOTE we are using Reject to signal the remove action.
				GoogleAccountManager.GetInstance ().RemoveAccount (account);
			}
			Dialog.Destroy ();				
		}

		private GoogleAccount account;
		private string password;
		private string username;

		// widgets 
		[Glade.Widget] Gtk.Entry password_entry;
		[Glade.Widget] Gtk.Entry username_entry;

		[Glade.Widget] Gtk.Button add_button;
		[Glade.Widget] Gtk.Button remove_button;
		[Glade.Widget] Gtk.Button cancel_button;

		[Glade.Widget] Gtk.HBox status_area;
	}

	public class GoogleAddAlbum : GladeDialog {
		[Glade.Widget] Gtk.OptionMenu album_optionmenu;

		[Glade.Widget] Gtk.Entry title_entry;
		[Glade.Widget] Gtk.Entry description_entry;
		[Glade.Widget] Gtk.CheckButton public_check;
		
		[Glade.Widget] Gtk.Button add_button;
		[Glade.Widget] Gtk.Button cancel_button;

		private GoogleExport export;
		private PicasaWeb picasa;
		private string description;
		private string title;
		private bool public_album;

		public GoogleAddAlbum (GoogleExport export, PicasaWeb picasa) : base ("google_add_album_dialog")
		{
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

			if (title == "")
				add_button.Sensitive = false;
			else
				add_button.Sensitive = true;
		}
		
		[GLib.ConnectBefore]
		protected void HandleAddResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId == Gtk.ResponseType.Ok) {
				public_album = public_check.Active;

				if (public_album)
					picasa.CreateAlbum (title, description, AlbumAccess.Public);
				else	
					picasa.CreateAlbum (title, description, AlbumAccess.Private);
				export.HandleAlbumAdded (title);
			}
			Dialog.Destroy ();
		}
	}

	
	public class GoogleExport : GladeDialog {
		public GoogleExport (IBrowsableCollection selection) : base ("google_export_dialog")
		{
			this.photos = (Photo []) selection.Items;
			album_button.Sensitive = false;
			IconView view = new IconView (selection);
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

			if (edit_button != null)
				edit_button.Clicked += HandleEditGallery;
			
			rh = new Gtk.ResponseHandler (HandleResponse);
			Dialog.Response += HandleResponse;
			connect = true;
			HandleSizeActive (null, null);
			Connect ();

			scale_check.Toggled += HandleScaleCheckToggled;
			
			LoadPreference (Preferences.EXPORT_PICASAWEB_SCALE);
			LoadPreference (Preferences.EXPORT_PICASAWEB_SIZE);
			LoadPreference (Preferences.EXPORT_PICASAWEB_ROTATE);
			LoadPreference (Preferences.EXPORT_PICASAWEB_BROWSER);
//			LoadPreference (Preferences.EXPORT_PICASAWEB_META);
		}
		
		Gtk.ResponseHandler rh;
		
		private bool scale;
		private int size;
		private bool browser;
		private bool rotate;
//		private bool meta;
		private bool connect = false;

		private long exported_size = 0;
		private long sent_bytes = 0;

		Photo [] photos;
		int photo_index;
		FSpot.ThreadProgressDialog progress_dialog;
		
		ArrayList accounts;
		private GoogleAccount account;
		private PicasaAlbum album;

		private string xml_path;

		// Dialogs
		private GoogleAccountDialog gallery_add;
		private GoogleAddAlbum album_add;

		// Widgets
		[Glade.Widget] Gtk.OptionMenu gallery_optionmenu;
		[Glade.Widget] Gtk.OptionMenu album_optionmenu;
		
		[Glade.Widget] Gtk.Entry width_entry;
		[Glade.Widget] Gtk.Entry height_entry;

		[Glade.Widget] Gtk.Label status_label;

		[Glade.Widget] Gtk.CheckButton browser_check;
		[Glade.Widget] Gtk.CheckButton scale_check;
		[Glade.Widget] Gtk.CheckButton rotate_check;
//		[Glade.Widget] Gtk.CheckButton meta_check;
		
		[Glade.Widget] Gtk.SpinButton size_spin;

		[Glade.Widget] Gtk.Button album_button;
		[Glade.Widget] Gtk.Button add_button;
		[Glade.Widget] Gtk.Button edit_button;
		
		[Glade.Widget] Gtk.Button ok_button;
		[Glade.Widget] Gtk.Button cancel_button;

		[Glade.Widget] Gtk.ScrolledWindow thumb_scrolledwindow;

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
			rotate = rotate_check.Active;
//			meta = meta_check.Active;

			if (account != null) { 
				//System.Console.WriteLine ("history = {0}", album_optionmenu.History);
				album = (PicasaAlbum) account.Picasa.GetAlbums() [Math.Max (0, album_optionmenu.History)]; 
				photo_index = 0;
				
				Dialog.Destroy ();

				command_thread = new System.Threading.Thread (new System.Threading.ThreadStart (this.Upload));
				command_thread.Name = Mono.Posix.Catalog.GetString ("Uploading Pictures");
				
				progress_dialog = new FSpot.ThreadProgressDialog (command_thread, photos.Length);
				progress_dialog.Start ();

				// Save these settings for next time
				Preferences.Set (Preferences.EXPORT_PICASAWEB_SCALE, scale);
				Preferences.Set (Preferences.EXPORT_PICASAWEB_SIZE, size);
				Preferences.Set (Preferences.EXPORT_PICASAWEB_ROTATE, rotate);
				Preferences.Set (Preferences.EXPORT_PICASAWEB_BROWSER, browser);
//				Preferences.Set (Preferences.EXPORT_GALLERY_META, meta);
			}
		}
		
		public void HandleSizeActive (object sender, EventArgs args)
		{
			size_spin.Sensitive = scale_check.Active;
		}

		private void HandleUploadProgress(object o, UploadProgressEventArgs args)
		{
			if (!scale) {
				progress_dialog.ProgressText = System.String.Format ("{0} of {1}",ToHumanReadableSize(sent_bytes + args.BytesSent), ToHumanReadableSize(exported_size));	
				progress_dialog.Fraction = (sent_bytes + args.BytesSent) / (double)exported_size;

			} else {
				if (exported_size == 0)
					progress_dialog.ProgressText = System.String.Format ("{0} Sent",ToHumanReadableSize(args.BytesSent));	
				else
					progress_dialog.ProgressText = System.String.Format ("{0} of approx. {1}", ToHumanReadableSize(sent_bytes + args.BytesSent), ToHumanReadableSize(exported_size));
				progress_dialog.Fraction = ((photo_index - 1) / (double) photos.Length) + (args.BytesSent / (args.BytesTotal * (double) photos.Length));
			}
		}

		private void Upload ()
		{
			try {
				album.UploadProgress += HandleUploadProgress;

				System.Console.WriteLine ("Starting Upload to Picasa");

				if (!scale)
					foreach (Photo photo in photos) {
						System.IO.FileInfo file_info = new System.IO.FileInfo(photo.GetVersionPath(photo.DefaultVersionId));
						exported_size += file_info.Length;
					}
					

				while (photo_index < photos.Length) {
					Photo photo = photos [photo_index];

					System.IO.FileInfo file_info;
					System.Console.WriteLine ("uploading {0}", photo_index);

					progress_dialog.Message = System.String.Format (Mono.Posix.Catalog.GetString ("Uploading picture \"{0}\" ({1} of {2})"), photo.Name, photo_index+1, photos.Length);
					photo_index++;

					
					string orig = photo.DefaultVersionUri.LocalPath;
					string path = PixbufUtils.Resize (orig, size, true);
					string final = path + System.IO.Path.GetExtension (orig);
					if (scale) {
						orig = photo.DefaultVersionUri.LocalPath;
						path = PixbufUtils.Resize (orig, size, true);
						final = path + System.IO.Path.GetExtension (orig);
						System.IO.File.Move (path, final);
						if (photo_index == 1) {
							file_info = new System.IO.FileInfo(final);
							exported_size = photos.Length * file_info.Length;
							Console.WriteLine ("total size: {0}", exported_size);
						}

						file_info = new System.IO.FileInfo(final);
						sent_bytes += file_info.Length;
						album.UploadPicture (final);
						System.IO.File.Delete (final);
					} else if (rotate){
						orig = photo.DefaultVersionUri.LocalPath;
						path = System.IO.Path.GetTempFileName();
						Exif.ExifData exif_data = new Exif.ExifData(orig);
						PixbufOrientation orientation = PixbufUtils.GetOrientation (exif_data);
						if (orientation == PixbufOrientation.RightTop)
							JpegUtils.Transform (orig, path, JpegUtils.TransformType.Rotate90);
						else if (orientation == PixbufOrientation.LeftBottom)
							JpegUtils.Transform (orig, path, JpegUtils.TransformType.Rotate270);
						else
							File.Copy(orig, path, true);
						final = path + System.IO.Path.GetExtension (orig);
						System.IO.File.Move (path, final);
						album.UploadPicture (final, photo.Description);
						System.IO.File.Delete (final);
					} else {
						album.UploadPicture (photo.DefaultVersionUri.LocalPath, photo.Description);
					}


					if (!scale) {
						file_info = new System.IO.FileInfo(photo.GetVersionPath(photo.DefaultVersionId));
						sent_bytes += file_info.Length;
					}
						
				}

				progress_dialog.Message = Mono.Posix.Catalog.GetString ("Done Sending Photos");
				progress_dialog.Fraction = 1.0;
				progress_dialog.ProgressText = Mono.Posix.Catalog.GetString ("Upload Complete");
				progress_dialog.ButtonLabel = Gtk.Stock.Ok;
			} catch (System.Exception e) {
				progress_dialog.Message = String.Format (Mono.Posix.Catalog.GetString ("Error Uploading To Gallery: {0}"),
									 e.Message);
				progress_dialog.ProgressText = Mono.Posix.Catalog.GetString ("Error");
				System.Console.WriteLine (e);
			}
			
			if (browser) {
				GnomeUtil.UrlShow (null, album.Link);
			}
		}

		private void HandleScaleCheckToggled (object o, EventArgs e)
		{
			rotate_check.Sensitive = !scale_check.Active;
		}
		
		private void PopulateGoogleOptionMenu (GoogleAccountManager manager, GoogleAccount changed_account)
		{
			Gtk.Menu menu = new Gtk.Menu ();
			this.account = changed_account;
			int pos = -1;

			accounts = manager.GetAccounts ();
			if (accounts == null || accounts.Count == 0) {
				Gtk.MenuItem item = new Gtk.MenuItem (Mono.Posix.Catalog.GetString ("(No Gallery)"));
				menu.Append (item);
				gallery_optionmenu.Sensitive = false;
				edit_button.Sensitive = false;
			} else {
				int i = 0;
				foreach (GoogleAccount account in accounts) {
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

		private static string ToHumanReadableSize (long in_size)
		{
			string tmp_str = "";
			float tmp_size = in_size;
			int k = 0;
			string[] size_abr = {"bytes", "kB", "MB", "GB", "TB" };
			
			while (tmp_size > 700) { //it's easier to read 0.9MB than 932kB
				tmp_size = tmp_size / 1024;
				k++;
			}
			
			if (tmp_size < 7)
				tmp_str = tmp_size.ToString ("#.##");
			else if (tmp_size < 70)
				tmp_str = tmp_size.ToString ("##.#");
			else
				tmp_str = tmp_size.ToString ("#,###");
			
			if (k < size_abr.Length)
				return tmp_str + " " + size_abr[k];
			else
				return in_size.ToString();
		}
	
		private void Connect ()
		{
			Connect (null);
		}

		private void Connect (GoogleAccount selected)
		{
			try {
				if (accounts.Count != 0 && connect) {
					if (selected == null)
						account = (GoogleAccount) accounts [gallery_optionmenu.History];
					else
						account = selected;

					if (!account.Connected)
						account.Connect ();
					
					PopulateAlbumOptionMenu (account.Picasa);

					long qu = account.Picasa.QuotaUsed;
					long ql = account.Picasa.QuotaLimit;

					StringBuilder sb = new StringBuilder("<small>");
					sb.Append(Catalog.GetString("Available space :"));
					sb.Append(ToHumanReadableSize(ql - qu));
					sb.Append(" (");
					sb.Append(100 * qu / ql);
					sb.Append("% used out of ");
					sb.Append(ToHumanReadableSize(ql));
					sb.Append(")");
					sb.Append("</small>");

					status_label.Text = sb.ToString();
					status_label.UseMarkup = true;

					album_button.Sensitive = true;
				}
			} catch (System.Exception ex) {
				if (selected != null)
					account = selected;

				System.Console.WriteLine("Can not connect to Picasa. Bad username ? password ? network connection ?");
				//System.Console.WriteLine ("{0}",ex);
				PopulateAlbumOptionMenu (account.Picasa);

				status_label.Text = "";
				album_button.Sensitive = false;
				
				GoogleAccountDialog dialog = new GoogleAccountDialog (this.Dialog, account, true);
				Gtk.ResponseType response = (Gtk.ResponseType) dialog.Dialog.Run ();

				if (response == Gtk.ResponseType.Ok) {
					Connect (account);					
				}
			} 
		}

		private void HandleAccountSelected (object sender, System.EventArgs args)
		{
			Connect ();
		}

		public void HandleAlbumAdded (string title) {
			GoogleAccount account = (GoogleAccount) accounts [gallery_optionmenu.History];
			PopulateAlbumOptionMenu (account.Picasa);
				
			// make the newly created album selected
			PicasaAlbumCollection albums = account.Picasa.GetAlbums();
			for (int i=0; i < albums.Count; i++) {
				if (((PicasaAlbum)albums[i]).Title == title) {
					album_optionmenu.SetHistory((uint)i);
				}
			}
		}

		private void PopulateAlbumOptionMenu (PicasaWeb picasa)
		{
			PicasaAlbumCollection albums = null;
			if (picasa != null) {
				//gallery.FetchAlbumsPrune ();
				albums = picasa.GetAlbums();
			}

			Gtk.Menu menu = new Gtk.Menu ();

			bool disconnected = picasa == null || !account.Connected || albums == null;

			if (disconnected || albums.Count == 0) {
				string msg = disconnected ? Mono.Posix.Catalog.GetString ("(Not Connected)") 
					: Mono.Posix.Catalog.GetString ("(No Albums)");

				Gtk.MenuItem item = new Gtk.MenuItem (msg);
				menu.Append (item);

				ok_button.Sensitive = false;
				album_optionmenu.Sensitive = false;
				album_button.Sensitive = false;

				if (disconnected) 
					album_button.Sensitive = false;
			} else {
				foreach (PicasaAlbum album in albums.AllValues) {
					System.Text.StringBuilder label_builder = new System.Text.StringBuilder ();
					
//					for (int i=0; i < album.Parents.Count; i++) {
//						label_builder.Append ("  ");
//					}
					label_builder.Append (album.Title);

					Gtk.MenuItem item = new Gtk.MenuItem (label_builder.ToString ());
					((Gtk.Label)item.Child).UseUnderline = false;
					menu.Append (item);
			
				        //AlbumPermission add_permission = album.Perms & AlbumPermission.Add;

//					if (add_permission == 0)
//						item.Sensitive = false;
				}

				ok_button.Sensitive = photos.Length > 0;
				album_optionmenu.Sensitive = true;
				album_button.Sensitive = true;
			}

			menu.ShowAll ();
			album_optionmenu.Menu = menu;
		}
		
		public void HandleAddGallery (object sender, System.EventArgs args)
		{
			gallery_add = new GoogleAccountDialog (this.Dialog);
		}
		
		public void HandleEditGallery (object sender, System.EventArgs args)
		{
			gallery_add = new GoogleAccountDialog (this.Dialog, account, false);
		}

		public void HandleAddAlbum (object sender, System.EventArgs args)
		{
			if (account == null)
				throw new Exception (Catalog.GetString ("No account selected"));
				
			album_add = new GoogleAddAlbum (this, account.Picasa);
		}

		void LoadPreference (string key)
		{
			object val = Preferences.Get (key);

			if (val == null)
				return;
			
			//System.Console.WriteLine ("Setting {0} to {1}", key, val);

			switch (key) {
			case Preferences.EXPORT_PICASAWEB_SCALE:
				if (scale_check.Active != (bool) val) {
					scale_check.Active = (bool) val;
					rotate_check.Sensitive = !(bool) val;
				}
				break;

			case Preferences.EXPORT_PICASAWEB_SIZE:
				size_spin.Value = (double) (int) val;
				break;
			
			case Preferences.EXPORT_PICASAWEB_BROWSER:
				if (browser_check.Active != (bool) val)
					browser_check.Active = (bool) val;
				break;
			
			case Preferences.EXPORT_PICASAWEB_ROTATE:
				if (rotate_check.Active != (bool) val)
					rotate_check.Active = (bool) val;
				break;
			
//			case Preferences.EXPORT_GALLERY_META:
//				if (meta_check.Active != (bool) val)
//					meta_check.Active = (bool) val;
//				break;
			}
		}
	}
}
