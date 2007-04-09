using System;
using System.Net;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Web;
using Mono.Unix;

using FSpot.Filters;

using GalleryRemote;

namespace FSpot {
	public class GalleryAccount {
		public GalleryAccount (string name, string url, string username, string password) : this (name, url, username, password, GalleryVersion.VersionUnknown) {}
		public GalleryAccount (string name, string url, string username, string password, GalleryVersion version)
		{
			this.name = name;
			this.username = username;
			this.password = password;
			this.Url = url;

			if (version != GalleryVersion.VersionUnknown) {
				this.version = version;
			} else {
				this.version = Gallery.DetectGalleryVersion(Url);
			}
		}

		public Gallery Connect ()
		{
			//System.Console.WriteLine ("GalleryAccount.Connect()");
			
			Gallery gal = null;

			if (version == GalleryVersion.VersionUnknown)
				this.version = Gallery.DetectGalleryVersion(Url);

			if (version == GalleryVersion.Version1) {
				gal = new Gallery1 (url, url);
			} else if (version == GalleryVersion.Version2) {
				gal = new Gallery2 (url, url);
			} else {
				throw new Exception ("Cannot connect to a Gallery for which the version is unknown");
			}

			System.Console.WriteLine ("Gallery created: " + gal);

			gal.Login (username, password);

			gallery = gal;
			connected = true;

			return gallery;
		}

		GalleryVersion version;
		public GalleryVersion Version{
			get {
				return version;
			}
		}

		private bool connected;
		public bool Connected {
			get {
				bool retVal = false;
				if(gallery != null) {
					retVal = gallery.IsConnected ();
				}
				if (connected != retVal) {
					System.Console.WriteLine ("Connected and retVal for IsConnected() don't agree");
				}
				return retVal;
			}
		}

		public void MarkChanged ()
		{
			connected = false;
			gallery = null;
		}

		Gallery gallery;
		public Gallery Gallery {
			get {
				return gallery;
			}
		}

		string name;
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		string url;
		public string Url {
			get {
				return url;
			}
			set {
				if (url != value) {
					url = value;
					MarkChanged ();
				}
			}
		}

		string username;
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

      		string password;
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
	}

	
	public class GalleryAccountManager 
	{
		private static GalleryAccountManager instance;
		string xml_path;
		ArrayList accounts;

		public delegate void AccountListChangedHandler (GalleryAccountManager manager, GalleryAccount changed_account);
		public event AccountListChangedHandler AccountListChanged;
		
		public static GalleryAccountManager GetInstance ()
		{
			if (instance == null) {
				instance = new GalleryAccountManager ();
			}

			return instance;
		}

		private GalleryAccountManager ()
		{
			// FIXME this xml file path should be be retrieved from a central location not hard coded there
			this.xml_path = System.IO.Path.Combine (FSpot.Global.BaseDirectory, "Accounts.xml");
			
			accounts = new ArrayList ();
			ReadAccounts ();
		}	

		public void MarkChanged ()
		{
			MarkChanged (true, null);
		}

		public void MarkChanged (bool write, GalleryAccount changed_account)
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

		public void AddAccount (GalleryAccount account)
		{
			AddAccount (account, true);
		}

		public void AddAccount (GalleryAccount account, bool write)
		{
			accounts.Add (account);
			MarkChanged (write, account);
		}

		public void RemoveAccount (GalleryAccount account)
		{
			accounts.Remove (account);
			MarkChanged ();
		}

		public void WriteAccounts ()
		{
			System.Xml.XmlTextWriter writer = new System.Xml.XmlTextWriter (xml_path, System.Text.Encoding.Default);
			
			writer.Formatting = System.Xml.Formatting.Indented;
			writer.Indentation = 2;
			writer.IndentChar = ' ';

			writer.WriteStartDocument (true);
			
			writer.WriteStartElement ("GalleryRemote");
			foreach (GalleryAccount account in accounts) {
				writer.WriteStartElement ("Account");
				writer.WriteElementString ("Name", account.Name);
				
				writer.WriteElementString ("Url", account.Url);
				writer.WriteElementString ("Username", account.Username);
				writer.WriteElementString ("Password", account.Password);
				writer.WriteElementString ("Version", account.Version.ToString());
				writer.WriteEndElement (); //Account
			}
			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Close ();
		}

		private GalleryAccount ParseAccount (System.Xml.XmlNode node) 
		{
			if (node.Name != "Account")
				
				return null;

			string name = null;
			string url = null;
			string username = null;
			string password = null;
			GalleryVersion version = GalleryVersion.VersionUnknown;

			foreach (System.Xml.XmlNode child in node.ChildNodes) {
				if (child.Name == "Name") {
					name = child.ChildNodes [0].Value;
				
				} else if (child.Name == "Url") {
					url = child.ChildNodes [0].Value;
				} else if (child.Name == "Password") {
					password = child.ChildNodes [0].Value;
				} else if (child.Name == "Username") {
					username = child.ChildNodes [0].Value;
				} else if (child.Name == "Version") {
					string versionString = child.ChildNodes [0].Value;
					if (versionString == "Version1") 
						version = GalleryVersion.Version1;
					else if (versionString == "Version2")
						version = GalleryVersion.Version2;
					else
						Console.WriteLine ("Unexpected versions string: " + versionString);
				}
			}
			return new GalleryAccount (name, url, username, password, version);
		}

		private void ReadAccounts ()
		{

			if (! File.Exists (xml_path)) {
				MarkChanged ();
				return;
			}

			try {
				string query = "//GalleryRemote/Account";
				System.Xml.XmlDocument doc = new System.Xml.XmlDocument ();
				
				//System.Console.WriteLine ("xml_path: " + xml_path);
				doc.Load (xml_path);
				System.Xml.XmlNodeList nodes = doc.SelectNodes (query);
				
				//System.Console.WriteLine ("selected {0} nodes match {1}", nodes.Count, query);
				foreach (System.Xml.XmlNode node in nodes) {
					GalleryAccount account = ParseAccount (node);
					if (account != null)
						AddAccount (account, false);
				
				}
			} catch (System.Exception e) {
				// FIXME do something
				System.Console.WriteLine ("Exception loading gallery accounts");
				System.Console.WriteLine (e);
			}

			MarkChanged ();
		}
	}
	
	public class AccountDialog : GladeDialog {
		public AccountDialog (Gtk.Window parent) : this (parent, null, false) { 
			Dialog.Response += HandleAddResponse;
			add_button.Sensitive = false;
		}
		
		public AccountDialog (Gtk.Window parent, GalleryAccount account, bool show_error) :  base ("gallery_add_dialog")
		{
			this.Dialog.Modal = false;
			this.Dialog.TransientFor = parent;
			this.Dialog.DefaultResponse = Gtk.ResponseType.Ok;
			
			this.account = account;

			status_area.Visible = show_error;

			if (account != null) {
				gallery_entry.Text = account.Name;
				url_entry.Text = account.Url;
				password_entry.Text = account.Password;
				username_entry.Text = account.Username;
				add_button.Label = Gtk.Stock.Ok;
				Dialog.Response += HandleEditResponse;
			}

			if (remove_button != null)
				remove_button.Visible = account != null;

			this.Dialog.Show ();

			gallery_entry.Changed += HandleChanged;
			url_entry.Changed += HandleChanged;
			password_entry.Changed += HandleChanged;
			username_entry.Changed += HandleChanged;
			HandleChanged (null, null);
		}

		private void HandleChanged (object sender, System.EventArgs args)
		{
			name = gallery_entry.Text;
			url = url_entry.Text;
			password = password_entry.Text;
			username = username_entry.Text;

			if (name == String.Empty || url == String.Empty || password == String.Empty || username == String.Empty)
				add_button.Sensitive = false;
			else
				add_button.Sensitive = true;

		}
		
		[GLib.ConnectBefore]
		protected void HandleAddResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId == Gtk.ResponseType.Ok) {
				try {
					Uri uri = new Uri (url);
					if (uri.Scheme != Uri.UriSchemeHttp &&
					    uri.Scheme != Uri.UriSchemeHttps)
						throw new System.UriFormatException ();
					
					GalleryAccount created = new GalleryAccount (name, 
										     url, 
										     username,
										     password);
					
					created.Connect ();
					GalleryAccountManager.GetInstance ().AddAccount (created);
					account = created;
				} catch (System.UriFormatException) {
					
					HigMessageDialog md = 
						new HigMessageDialog (Dialog, 
								      Gtk.DialogFlags.Modal |
								      Gtk.DialogFlags.DestroyWithParent,
								      Gtk.MessageType.Error, Gtk.ButtonsType.Ok,
								      Catalog.GetString ("Invalid URL"),
								      Catalog.GetString ("The gallery URL entry does not appear to be a valid URL"));
					md.Run ();
					md.Destroy ();
					return;
				} catch (GalleryRemote.GalleryException e) {
					HigMessageDialog md = 
						new HigMessageDialog (Dialog, 
								      Gtk.DialogFlags.Modal |
								      Gtk.DialogFlags.DestroyWithParent,
								      Gtk.MessageType.Error, Gtk.ButtonsType.Ok,
								      Catalog.GetString ("Error while connecting to Gallery"),
								      String.Format (Catalog.GetString ("The following error was encountered while attempting to log in: {0}"), e.Message));
					if (e.ResponseText != null) {
						System.Console.WriteLine (e.Message);
						System.Console.WriteLine (e.ResponseText);
					}
					md.Run ();
					md.Destroy ();
					return;
				} catch (System.Net.WebException we) {
					HigMessageDialog md = 
						new HigMessageDialog (Dialog, 
								      Gtk.DialogFlags.Modal |
								      Gtk.DialogFlags.DestroyWithParent,
								      Gtk.MessageType.Error, Gtk.ButtonsType.Ok,
								      Catalog.GetString ("Error while connecting to Gallery"),
								      String.Format (Catalog.GetString ("The following error was encountered while attempting to log in: {0}"), we.Message));
					md.Run ();
					md.Destroy ();
					return;
				}
			}
			Dialog.Destroy ();
		}

		protected void HandleEditResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId == Gtk.ResponseType.Ok) {
				account.Name = name;
				account.Url = url;
				account.Username = username;
				account.Password = password;
				GalleryAccountManager.GetInstance ().MarkChanged (true, account);
			} else if (args.ResponseId == Gtk.ResponseType.Reject) {
				// NOTE we are using Reject to signal the remove action.
				GalleryAccountManager.GetInstance ().RemoveAccount (account);
			}
			Dialog.Destroy ();				
		}

		private GalleryAccount account;
		private string name;
		private string url;
		private string password;
		private string username;

		// widgets 
		[Glade.Widget] Gtk.Entry url_entry;
		[Glade.Widget] Gtk.Entry password_entry;
		[Glade.Widget] Gtk.Entry gallery_entry;
		[Glade.Widget] Gtk.Entry username_entry;

		[Glade.Widget] Gtk.Button add_button;
		[Glade.Widget] Gtk.Button remove_button;
		[Glade.Widget] Gtk.Button cancel_button;

		[Glade.Widget] Gtk.HBox status_area;
	}

	public class GalleryAddAlbum : GladeDialog {
		[Glade.Widget] Gtk.OptionMenu album_optionmenu;

		[Glade.Widget] Gtk.Entry name_entry;
		[Glade.Widget] Gtk.Entry description_entry;
		[Glade.Widget] Gtk.Entry title_entry;
		
		[Glade.Widget] Gtk.Button add_button;
		[Glade.Widget] Gtk.Button cancel_button;

		private GalleryExport export;
		private Gallery gallery;
		private string parent;
		private string name;
		private string description;
		private string title;

		public GalleryAddAlbum (GalleryExport export, Gallery gallery) : base ("gallery_add_album_dialog")
		{
			this.export = export;
			this.gallery = gallery;	
			PopulateAlbums ();
			
			Dialog.Response += HandleAddResponse;

			name_entry.Changed += HandleChanged;
			description_entry.Changed += HandleChanged;
			title_entry.Changed += HandleChanged;
			HandleChanged (null, null);
		}
		
		private void PopulateAlbums ()
		{
			Gtk.Menu menu = new Gtk.Menu ();
			if (gallery.Version == GalleryVersion.Version1) {
				Gtk.MenuItem top_item = new Gtk.MenuItem (Catalog.GetString ("(TopLevel)"));
				menu.Append (top_item);
			}
			
			foreach (Album album in gallery.Albums) {
				System.Text.StringBuilder label_builder = new System.Text.StringBuilder ();
				
				for (int i=0; i < album.Parents.Count; i++) {
					label_builder.Append ("  ");
				}
				label_builder.Append (album.Title);
				
				Gtk.MenuItem item = new Gtk.MenuItem (label_builder.ToString ());
				((Gtk.Label)item.Child).UseUnderline = false;
				menu.Append (item);
				
				AlbumPermission create_sub = album.Perms & AlbumPermission.CreateSubAlbum;
				
				if (create_sub == 0)
					item.Sensitive = false;
			}

			album_optionmenu.Sensitive = true;
			menu.ShowAll ();
			album_optionmenu.Menu = menu;
		}
		
		private void HandleChanged (object sender, EventArgs args)
		{
			if (gallery.Version == GalleryVersion.Version1) {
				if (gallery.Albums.Count == 0 || album_optionmenu.History <= 0) {
					parent = String.Empty;
				} else {
					parent = ((Album) gallery.Albums [album_optionmenu.History-1]).Name;
				}
			} else {
				if (gallery.Albums.Count == 0 || album_optionmenu.History < 0) {
					parent = String.Empty;
				} else {
					parent = ((Album) gallery.Albums [album_optionmenu.History]).Name;
				}
			}
			name = name_entry.Text;
			description = description_entry.Text;
			title = title_entry.Text;

			if (name == String.Empty || title == String.Empty)
				add_button.Sensitive = false;
			else
				add_button.Sensitive = true;
		}
		
		[GLib.ConnectBefore]
		protected void HandleAddResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId == Gtk.ResponseType.Ok) {
				gallery.NewAlbum (parent, name, title, description);
				export.HandleAlbumAdded (title);
			}
			Dialog.Destroy ();
		}
	}

	
	public class GalleryExport : GladeDialog, FSpot.Extensions.IExporter {
		public GalleryExport () : base ("gallery_export_dialog")
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

			thumb_scrolledwindow.Add (view);
			view.Show ();
			Dialog.Show ();


			GalleryAccountManager manager = GalleryAccountManager.GetInstance ();
			manager.AccountListChanged += PopulateGalleryOptionMenu;
			PopulateGalleryOptionMenu (manager, null);

			if (edit_button != null)
				edit_button.Clicked += HandleEditGallery;
			
			Dialog.Response += HandleResponse;
			connect = true;
			HandleSizeActive (null, null);
			Connect ();

			LoadPreference (Preferences.EXPORT_GALLERY_SCALE);
			LoadPreference (Preferences.EXPORT_GALLERY_SIZE);
			LoadPreference (Preferences.EXPORT_GALLERY_BROWSER);
			LoadPreference (Preferences.EXPORT_GALLERY_META);
			LoadPreference (Preferences.EXPORT_GALLERY_ROTATE);
		}
		
		private bool scale;
		private bool rotate;
		private int size;
		private bool browser;
		private bool meta;
		private bool connect = false;

		IBrowsableItem[] items;
		int photo_index;
		FSpot.ThreadProgressDialog progress_dialog;
		
		ArrayList accounts;
		private GalleryAccount account;
		private Album album;

		private string xml_path;

		// Widgets
		[Glade.Widget] Gtk.OptionMenu gallery_optionmenu;
		[Glade.Widget] Gtk.OptionMenu album_optionmenu;
		
		[Glade.Widget] Gtk.Entry width_entry;
		[Glade.Widget] Gtk.Entry height_entry;

		[Glade.Widget] Gtk.CheckButton browser_check;
		[Glade.Widget] Gtk.CheckButton scale_check;
		[Glade.Widget] Gtk.CheckButton meta_check;
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
			meta = meta_check.Active;
			rotate = rotate_check.Active;

			if (account != null) { 
				//System.Console.WriteLine ("history = {0}", album_optionmenu.History);
				album = (Album) account.Gallery.Albums [Math.Max (0, album_optionmenu.History)]; 
				photo_index = 0;
				
				Dialog.Destroy ();

				command_thread = new System.Threading.Thread (new System.Threading.ThreadStart (this.Upload));
				command_thread.Name = Catalog.GetString ("Uploading Pictures");
				
				progress_dialog = new FSpot.ThreadProgressDialog (command_thread, items.Length);
				progress_dialog.Start ();

				// Save these settings for next time
				Preferences.Set (Preferences.EXPORT_GALLERY_SCALE, scale);
				Preferences.Set (Preferences.EXPORT_GALLERY_SIZE, size);
				Preferences.Set (Preferences.EXPORT_GALLERY_BROWSER, browser);
				Preferences.Set (Preferences.EXPORT_GALLERY_META, meta);
				Preferences.Set (Preferences.EXPORT_GALLERY_ROTATE, rotate);
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

				System.Console.WriteLine ("Starting upload");
				
				FilterSet filters = new FilterSet ();
				if (account.Version == GalleryVersion.Version1)
					filters.Add (new WhiteListFilter (new string []{".jpg", ".jpeg", ".png", ".gif"}));
				if (scale)
					filters.Add (new ResizeFilter ((uint) size));
				else if (rotate)
					filters.Add (new OrientationFilter ());
				

				while (photo_index < items.Length) {
					IBrowsableItem item = items [photo_index];

					System.Console.WriteLine ("uploading {0}", photo_index);

					progress_dialog.Message = System.String.Format (Catalog.GetString ("Uploading picture \"{0}\""), item.Name);
					progress_dialog.Fraction = photo_index / (double) items.Length;
					photo_index++;

					progress_dialog.ProgressText = System.String.Format (Catalog.GetString ("{0} of {1}"), photo_index, items.Length);
					
					
					Filters.FilterRequest req = new Filters.FilterRequest (item.DefaultVersionUri);

					filters.Convert (req);
				try {
					int id = album.Add (item, req.Current.LocalPath);

					if (item != null && item is Photo && Core.Database != null && id != 0) {
						Core.Database.Exports.Create ((item as Photo).Id, (item as Photo).DefaultVersionId,
									      ExportStore.Gallery2ExportType,
									      String.Format("{0}:{1}",album.Gallery.Uri.ToString (), id.ToString ()));
					}
				} catch (System.Exception e) {
					progress_dialog.Message = String.Format (Catalog.GetString ("Error uploading picture \"{0}\" to Gallery: {1}"), item.Name, e.Message);
					progress_dialog.ProgressText = Catalog.GetString ("Error");

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
				GnomeUtil.UrlShow (null, album.GetUrl());
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

				System.Console.WriteLine ("{0}",ex);
				PopulateAlbumOptionMenu (account.Gallery);
				album_button.Sensitive = false;
				
				new AccountDialog (this.Dialog, account, true);
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
				gallery.FetchAlbums ();
				albums = gallery.Albums;
			}

			Gtk.Menu menu = new Gtk.Menu ();

			bool disconnected = gallery == null || !account.Connected || albums == null;

			if (disconnected || albums.Count == 0) {
				string msg = disconnected ? Catalog.GetString ("(Not Connected)") 
					: Catalog.GetString ("(No Albums)");

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

				ok_button.Sensitive = items.Length > 0;
				album_optionmenu.Sensitive = true;
				album_button.Sensitive = true;
			}

			menu.ShowAll ();
			album_optionmenu.Menu = menu;
		}
		
		public void HandleAddGallery (object sender, System.EventArgs args)
		{
			new AccountDialog (this.Dialog);
		}
		
		public void HandleEditGallery (object sender, System.EventArgs args)
		{
			new AccountDialog (this.Dialog, account, false);
		}

		public void HandleAddAlbum (object sender, System.EventArgs args)
		{
			if (account == null)
				throw new Exception (Catalog.GetString ("No account selected"));
				
			new GalleryAddAlbum (this, account.Gallery);
		}

		void LoadPreference (string key)
		{
			object val = Preferences.Get (key);

			if (val == null)
				return;
			
			//System.Console.WriteLine ("Setting {0} to {1}", key, val);

			switch (key) {
			case Preferences.EXPORT_GALLERY_SCALE:
				if (scale_check.Active != (bool) val)
					scale_check.Active = (bool) val;
				break;

			case Preferences.EXPORT_GALLERY_SIZE:
				size_spin.Value = (double) (int) val;
				break;
			
			case Preferences.EXPORT_GALLERY_BROWSER:
				if (browser_check.Active != (bool) val)
					browser_check.Active = (bool) val;
				break;
			
			case Preferences.EXPORT_GALLERY_META:
				if (meta_check.Active != (bool) val)
					meta_check.Active = (bool) val;
				break;
			case Preferences.EXPORT_GALLERY_ROTATE:
				if (rotate_check.Active != (bool) val)
					rotate_check.Active = (bool) val;
				break;
			}
		}
	}
}
