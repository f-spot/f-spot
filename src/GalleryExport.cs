namespace FSpot {
	public class GalleryAdd {
		public GalleryAdd (System.Collections.ArrayList list) {
			gallery_list = list;

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

		private HandleAdd (object sender, System.EventArgs args)
		{

			try {
				GalleryRemote.Gallery gallery = new GalleryRemote.Gallery (name, url);
				gallery.Login (username, password);
				gallery_list.Add (gallery);
			} catch (System.Exception e) {
				System.Console.WriteLine (e);
				// FIXME Set an Error Label
				add_button.Sensitive = false;
			}
		}
		
		private HandleCancel (object sender, System.EventArgs args)
		{
			Dialog.Destroy ();
		}

		public Gtk.Dialog Dialog {
			get {
				gallery_add_dialog;
			}
		}

		private System.Collections.ArrayList gallery_list;

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
		public AlbumAdd (GalleryRemote gallery)
		{
		}

		public Gtk.Dialog Dialog {
			get {
				dialog;
			}
		}
		
		// widgets
		Gtk.Dialog dialog;
	}

	public class GalleryExport {
		public GalleryExport (Photo [] photos) 
		{
			Glade.XML xml = new Glade.XML (null, "f-spot.glade", "gallery_export_dialog", null);
			xml.Autoconnect (this);

			export_gallery_dialog.DefaultResponse = ResponseType.Ok;

			LoadGalleryList ();
			PopulateGalleryOptionMenu ();

			ResponseType response = (ResponseType) gallery_export_dialog.Run ();

			scale = scale_check.Toggled;
			broswer = browser_check.Toggled;
			meta = meta_check.Toggled;

			if (gallery) { 
				album = gallery.Albums [album_optionmenu.History]; 
			}
		}
		
		private bool scale;
		private bool browser;
		private bool meta;
		
		System.Collections.ArrayList gallery_list;
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

		public Gtk.Dialog Dialog {
			get {
				return gallery_export_dialog;
			}
		}

		private void LoadGalleryList ()
		{
			gallery_list = new System.Collections.ArrayList ();
		}

		public bool AddGallery (GalleryRemote.Gallery gallery)
		{
			gallery_list.Add (galler
		}

		private void PopulateGalleryOptionMenu ()
		{

			Gtk.Menu menu = new Gtk.Menu ();
			
			if (gallery_list.Count == 0) {
				Gtk.MenuItem item = new Gtk.MenuItem ("(None)");
				gallery_optionmenu.Sensitive = false;
			} else {
				foreach (GalleryRemote.Gallery gallery in gallery_list) {
					Gtk.MenuItem item = new MenuItem (label_builder.ToString ());
					menu.Append (item);					
				}
				gallery_optionmenu.Sensitive = true;
			}
		}

		private void PopulateAlbumOptionMenu (GalleryRemote gallery)
		{
			System.Collections.ArrayList albums = null;
			
			if (gallery != null)
				albums = gallery.Albums;

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

					Gtk.MenuItem item = new MenuItem (label_builder.ToString ());
					menu.Append (item);
				}
				
				album_optionmenu.Sensitive = true;
				album_button.Sensitive = true;
			}

			menu.ShowAll ();
			album_optionmenu.Menu = menu;
		}
		
		public void HandleAddGallery (object sender, System.EventArgs args)
		{
			gallery_add = new GalleryAdd (gallery_list);
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

			if (gallery_add) 
				gallery_add.Dialog.Destroy ();
			if (album_add)
				album_add.Dialog.Destroy ();
		}

	}
}
