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
	
	public class GalleryAdd {
		public GalleryAdd (GalleryExport export) {
			Glade.XML xml = new Glade.XML (null, "f-spot.glade", "gallery_add_dialog", null);
			xml.Autoconnect (this);
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

		private void HandleAdd (object sender, System.EventArgs args)
		{
			GalleryAccount account = new GalleryAccount (name, url, username, password);
			export.AddAccount (account);
		}
		
		private void HandleCancel (object sender, System.EventArgs args)
		{
			Dialog.Destroy ();
		}

		public Gtk.Dialog Dialog {
			get {
				return gallery_add_dialog;
			}
		}

		GalleryExport export;

		private string name;
		private string url;
		private string password;
		private string username;

		// widgets 
		[Glade.Widget] Gtk.Dialog gallery_add_dialog;
		
		[Glade.Widget] Gtk.Entry url_entry;
		[Glade.Widget] Gtk.Entry password_entry;
		[Glade.Widget] Gtk.Entry gallery_entry;
		[Glade.Widget] Gtk.Entry username_entry;

		[Glade.Widget] Gtk.Button add_button;
		[Glade.Widget] Gtk.Button cancel_button;
	}

	
	public class GalleryAddAlbum {
		public GalleryAddAlbum (GalleryRemote.Gallery gallery)
		{
		}

		public Gtk.Dialog Dialog {
			get {
				return dialog;
			}
		}
		
		// widgets
		Gtk.Dialog dialog;
	}

	public class GalleryExport {
		public GalleryExport (Photo [] photos) 
		{
			this.photos = photos;
			
			Glade.XML xml = new Glade.XML (null, "f-spot.glade", "gallery_export_dialog", null);
			xml.Autoconnect (this);
			
			IconView view = new IconView (new PhotoArray (photos));
			view.Show ();

			thumb_scrolledwindow.Add (view);

			LoadAccounts ();
			
			Gtk.ResponseType response = (Gtk.ResponseType) gallery_export_dialog.Run ();

			if (response == Gtk.ResponseType.Cancel)
				return;

			scale = scale_check.Active;
			browser = browser_check.Active;
			meta = meta_check.Active;

			if (gallery != null) { 
				System.Console.WriteLine ("history = {0}", album_optionmenu.History);
				album = (GalleryRemote.Album) gallery.Albums [System.Math.Max (0, album_optionmenu.History)]; 
				photo_index = 0;

				System.Threading.Thread t = new System.Threading.Thread (new System.Threading.ThreadStart (this.Upload));
				t.Name = "Uploading Pictures";
				
				progress_dialog = new FSpot.ThreadProgressDialog (t, photos.Length);
				progress_dialog.Start ();
			}
		}

		private bool scale;
		private bool browser;
		private bool meta;

		Photo [] photos;
		int photo_index;
		FSpot.ThreadProgressDialog progress_dialog;
		
		System.Collections.ArrayList accounts;
		private GalleryRemote.Gallery gallery;
		private GalleryRemote.Album album;

		// Dialogs
		private GalleryAdd gallery_add;
		private GalleryAddAlbum album_add;

		// Widgets
		[Glade.Widget] Gtk.Dialog gallery_export_dialog;
		
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
		
		public Gtk.Dialog Dialog {
			get {
				return gallery_export_dialog;
			}
		}
		
		private void Upload ()
		{
			try {
				System.Console.WriteLine ("Starting upload");
				while (photo_index < photos.Length) {
					Photo photo = photos [photo_index];

					System.Console.WriteLine ("uploading {0}", photo_index);

					progress_dialog.Message = System.String.Format ("Uploading picture \"{0}\"", photo.Name);
					progress_dialog.Fraction = photo_index / (double) photos.Length;
					photo_index++;

					progress_dialog.ProgressText = System.String.Format ("{0} of {1}", photo_index, photos.Length);
					album.Add (photo);
				}

				progress_dialog.Message = ("Done Sending Photos");
				progress_dialog.Fraction = 1.0;
				progress_dialog.ProgressText = "Upload Complete";
			} catch (System.Exception e) {
				progress_dialog.Message = e.ToString ();
				progress_dialog.ProgressText = "Error Uploading To Gallery";
			}
		}
		
		private void LoadAccounts ()
		{
			accounts = new System.Collections.ArrayList ();
			AddAccount (new GalleryAccount ("People", "http://discord.no-ip.com/people/gallery/gallery_remote2.php", "FSpot", "eddy"));
			AddAccount (new GalleryAccount ("Joe", "http://discord.no-ip.com/people/gallery/gallery_remote2.php", "billy", "batman"));

		}

		public void AddAccount (GalleryAccount account)
		{
			accounts.Add (account);
			PopulateGalleryOptionMenu ();
		}

		private void PopulateGalleryOptionMenu ()
		{
			Gtk.Menu menu = new Gtk.Menu ();
			
			if (accounts.Count == 0) {
				Gtk.MenuItem item = new Gtk.MenuItem ("(No Gallery)");
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

		private void HandleAccountSelected (object sender, System.EventArgs args)
		{
			if (accounts.Count != 0) {
				GalleryAccount account = (GalleryAccount) accounts [gallery_optionmenu.History];
				if (!account.Connected)
					account.Connect ();
				
				gallery = account.Gallery;

				PopulateAlbumOptionMenu (gallery);
			}
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
				Gtk.MenuItem item = new Gtk.MenuItem ("(No Albums)");
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
					label_builder.Append (album.Name);

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
			album_add = new GalleryAddAlbum (gallery);
		}

		public void HandleExport (object sender, System.EventArgs args)
		{
		}
		
		public void HandleCancel (object sender, System.EventArgs args)
		{
			gallery_export_dialog.Destroy ();

			if (gallery_add != null) 
				gallery_add.Dialog.Destroy ();
			if (album_add != null)
				album_add.Dialog.Destroy ();
		}

	}
}
