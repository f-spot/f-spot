using Gtk;
using GtkSharp;
using System;
using System.Text;
using System.Collections;

public class ExportCommand {
	public class Gallery {
		[Glade.Widget]
		private Dialog export_gallery_dialog;
		
		[Glade.Widget]
		private Button ok_button;
		
		[Glade.Widget]
		private Entry gallery_url_entry;
		
		[Glade.Widget]
		private Entry gallery_name_entry;
		
		[Glade.Widget]
		private Entry gallery_password_entry;

		[Glade.Widget]
		private OptionMenu gallery_album_option;

		private GalleryRemote gallery = null;

		private void PopulateAlbumOptionMenu ()
		{
			ArrayList albums = null;
			
			if (gallery != null)
				albums = gallery.Albums;

			Menu menu = new Menu ();
			
			if (albums == null || albums.Count == 0) {
				MenuItem item = new MenuItem ("(No Albums)");
				menu.Append (item);

				gallery_album_option.Sensitive = false;
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
				
				gallery_album_option.Sensitive = true;
			}

			menu.ShowAll ();
			gallery_album_option.Menu = menu;
				
		}

		private void Update () {
			string password = gallery_password_entry.Text;
			string url = gallery_url_entry.Text;
			string name = gallery_name_entry.Text;

			gallery_url_entry.Sensitive = false;
			gallery_password_entry.Sensitive = false;
			gallery_name_entry.Sensitive = false;

			if (url != null && url != "" 
			    && name != null && name != ""
			    && password != null && password != "") {
				try {
					gallery = new GalleryRemote (gallery_url_entry.Text);
					gallery.Login (gallery_name_entry.Text, gallery_password_entry.Text);
					gallery.FetchAlbums ();
				} catch (Exception ex) {
					// FIXME real error dialog
					Console.WriteLine ("Error: {0}", ex);
				}
			}
			PopulateAlbumOptionMenu ();

			gallery_url_entry.Sensitive = true;
			gallery_password_entry.Sensitive = true;
			gallery_name_entry.Sensitive = true;
		}
		
		private void HandleActivateCommand (object sender, EventArgs args)
		{
			Update ();
		}

		public bool Execute (Photo []photos) 
		{
			bool success = false;
			
			if (photos == null)
				return success;

			Glade.XML xml = new Glade.XML (null, "f-spot.glade", "export_gallery_dialog", null);
			xml.Autoconnect (this);

			export_gallery_dialog.DefaultResponse = (int) ResponseType.Ok;

			Update ();

			ResponseType response = (ResponseType)export_gallery_dialog.Run ();
			
			if (response == ResponseType.Ok) {
				Album album = null;
				try {
					if (gallery.Albums.Count != 0) {
						album = gallery.Albums[gallery_album_option.History] as Album;
						
						Console.WriteLine ("album = {0}", album.Name);
						
						foreach (Photo photo in photos) {
							album.Add (photo);
						}
						success = true;
					}

				}
				catch (Exception ex) {
					// FIXME this is a lame way to handle errors
					Console.WriteLine ("error {0}", ex);
				}
			}					
			
			export_gallery_dialog.Destroy ();
			return success;
		}
	}
}	
	
