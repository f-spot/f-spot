namespace FSpot {
	public class GalleryAccount {
		public GalleryAccount (string name, string url, string username, string password)
		{
			this.name = name;
			this.username = username;
			this.password = password;
			this.url = url;
		}
		
		public GalleryRemote.Gallery Connect ()
		{
			try {
				gallery = new GalleryRemote.Gallery (url);
				gallery.Login (username, password);
				connected = true;
				return gallery;
			} catch (System.Exception e) {
				// FIXME handle this login exception better
				System.Console.WriteLine (e);
				gallery = null;
				return null;
			}
		}

		private bool connected;
		public bool Connected {
			get {
				return connected;
			}
		}

		GalleryRemote.Gallery gallery;
		public GalleryRemote.Gallery Gallery {
			get {
				return gallery;
			}
		}

		string name;
		public string Name {
			get {
				return name;
			}
		}
		
		string url;
		public string Url {
			get {
				return url;
			}
		}

		string username;
		public string Username {
			get {
				return username;
			}
		}

      		string password;
		public string Password {
			get {
				return password;
			}
		}
	}
	
	public class GalleryAdd : GladeDialog {
		public GalleryAdd (GalleryExport export) : base ("gallery_add_dialog") 
		{
			this.export = export;

			gallery_add_dialog = this.Dialog;
			gallery_add_dialog.Modal = false;
			gallery_add_dialog.TransientFor = export.Dialog;
			gallery_add_dialog.Show ();

			Dialog.Response += HandleResponse;
			gallery_entry.Changed += HandleChanged;
			url_entry.Changed += HandleChanged;
			password_entry.Changed += HandleChanged;
			username_entry.Changed += HandleChanged;
			add_button.Sensitive = false;
		}

		private void HandleChanged (object sender, System.EventArgs args)
		{
			name = gallery_entry.Text;
			url = url_entry.Text;
			password = password_entry.Text;
			username = username_entry.Text;

			if (name == "" || url == "" || password == "" || username == "")
				add_button.Sensitive = false;
			else
				add_button.Sensitive = true;
		}

		protected void HandleResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId == Gtk.ResponseType.Ok) {
				if (!url.EndsWith ("/gallery_remote2.php"))
					url = url + "/gallery_remote2.php";

				GalleryAccount account = new GalleryAccount (name, 
									     url, 
									     username,
									     password);
				export.AddAccount (account);
			}
			Dialog.Destroy ();
		}

		GalleryExport export;

		private string name;
		private string url;
		private string password;
		private string username;

		// widgets 
		Gtk.Dialog gallery_add_dialog;
		
		[Glade.Widget] Gtk.Entry url_entry;
		[Glade.Widget] Gtk.Entry password_entry;
		[Glade.Widget] Gtk.Entry gallery_entry;
		[Glade.Widget] Gtk.Entry username_entry;

		[Glade.Widget] Gtk.Button add_button;
		[Glade.Widget] Gtk.Button cancel_button;
	}

	
	public class GalleryAddAlbum : GladeDialog {
		public GalleryAddAlbum (GalleryRemote.Gallery gallery) : base ("gallery_add_album_dialog")
		{
		
		}
	}

	public class GalleryExport : GladeDialog {
		public GalleryExport (IPhotoCollection selection) : base ("gallery_export_dialog")
		{
			this.photos = selection.Photos;

			// FIXME this xml file path should be be retrieved from a central location not hard coded there
			this.xml_path = System.IO.Path.Combine (FSpot.Global.BaseDirectory, "Accounts.xml");
			
			IconView view = new IconView (selection);
			view.DisplayDates = false;
			view.DisplayTags = false;

			Dialog.Modal = false;
			Dialog.TransientFor = null;

			thumb_scrolledwindow.Add (view);
			Dialog.ShowAll ();

			LoadAccounts ();

			Dialog.Response += HandleResponse;
			connect = true;
			Connect ();
		}

		private bool scale;
		private bool browser;
		private bool meta;
		private bool connect = false;

		Photo [] photos;
		int photo_index;
		FSpot.ThreadProgressDialog progress_dialog;
		
		System.Collections.ArrayList accounts;
		private GalleryAccount account;
		private GalleryRemote.Album album;

		private string xml_path;

		// Dialogs
		private GalleryAdd gallery_add;
		private GalleryAddAlbum album_add;

		// Widgets
		[Glade.Widget] Gtk.OptionMenu gallery_optionmenu;
		[Glade.Widget] Gtk.OptionMenu album_optionmenu;
		
		[Glade.Widget] Gtk.Entry width_entry;
		[Glade.Widget] Gtk.Entry height_entry;

		[Glade.Widget] Gtk.CheckButton browser_check;
		[Glade.Widget] Gtk.CheckButton scale_check;
		[Glade.Widget] Gtk.CheckButton meta_check;

		[Glade.Widget] Gtk.Button album_button;
		[Glade.Widget] Gtk.Button add_button;
		
		[Glade.Widget] Gtk.ScrolledWindow thumb_scrolledwindow;

		System.Threading.Thread command_thread;
		
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

			foreach (System.Xml.XmlNode child in node.ChildNodes) {
				if (child.Name == "Name") {
					name = child.ChildNodes [0].Value;
				} else if (child.Name == "Url") {
					url = child.ChildNodes [0].Value;
				} else if (child.Name == "Password") {
					password = child.ChildNodes [0].Value;
				} else if (child.Name == "Username") {
					username = child.ChildNodes [0].Value;
				}
			}
			return new GalleryAccount (name, url, username, password);
		}

		private void ReadAccounts ()
		{
			try {
				string query = "//GalleryRemote/Account";
				System.Xml.XmlDocument doc = new System.Xml.XmlDocument ();
				doc.Load (xml_path);
				System.Xml.XmlNodeList nodes = doc.SelectNodes (query);
				
				System.Console.WriteLine ("selected {0} nodes match {1}", nodes.Count, query);
				foreach (System.Xml.XmlNode node in nodes) {
					GalleryAccount account = ParseAccount (node);
					if (account != null)
						AddAccount (account, false);
				}
			} catch (System.Exception e) {
				// FIXME do something
			}
		}

		private void HandleResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId != Gtk.ResponseType.Ok) {
				Dialog.Destroy ();
				return;
			}

			scale = scale_check.Active;
			browser = browser_check.Active;
			meta = meta_check.Active;

			if (account != null) { 
				System.Console.WriteLine ("history = {0}", album_optionmenu.History);
				album = (GalleryRemote.Album) account.Gallery.Albums [System.Math.Max (0, album_optionmenu.History)]; 
				photo_index = 0;
				
				Dialog.Destroy ();

				command_thread = new System.Threading.Thread (new System.Threading.ThreadStart (this.Upload));
				command_thread.Name = Mono.Posix.Catalog.GetString ("Uploading Pictures");
				
				progress_dialog = new FSpot.ThreadProgressDialog (command_thread, photos.Length);
				progress_dialog.Start ();
			}
		}
		
		private void HandleProgressChanged (ProgressItem item)
		{
			//System.Console.WriteLine ("Changed value = {0}", item.Value);
			progress_dialog.Fraction = (photo_index - 1.0 + item.Value) / (double) photos.Length;
		}

		private void Upload ()
		{
			try {
				account.Gallery.Progress = new ProgressItem ();
				account.Gallery.Progress.Changed += HandleProgressChanged;

				System.Console.WriteLine ("Starting upload");
				while (photo_index < photos.Length) {
					Photo photo = photos [photo_index];

					System.Console.WriteLine ("uploading {0}", photo_index);

					progress_dialog.Message = System.String.Format (Mono.Posix.Catalog.GetString ("Uploading picture \"{0}\""), photo.Name);
					progress_dialog.Fraction = photo_index / (double) photos.Length;
					photo_index++;

					progress_dialog.ProgressText = System.String.Format (Mono.Posix.Catalog.GetString ("{0} of {1}"), photo_index, photos.Length);
					album.Add (photo);
				}

				progress_dialog.Message = Mono.Posix.Catalog.GetString ("Done Sending Photos");
				progress_dialog.Fraction = 1.0;
				progress_dialog.ProgressText = Mono.Posix.Catalog.GetString ("Upload Complete");
				progress_dialog.ButtonLabel = Gtk.Stock.Ok;
			} catch (System.Exception e) {
				progress_dialog.Message = e.ToString ();
				progress_dialog.ProgressText = Mono.Posix.Catalog.GetString ("Error Uploading To Gallery");
			}
			
			if (browser) {
				Gnome.Url.Show (album.GetUrl());
			}
		}
		
		private void LoadAccounts ()
		{ 
			accounts = new System.Collections.ArrayList ();
			ReadAccounts ();
		}

		public void AddAccount (GalleryAccount account)
		{
			AddAccount (account, true);
		}

		public void AddAccount (GalleryAccount account, bool write)
		{
			accounts.Add (account);
			PopulateGalleryOptionMenu ();
			if (write)
				WriteAccounts ();
		}

		private void PopulateGalleryOptionMenu ()
		{
			Gtk.Menu menu = new Gtk.Menu ();
			
			if (accounts.Count == 0) {
				Gtk.MenuItem item = new Gtk.MenuItem (Mono.Posix.Catalog.GetString ("(No Gallery)"));
				menu.Append (item);
				gallery_optionmenu.Sensitive = false;
			} else {
				foreach (GalleryAccount account in accounts) {
					Gtk.MenuItem item = new Gtk.MenuItem (account.Name);
					menu.Append (item);		
				}
				gallery_optionmenu.Sensitive = true;
			}

			menu.ShowAll ();
			gallery_optionmenu.Menu = menu;
		}

		private void Connect ()
		{
			if (accounts.Count != 0 && connect) {
				account = (GalleryAccount) accounts [gallery_optionmenu.History];
				if (!account.Connected)
					account.Connect ();
				
				if (account.Gallery != null)
					PopulateAlbumOptionMenu (account.Gallery);
			}
		}

		private void HandleAccountSelected (object sender, System.EventArgs args)
		{
			Connect ();
		}

		private void PopulateAlbumOptionMenu (GalleryRemote.Gallery gallery)
		{
			System.Collections.ArrayList albums = null;
			
			if (gallery != null) {
				gallery.FetchAlbums ();
				albums = gallery.Albums;
			}

			Gtk.Menu menu = new Gtk.Menu ();
			
			if (albums == null || albums.Count == 0) {
				Gtk.MenuItem item = new Gtk.MenuItem (Mono.Posix.Catalog.GetString ("(No Albums)"));
				menu.Append (item);

				album_optionmenu.Sensitive = false;
				album_button.Sensitive = false;
			} else {
				foreach (GalleryRemote.Album album in albums) {
					System.Text.StringBuilder label_builder = new System.Text.StringBuilder ();
					
					for (GalleryRemote.Album parent = album.Parent ();
					     parent != null;
					     parent = parent.Parent ()) {
						label_builder.Append ("  ");
						//Console.WriteLine ("looping");
					}
					label_builder.Append (album.Title);

					Gtk.MenuItem item = new Gtk.MenuItem (label_builder.ToString ());
					menu.Append (item);
			
				        GalleryRemote.AlbumPermission add_permission = album.Perms & GalleryRemote.AlbumPermission.Add;

					if (add_permission == 0)
						item.Sensitive = false;
				}
				album_optionmenu.Sensitive = true;
				album_button.Sensitive = true;
			}

			menu.ShowAll ();
			album_optionmenu.Menu = menu;
		}
		
		public void HandleAddGallery (object sender, System.EventArgs args)
		{
			gallery_add = new GalleryAdd (this);
		}

		public void HandleAddAlbum (object sender, System.EventArgs args)
		{
			album_add = new GalleryAddAlbum (account.Gallery);
		}
	}
}
