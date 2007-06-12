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

using FSpot.Filters;
using Gnome.Keyring;
using SmugMugNet;

namespace FSpot {
	public class SmugMugAccount {
		private string username;
		private string password;
		private string token;
		private bool connected;
		private SmugMugApi smugmug_proxy;

		public SmugMugAccount (string username, string password) 
		{
			this.username = username;
			this.password = password;
		}

		public SmugMugApi Connect ()
		{
			System.Console.WriteLine ("SmugMug.Connect() {0}", username);
			SmugMugApi proxy = new SmugMugApi (username, password);
			ServicePointManager.CertificatePolicy = new NoCheckCertificatePolicy ();
			proxy.Login ();

			this.smugmug_proxy = proxy;
			return proxy;
		}

		private void MarkChanged()
		{
			smugmug_proxy = null;
			connected = false;
		}

		public bool Connected {
			get {
				return (smugmug_proxy != null);
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

		public SmugMugApi SmugMug {
			get {
				return smugmug_proxy;
			}
		}
	}

	
	public class SmugMugAccountManager 
	{
		private static SmugMugAccountManager instance;
		private const string keyring_item_name = "SmugMug Account";
		ArrayList accounts;

		public delegate void AccountListChangedHandler (SmugMugAccountManager manager, SmugMugAccount changed_account);
		public event AccountListChangedHandler AccountListChanged;
		
		public static SmugMugAccountManager GetInstance ()
		{
			if (instance == null) {
				instance = new SmugMugAccountManager ();
			}

			return instance;
		}

		private SmugMugAccountManager ()
		{
			accounts = new ArrayList ();
			ReadAccounts ();
		}	

		public void MarkChanged ()
		{
			MarkChanged (true, null);
		}

		public void MarkChanged (bool write, SmugMugAccount changed_account)
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

		public void AddAccount (SmugMugAccount account)
		{
			AddAccount (account, true);
		}

		public void AddAccount (SmugMugAccount account, bool write)
		{
			accounts.Add (account);
			MarkChanged (write, account);
		}

		public void RemoveAccount (SmugMugAccount account)
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
			foreach (SmugMugAccount account in accounts) {
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
	
					SmugMugAccount account = new SmugMugAccount(username, password);
					if (account != null)
						AddAccount (account, false);

				}
			} catch (Exception e) {
				Console.Error.WriteLine(e);
			}

			MarkChanged ();
		}
	}
	
	public class SmugMugAccountDialog : GladeDialog {
		public SmugMugAccountDialog (Gtk.Window parent) : this (parent, null) { 
			Dialog.Response += HandleAddResponse;
			add_button.Sensitive = false;
		}
		
		public SmugMugAccountDialog (Gtk.Window parent, SmugMugAccount account) :  base ("smugmug_add_dialog")
		{
			this.Dialog.Modal = false;
			this.Dialog.TransientFor = parent;
			this.Dialog.DefaultResponse = Gtk.ResponseType.Ok;
			
			this.account = account;
	
			password_entry.ActivatesDefault = true;
			username_entry.ActivatesDefault = true;

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
				SmugMugAccount account = new SmugMugAccount (username, password);
				SmugMugAccountManager.GetInstance ().AddAccount (account);
			}
			Dialog.Destroy ();
		}

		protected void HandleEditResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId == Gtk.ResponseType.Ok) {
				account.Username = username;
				account.Password = password;
				SmugMugAccountManager.GetInstance ().MarkChanged (true, account);
			} else if (args.ResponseId == Gtk.ResponseType.Reject) {
				// NOTE we are using Reject to signal the remove action.
				SmugMugAccountManager.GetInstance ().RemoveAccount (account);
			}
			Dialog.Destroy ();				
		}

		private SmugMugAccount account;
		private string password;
		private string username;
		private string token;


		// widgets 
		[Glade.Widget] Gtk.Entry password_entry;
		[Glade.Widget] Gtk.Entry username_entry;

		[Glade.Widget] Gtk.Button add_button;
		[Glade.Widget] Gtk.Button remove_button;
		[Glade.Widget] Gtk.Button cancel_button;

		[Glade.Widget] Gtk.HBox status_area;
		[Glade.Widget] Gtk.HBox locked_area;
	}

	public class SmugMugAddAlbum : GladeDialog {
		//[Glade.Widget] Gtk.OptionMenu album_optionmenu;

		[Glade.Widget] Gtk.Entry title_entry;
		[Glade.Widget] Gtk.CheckButton public_check;
		[Glade.Widget] Gtk.ComboBox category_combo;
		
		[Glade.Widget] Gtk.Button add_button;
		[Glade.Widget] Gtk.Button cancel_button;

		private SmugMugExport export;
		private SmugMugApi smugmug;
		private string description;
		private string title;
		private bool public_album;
		private ListStore category_store;

		public SmugMugAddAlbum (SmugMugExport export, SmugMugApi smugmug) : base ("smugmug_add_album_dialog")
		{
			this.export = export;
			this.smugmug = smugmug;
			
			this.category_store = new ListStore (typeof(int), typeof(string));
			CellRendererText display_cell = new CellRendererText();
			category_combo.PackStart (display_cell, true);
			category_combo.SetCellDataFunc (display_cell, new CellLayoutDataFunc (CategoryDataFunc));
			this.category_combo.Model = category_store;
			PopulateCategoryCombo ();
			
			Dialog.Response += HandleAddResponse;

			title_entry.Changed += HandleChanged;
			HandleChanged (null, null);
		}
		
		private void HandleChanged (object sender, EventArgs args)
		{
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
				smugmug.CreateAlbum (title, CurrentCategoryId, public_check.Active);
				export.HandleAlbumAdded (title);
			}
			Dialog.Destroy ();
		}

		void CategoryDataFunc (CellLayout layout, CellRenderer renderer, TreeModel model, TreeIter iter) 
		{
			string name = (string)model.GetValue (iter, 1);
			(renderer as CellRendererText).Text = name;
		}

		protected void PopulateCategoryCombo ()
		{
			SmugMugNet.Category[] categories = smugmug.GetCategories ();

			foreach (SmugMugNet.Category category in categories) {				
				category_store.AppendValues (category.CategoryID, category.Title);
			}

			category_combo.Active = 0;

			category_combo.ShowAll ();
		}

		protected int CurrentCategoryId
		{
			get {
				TreeIter current;
				category_combo.GetActiveIter (out current);
				return (int)category_combo.Model.GetValue (current, 0);
			}
		}
	}

	
	public class SmugMugExport : GladeDialog, FSpot.Extensions.IExporter{
		public SmugMugExport () : base ("smugmug_export_dialog")
		{
		}
		public void Run (IBrowsableCollection selection)
		{
			this.items = selection.Items;
			album_button.Sensitive = false;
			IconView view = new IconView (selection);
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
			
			rh = new Gtk.ResponseHandler (HandleResponse);
			Dialog.Response += HandleResponse;
			connect = true;
			HandleSizeActive (null, null);
			Connect ();

			scale_check.Toggled += HandleScaleCheckToggled;
			
			LoadPreference (Preferences.EXPORT_SMUGMUG_SCALE);
			LoadPreference (Preferences.EXPORT_SMUGMUG_SIZE);
			LoadPreference (Preferences.EXPORT_SMUGMUG_ROTATE);
			LoadPreference (Preferences.EXPORT_SMUGMUG_BROWSER);
		}
		
		Gtk.ResponseHandler rh;
		
		private bool scale;
		private int size;
		private bool browser;
		private bool rotate;
//		private bool meta;
		private bool connect = false;

		private long approx_size = 0;
		private long sent_bytes = 0;

		IBrowsableItem [] items;
		int photo_index;
		FSpot.ThreadProgressDialog progress_dialog;
		
		ArrayList accounts;
		private SmugMugAccount account;
		private Album album;

		private string xml_path;

		// Dialogs
		private SmugMugAccountDialog gallery_add;
		private SmugMugAddAlbum album_add;

		// Widgets
		[Glade.Widget] Gtk.OptionMenu gallery_optionmenu;
		[Glade.Widget] Gtk.OptionMenu album_optionmenu;
		
		[Glade.Widget] Gtk.Entry width_entry;
		[Glade.Widget] Gtk.Entry height_entry;

		[Glade.Widget] Gtk.Label status_label;

		[Glade.Widget] Gtk.CheckButton browser_check;
		[Glade.Widget] Gtk.CheckButton scale_check;
		[Glade.Widget] Gtk.CheckButton rotate_check;
		
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
				album = (Album) account.SmugMug.GetAlbums() [Math.Max (0, album_optionmenu.History)]; 
				photo_index = 0;
				
				Dialog.Destroy ();

				command_thread = new System.Threading.Thread (new System.Threading.ThreadStart (this.Upload));
				command_thread.Name = Mono.Unix.Catalog.GetString ("Uploading Pictures");
				
				progress_dialog = new FSpot.ThreadProgressDialog (command_thread, items.Length);
				progress_dialog.Start ();

				// Save these settings for next time
				Preferences.Set (Preferences.EXPORT_SMUGMUG_SCALE, scale);
				Preferences.Set (Preferences.EXPORT_SMUGMUG_SIZE, size);
				Preferences.Set (Preferences.EXPORT_SMUGMUG_ROTATE, rotate);
				Preferences.Set (Preferences.EXPORT_SMUGMUG_BROWSER, browser);
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

			System.Console.WriteLine ("Starting Upload to Smugmug, album {0} - {1}", album.Title, album.AlbumID);

			FilterSet filters = new FilterSet ();
			filters.Add (new JpegFilter ());

			if (scale)
				filters.Add (new ResizeFilter ((uint)size));

			if (rotate)
				filters.Add (new OrientationFilter ());

			while (photo_index < items.Length) {
				try {
					IBrowsableItem item = items[photo_index];

					FileInfo file_info;
					Console.WriteLine ("uploading {0}", photo_index);

					progress_dialog.Message = String.Format (Catalog.GetString ("Uploading picture \"{0}\" ({1} of {2})"), 
										 item.Name, photo_index+1, items.Length);
					progress_dialog.ProgressText = string.Empty;
					progress_dialog.Fraction = ((photo_index) / (double) items.Length);
					photo_index++;
					
					FilterRequest request = new FilterRequest (item.DefaultVersionUri);

					filters.Convert (request);

					file_info = new FileInfo (request.Current.LocalPath);

					if (approx_size == 0) //first image
						approx_size = file_info.Length * items.Length;
					else
						approx_size = sent_bytes * items.Length / (photo_index - 1);

					int image_id = account.SmugMug.Upload (request.Current.LocalPath, album.AlbumID);
					if (Core.Database != null && item is Photo)
						Core.Database.Exports.Create ((item as Photo).Id,
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
					System.Console.WriteLine (e);

					if (progress_dialog.PerformRetrySkip ())
					 	photo_index--;
				}
			}
				
			progress_dialog.Message = Catalog.GetString ("Done Sending Photos");
			progress_dialog.Fraction = 1.0;
			progress_dialog.ProgressText = Mono.Unix.Catalog.GetString ("Upload Complete");
			progress_dialog.ButtonLabel = Gtk.Stock.Ok;

			if (browser && album_uri != null) {
				GnomeUtil.UrlShow (null, album_uri.ToString ());
			}
		}

		private void HandleScaleCheckToggled (object o, EventArgs e)
		{
			rotate_check.Sensitive = !scale_check.Active;
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
					album_button.Sensitive = true;
				}
			} catch (System.Exception) {
				System.Console.WriteLine ("Can not connect to SmugMug. Bad username ? password ? network connection ?");
				//System.Console.WriteLine ("{0}",ex);
				if (selected != null)
					account = selected;

				PopulateAlbumOptionMenu (account.SmugMug);

				status_label.Text = String.Empty;
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
					Console.WriteLine("Can't get the albums");
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

				ok_button.Sensitive = false;
				album_optionmenu.Sensitive = false;
				album_button.Sensitive = false;

				if (disconnected) 
					album_button.Sensitive = false;
			} else {
				foreach (Album album in albums) {
					System.Text.StringBuilder label_builder = new System.Text.StringBuilder ();
					
					label_builder.Append (album.Title);

					Gtk.MenuItem item = new Gtk.MenuItem (label_builder.ToString ());
					((Gtk.Label)item.Child).UseUnderline = false;
					menu.Append (item);
				}

				ok_button.Sensitive = items.Length > 0;
				album_optionmenu.Sensitive = true;
				album_button.Sensitive = true;
			}

			menu.ShowAll ();
			album_optionmenu.Menu = menu;
		}
		
		public void HandleAddGallery (object sender, System.EventArgs args)
		{
			gallery_add = new SmugMugAccountDialog (this.Dialog);
		}
		
		public void HandleEditGallery (object sender, System.EventArgs args)
		{
			gallery_add = new SmugMugAccountDialog (this.Dialog, account);
		}

		public void HandleAddAlbum (object sender, System.EventArgs args)
		{
			if (account == null)
				throw new Exception (Catalog.GetString ("No account selected"));
				
			album_add = new SmugMugAddAlbum (this, account.SmugMug);
		}

		void LoadPreference (string key)
		{
			object val = Preferences.Get (key);

			if (val == null)
				return;

			//System.Console.WriteLine ("Setting {0} to {1}", key, val);

			switch (key) {
			case Preferences.EXPORT_SMUGMUG_SCALE:
				if (scale_check.Active != (bool) val) {
					scale_check.Active = (bool) val;
					rotate_check.Sensitive = !(bool) val;
				}
				break;

			case Preferences.EXPORT_SMUGMUG_SIZE:
				size_spin.Value = (double) (int) val;
				break;
			
			case Preferences.EXPORT_SMUGMUG_BROWSER:
				if (browser_check.Active != (bool) val)
					browser_check.Active = (bool) val;
				break;
			
			case Preferences.EXPORT_SMUGMUG_ROTATE:
				if (rotate_check.Active != (bool) val)
					rotate_check.Active = (bool) val;
				break;
			}
		}

		protected void HandleCloseEvent (object sender, System.EventArgs args) 
		{
			account.SmugMug.Logout ();
		}
	}
}
