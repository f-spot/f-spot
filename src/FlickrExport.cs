namespace FSpot {
	public class FlickrExport {
		IPhotoCollection selection;

		[Glade.Widget] Gtk.Dialog flickr_export_dialog;		
		[Glade.Widget] Gtk.CheckButton scale_check;
		[Glade.Widget] Gtk.CheckButton meta_check;
		[Glade.Widget] Gtk.CheckButton tag_check;
		[Glade.Widget] Gtk.Entry email_entry;
		[Glade.Widget] Gtk.Entry width_entry;
		[Glade.Widget] Gtk.Entry height_entry;
		[Glade.Widget] Gtk.ScrolledWindow thumb_scrolledwindow;
		
		System.Threading.Thread command_thread;

		FSpot.ProgressItem progress_item;
		FlickrRemote fr = new FlickrRemote ();

		public FlickrExport (IPhotoCollection selection)
		{
			this.selection = selection;
			
			Glade.XML xml = new Glade.XML (null, "f-spot.glade", "flickr_export_dialog", null);
			xml.Autoconnect (this);

			IconView view = new IconView (selection);
			view.DisplayDates = false;

			Dialog.Modal = false;
			Dialog.TransientFor = null;

			thumb_scrolledwindow.Add (view);

			Dialog.ShowAll ();
			Dialog.Response += HandleResponse;
		}
		
		private string GetPassword (string email) 
		{
			Gtk.Dialog password_dialog = new Gtk.Dialog (Mono.Posix.Catalog.GetString ("Enter Password"), Dialog, Gtk.DialogFlags.Modal);
			
			Gtk.Entry password_entry = new Gtk.Entry ();
			
			password_dialog.VBox.PackStart (new Gtk.Label (Mono.Posix.Catalog.GetString ("Enter Password for ") + email));
			password_dialog.VBox.PackStart (password_entry);
			password_dialog.AddButton (Gtk.Stock.Ok, Gtk.ResponseType.Ok);
			password_dialog.ShowAll ();
			password_dialog.Run ();
			string password =  password_entry.Text;
			password_dialog.Destroy ();
			return password;
		}

		private void Login () {
			string email = email_entry.Text;
			string password = GetPassword (email);
			fr.Login (email, password);
		}
		
		private void Upload () {
			foreach (Photo photo in selection.Photos) {
				fr.Upload (photo);
			}
		}
		
		private void HandleResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId != Gtk.ResponseType.Ok) {
				Dialog.Destroy ();
				return;
			}
			
			fr.ExportTags = tag_check.Active;
			Login ();
			
			command_thread = new  System.Threading.Thread (new System.Threading.ThreadStart (this.Upload));
			command_thread.Name = Mono.Posix.Catalog.GetString ("Uploading Pictures");
			command_thread.Start ();
		}

		public Gtk.Dialog Dialog {
			get {
				return flickr_export_dialog;
			}
		}
	}

}
