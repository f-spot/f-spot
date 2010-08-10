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

namespace FSpot.Exporters.PicasaWeb {
	public class GoogleAccount {

		private string username;
		private string password;
		private string token;
		private string unlock_captcha;
		private GoogleConnection connection;
		private Mono.Google.Picasa.PicasaWeb picasa;

		public GoogleAccount (string username, string password)
		{
			this.username = username;
			this.password = password;
		}

		public GoogleAccount (string username, string password, string token, string unlock_captcha)
		{
			this.username = username;
			this.password = password;
			this.token = token;
			this.unlock_captcha = unlock_captcha;
		}

		public Mono.Google.Picasa.PicasaWeb Connect ()
		{
			Log.Debug ("GoogleAccount.Connect()");
			GoogleConnection conn = new GoogleConnection (GoogleService.Picasa);
			ServicePointManager.CertificatePolicy = new NoCheckCertificatePolicy ();
			if (unlock_captcha == null || token == null)
				conn.Authenticate(username, password);
			else {
				conn.Authenticate(username, password, token, unlock_captcha);
				token = null;
				unlock_captcha = null;
			}
			connection = conn;
			var picasa = new Mono.Google.Picasa.PicasaWeb(conn);
			this.picasa = picasa;
			return picasa;
		}

		private void MarkChanged()
		{
			connection = null;
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

		public string Token {
			get {
				return token;
			}
			set {
				token = value;
			}
		}

		public string UnlockCaptcha {
			get {
				return unlock_captcha;
			}
			set {
				unlock_captcha = value;
			}
		}

		public Mono.Google.Picasa.PicasaWeb Picasa {
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
			string keyring;
			try {
				keyring = Ring.GetDefaultKeyring();
			} catch {
				return;
			}
			Hashtable request_attributes = new Hashtable();
			request_attributes["name"] = keyring_item_name;
			request_attributes["username"] = account.Username;
			try {
				foreach(ItemData result in Ring.Find(ItemType.GenericSecret, request_attributes)) {
					Ring.DeleteItem(keyring, result.ItemID);
				}
			} catch (Exception e) {
				Log.DebugException (e);
			}
			accounts.Remove (account);
			MarkChanged ();
		}

		public void WriteAccounts ()
		{
			string keyring;
			try {
				keyring = Ring.GetDefaultKeyring();
			} catch {
				return;
			}
			foreach (GoogleAccount account in accounts) {
				Hashtable update_request_attributes = new Hashtable();
				update_request_attributes["name"] = keyring_item_name;
				update_request_attributes["username"] = account.Username;

				try {
					Ring.CreateItem(keyring, ItemType.GenericSecret, keyring_item_name, update_request_attributes, account.Password, true);
				} catch {
					continue;
				}
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
				Log.DebugException (e);
			}

			MarkChanged ();
		}
	}

	public class GoogleAccountDialog {
		public GoogleAccountDialog (Gtk.Window parent) : this (parent, null, false, null) {
			Dialog.Response += HandleAddResponse;
			add_button.Sensitive = false;
		}

		public GoogleAccountDialog (Gtk.Window parent, GoogleAccount account, bool show_error, CaptchaException captcha_exception)
		{
			xml = new Glade.XML (null, "PicasaWebExport.glade", dialog_name, "f-spot");
			xml.Autoconnect (this);
			Dialog.Modal = false;
			Dialog.TransientFor = parent;
			Dialog.DefaultResponse = Gtk.ResponseType.Ok;

			this.account = account;

			bool show_captcha = (captcha_exception != null);
			status_area.Visible = show_error;
			locked_area.Visible = show_captcha;
			captcha_label.Visible = show_captcha;
			captcha_entry.Visible = show_captcha;
			captcha_image.Visible = show_captcha;

			password_entry.ActivatesDefault = true;
			username_entry.ActivatesDefault = true;

			if (show_captcha) {
				try {
					using  (var img = ImageFile.Create(new SafeUri(captcha_exception.CaptchaUrl, true))) {
						captcha_image.Pixbuf = img.Load();
						token = captcha_exception.Token;
					}
				} catch (Exception) {}
			}

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

			add_button.Sensitive = !(password == String.Empty || username == String.Empty);
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
				account.Token = token;
				account.UnlockCaptcha = captcha_entry.Text;
				GoogleAccountManager.GetInstance ().MarkChanged (true, account);
			} else if (args.ResponseId == Gtk.ResponseType.Reject) {
				// NOTE we are using Reject to signal the remove action.
				GoogleAccountManager.GetInstance ().RemoveAccount (account);
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

		private GoogleAccount account;
		private string password;
		private string username;
		private string token;

		private Glade.XML xml;
		private string dialog_name = "google_add_dialog";

		// widgets
		[Glade.Widget] Gtk.Dialog dialog;
		[Glade.Widget] Gtk.Entry password_entry;
		[Glade.Widget] Gtk.Entry username_entry;
		[Glade.Widget] Gtk.Entry captcha_entry;

		[Glade.Widget] Gtk.Button add_button;
		[Glade.Widget] Gtk.Button remove_button;
		[Glade.Widget] Gtk.Button cancel_button;

		[Glade.Widget] Gtk.HBox status_area;
		[Glade.Widget] Gtk.HBox locked_area;

		[Glade.Widget] Gtk.Image captcha_image;
		[Glade.Widget] Gtk.Label captcha_label;

	}

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


	public class GoogleExport : FSpot.Extensions.IExporter {
		public GoogleExport ()
		{
		}

		public void Run (IBrowsableCollection selection)
		{
			xml = new Glade.XML (null, "PicasaWebExport.glade", dialog_name, "f-spot");
			xml.Autoconnect (this);

			this.items = selection.Items;
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
//			LoadPreference (Preferences.EXPORT_PICASAWEB_META);
			LoadPreference (TAG_KEY);
		}

		private bool scale;
		private int size;
		private bool browser;
//		private bool meta;
		private bool export_tag;
		private bool connect = false;

		private long approx_size = 0;
		private long sent_bytes = 0;

		IBrowsableItem [] items;
		int photo_index;
		ThreadProgressDialog progress_dialog;

		ArrayList accounts;
		private GoogleAccount account;
		private PicasaAlbum album;
		private PicasaAlbumCollection albums = null;

		private string xml_path;

		private Glade.XML xml;
		private string dialog_name = "google_export_dialog";

		public const string EXPORT_SERVICE = "picasaweb/";
		public const string SCALE_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "scale";
		public const string SIZE_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "size";
		public const string BROWSER_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "browser";
		public const string TAG_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "tag";

		// widgets
		[Glade.Widget] Gtk.Dialog dialog;
		[Glade.Widget] Gtk.OptionMenu gallery_optionmenu;
		[Glade.Widget] Gtk.OptionMenu album_optionmenu;

		[Glade.Widget] Gtk.Entry width_entry;
		[Glade.Widget] Gtk.Entry height_entry;

		[Glade.Widget] Gtk.Label status_label;
		[Glade.Widget] Gtk.Label album_status_label;

		[Glade.Widget] Gtk.CheckButton browser_check;
		[Glade.Widget] Gtk.CheckButton scale_check;
//		[Glade.Widget] Gtk.CheckButton meta_check;
		[Glade.Widget] Gtk.CheckButton tag_check;

		[Glade.Widget] Gtk.SpinButton size_spin;

		[Glade.Widget] Gtk.Button album_button;
		[Glade.Widget] Gtk.Button add_button;
		[Glade.Widget] Gtk.Button edit_button;

		[Glade.Widget] Gtk.Button export_button;
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
//			meta = meta_check.Active;
			export_tag = tag_check.Active;

			if (account != null) {
				album = (PicasaAlbum) account.Picasa.GetAlbums() [Math.Max (0, album_optionmenu.History)];
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

		private void HandleUploadProgress(object o, UploadProgressEventArgs args)
		{
				if (approx_size == 0)
					progress_dialog.ProgressText = System.String.Format (Catalog.GetString ("{0} Sent"), GLib.Format.SizeForDisplay (args.BytesSent));
				else
					progress_dialog.ProgressText = System.String.Format (Catalog.GetString ("{0} of approx. {1}"), GLib.Format.SizeForDisplay (sent_bytes + args.BytesSent), GLib.Format.SizeForDisplay (approx_size));
				progress_dialog.Fraction = ((photo_index - 1) / (double) items.Length) + (args.BytesSent / (args.BytesTotal * (double) items.Length));
		}

		private class DateComparer : IComparer
		{
			public int Compare (object left, object right)
			{
				return DateTime.Compare ((left as IBrowsableItem).Time, (right as IBrowsableItem).Time);
			}
		}

		private void Upload ()
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
					IBrowsableItem item = items[photo_index];

					FileInfo file_info;
					Log.Debug ("Picasa uploading " + photo_index);

					progress_dialog.Message = String.Format (Catalog.GetString ("Uploading picture \"{0}\" ({1} of {2})"),
										 item.Name, photo_index+1, items.Length);
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
						foreach (Tag tag in item.Tags)
							picture.AddTag (tag.Name);
				} catch (System.Threading.ThreadAbortException te) {
					Log.Exception (te);
					System.Threading.Thread.ResetAbort ();
				} catch (System.Exception e) {
					progress_dialog.Message = String.Format (Catalog.GetString ("Error Uploading To Gallery: {0}"),
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

			if (browser) {
				GtkBeans.Global.ShowUri (Dialog.Screen, album.Link);
			}
		}

		private void PopulateGoogleOptionMenu (GoogleAccountManager manager, GoogleAccount changed_account)
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
						account = (GoogleAccount) accounts [gallery_optionmenu.History];
					else
						account = selected;

					if (!account.Connected)
						account.Connect ();

					PopulateAlbumOptionMenu (account.Picasa);

					long qu = account.Picasa.QuotaUsed;
					long ql = account.Picasa.QuotaLimit;

					StringBuilder sb = new StringBuilder("<small>");
					sb.Append(String.Format (Catalog.GetString ("Available space: {0}, {1}% used out of {2}"),
								GLib.Format.SizeForDisplay(ql - qu),
								(100 * qu / ql),
								GLib.Format.SizeForDisplay (ql)));
					sb.Append("</small>");
					status_label.Text = sb.ToString();
					status_label.UseMarkup = true;

					album_button.Sensitive = true;
				}
			} catch (CaptchaException exc){
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

				status_label.Text = String.Empty;
				album_button.Sensitive = false;

				new GoogleAccountDialog (this.Dialog, account, true, null);
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
//			PicasaAlbumCollection albums = account.Picasa.GetAlbums();
			for (int i=0; i < albums.Count; i++) {
				if (((PicasaAlbum)albums[i]).Title == title) {
					album_optionmenu.SetHistory((uint)i);
				}
			}
		}

		private void PopulateAlbumOptionMenu (Mono.Google.Picasa.PicasaWeb picasa)
		{
			if (picasa != null) {
				try {
					albums = picasa.GetAlbums();
				} catch {
					Log.Warning ("Picasa: can't get the albums");
					albums = null;
					picasa = null;
				}
			}

			Gtk.Menu menu = new Gtk.Menu ();

			bool disconnected = picasa == null || !account.Connected || albums == null;

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
				foreach (PicasaAlbum album in albums.AllValues) {
					System.Text.StringBuilder label_builder = new System.Text.StringBuilder ();

					label_builder.Append (album.Title);
					label_builder.Append (" (" + album.PicturesCount + ")");

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

		public void HandleAlbumOptionMenuChanged (object sender, System.EventArgs args)
		{
			if (albums == null || albums.Count == 0)
				return;

			PicasaAlbum a = albums [album_optionmenu.History];
			export_button.Sensitive = a.PicturesRemaining >= items.Length;
			if (album_status_label.Visible = !export_button.Sensitive) {
				StringBuilder sb = new StringBuilder("<small>");
				sb.Append(String.Format (Catalog.GetString ("The selected album has a limit of {0} pictures,\n" +
								"which would be passed with the current selection of {1} images"),
								a.PicturesCount + a.PicturesRemaining, items.Length));
				sb.Append("</small>");
				album_status_label.Text = String.Format (sb.ToString());
				album_status_label.UseMarkup = true;
			} else {
				album_status_label.Text = String.Empty;
			}
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

			case TAG_KEY:
				if (tag_check.Active != Preferences.Get<bool> (key))
					tag_check.Active = Preferences.Get<bool> (key);
				break;
			}
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
