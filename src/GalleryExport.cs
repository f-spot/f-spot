namespace FSpot {
	public class GalleryExport {
		public GalleryExport (Photo [] photos) 
		{
			Glade.XML xml = new Glade.XML (null, "f-spot.glade", "gallery_export_dialog", null);
			xml.Autoconnect (this);

			export_gallery_dialog.DefaultResponse = ResponseType.Ok;

			ResponseType response = (ResponseType) gallery_export_dialog.Run ();
			
			
		}
		
		
		private void PopulateGalleryOptionMenu ()
		{
			
		}

		private void PopulateAlbumOptionMenu (GalleryRemote gallery)
		{
			ArrayList albums = null;
			
			if (gallery != null)
				albums = gallery.Albums;

			Menu menu = new Menu ();
			
			if (albums == null || albums.Count == 0) {
				MenuItem item = new MenuItem ("(No Albums)");
				menu.Append (item);

				album_optionmenu.Sensitive = false;
			} else {
				foreach (Album album in albums) {
					StringBuilder label_builder = new StringBuilder ();
					
					for (Album parent = album.Parent ();
					     parent != null;
					     parent = parent.Parent ()) {
						label_builder.Append ("  ");
						Console.WriteLine ("looping");
					}
					label_builder.Append (album.Name);

					MenuItem item = new MenuItem (label_builder.ToString ());
					menu.Append (item);
				}
				
				album_optionmenu.Sensitive = true;
			}

			menu.ShowAll ();
			album_optionmenu.Menu = menu;
		}
		
		public void HandleAddGallery (object sender, System.EventArgs args)
		{
			
		}

		public void HandleAddAlbum (object sender, System.EventArgs args)
		{

		}

		public void HandleExport (object sender, System.EventArgs args)
		{

		}
		
		public void HandleCancel (object sender, System.EventArgs args)
		{

		}

		[Glade.Widget] Gtk.Dialog gallery_export_dialog;

		[Glade.Widget] Gtk.OptionMenu gallery_optionmenu;
		[Glade.Widget] Gtk.OptionMenu album_optionmenu;
		
		[Glade.Widget] Gtk.Entry width_entry;
		[Glade.Widget] Gtk.Entry height_entry;

		[Glade.Widget] Gtk.CheckButton browser_check;
		[Glade.Widget] Gtk.CheckButton scale_check;
		[Glade.Widget] Gtk.CheckButton meta_check;
	}
}
