using Gtk;
using GtkSharp;
using System;
using System.Text;
using System.Collections;
using System.Threading;

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

		private GalleryRemote.Gallery gallery = null;
		private GalleryRemote.Album current_album = null;
		private Photo [] current_photos;
		
		private FSpot.ThreadProgressDialog dialog;
		
		string password;
		string url; 
		string name;
		
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
				foreach (GalleryRemote.Album album in albums) {
					StringBuilder label_builder = new StringBuilder ();
					
					for (GalleryRemote.Album parent = album.Parent ();
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
			password = gallery_password_entry.Text;
			url = gallery_url_entry.Text;
			name = gallery_name_entry.Text;

			gallery_url_entry.Sensitive = false;
			gallery_password_entry.Sensitive = false;
			gallery_name_entry.Sensitive = false;

			LoadGallery ();
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

			export_gallery_dialog.DefaultResponse = ResponseType.Ok;

			Update ();

			ResponseType response = (ResponseType)export_gallery_dialog.Run ();
			
			if (response == ResponseType.Ok) {
				try {
					if (gallery.Albums.Count != 0) {
						current_album = gallery.Albums[gallery_album_option.History] as GalleryRemote.Album;
						current_photos = photos;

						Console.WriteLine ("album = {0}", current_album.Name);
						

						Thread t = new Thread (new ThreadStart (this.SendPhotos));
						t.Name = "Uploading Pictures";
						dialog = new FSpot.ThreadProgressDialog (t, photos.Length);
						dialog.Start ();
			
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

		private void SendPhotos () {
			Console.WriteLine ("Sending {0} photos", current_photos.Length);

			try {
				int i = 0;
				foreach (Photo photo in current_photos) {
					dialog.Message = String.Format ("Uploading picture \"{0}\"", photo.Name);
					dialog.Fraction = i / (double) current_photos.Length;
					dialog.ProgressText = String.Format ("{0} of {1}", 
									     i + 1, current_photos.Length);
					current_album.Add (photo);
					i++;
				}
				
				dialog.Message = ("Done Sending Photos");
				dialog.Fraction = 1.0;
				dialog.ProgressText = "Upload Complete";
			} catch (Exception e) {
				dialog.Message = e.ToString ();
				dialog.ProgressText = "Error Uploading To Gallery";
			}

		}

		private void LoadGallery () {			
			if (url != null && url != "" 
			    && name != null && name != ""
			    && password != null && password != "") {
				try {
					if (!url.EndsWith ("/gallery_remote2.php"))
					    url = url + "/gallery_remote2.php";
					
					gallery = new GalleryRemote.Gallery (url);
					gallery.Login (name, password);
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
	}
}      
	
