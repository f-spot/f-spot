namespace FSpot {
	public class FlickrExport {
		IPhotoCollection selection;

		[Glade.Widget] Gtk.Dialog flickr_export_dialog;		
		[Glade.Widget] Gtk.CheckButton scale_check;
		[Glade.Widget] Gtk.CheckButton meta_check;
		[Glade.Widget] Gtk.CheckButton tag_check;
		[Glade.Widget] Gtk.CheckButton open_check;

		[Glade.Widget] Gtk.Entry email_entry;
		[Glade.Widget] Gtk.Entry width_entry;
		[Glade.Widget] Gtk.Entry height_entry;
		[Glade.Widget] Gtk.ScrolledWindow thumb_scrolledwindow;
		
		System.Threading.Thread command_thread;
		ThreadProgressDialog progress_dialog;
		ProgressItem progress_item;
		
		bool open;

		int photo_index;
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
			password_entry.Visibility = false;

			password_dialog.VBox.BorderWidth = 12;
			password_dialog.VBox.Spacing = 6;
			password_dialog.VBox.PackStart (new Gtk.Label (Mono.Posix.Catalog.GetString ("Enter Password for ") + email));
			password_dialog.VBox.PackStart (password_entry);
			password_dialog.AddButton (Gtk.Stock.Ok, Gtk.ResponseType.Ok);
			password_dialog.ShowAll ();
			password_dialog.Run ();
			string password =  password_entry.Text;
			password_dialog.Destroy ();
			return password;
		}

		private bool Login () {
			fr.Progress = null;
			string email = email_entry.Text;
			string password = GetPassword (email);
			return fr.Login (email, password);
		}

		private void HandleProgressChanged (ProgressItem item)
		{
			//System.Console.WriteLine ("Changed value = {0}", item.Value);
			progress_dialog.Fraction = (photo_index - 1.0 + item.Value) / (double) selection.Photos.Length;	
		}
		
		private void Upload () {
			fr.Progress = new ProgressItem ();
			fr.Progress.Changed += HandleProgressChanged;
			System.Collections.ArrayList ids = new System.Collections.ArrayList ();
			try {

				foreach (Photo photo in selection.Photos) {
					progress_dialog.Message = System.String.Format (Mono.Posix.Catalog.GetString ("Uploading picture \"{0}\""), photo.Name);
					progress_dialog.Fraction = photo_index / (double)selection.Photos.Length;
					photo_index++;
					progress_dialog.ProgressText = System.String.Format (Mono.Posix.Catalog.GetString ("{0} of {1}"), photo_index, selection.Photos.Length);
					string id = fr.Upload (photo);
					ids.Add (id);
					progress_dialog.Message = Mono.Posix.Catalog.GetString ("Done Sending Photos");
					progress_dialog.Fraction = 1.0;
					progress_dialog.ProgressText = Mono.Posix.Catalog.GetString ("Upload Complete");
					progress_dialog.ButtonLabel = Gtk.Stock.Ok;
				}
			} catch (System.Exception e) {
				progress_dialog.Message = e.ToString ();
				progress_dialog.ProgressText = Mono.Posix.Catalog.GetString ("Error Uploading To Flickr");
			}

			if (open && ids.Count != 0) {
				string view_url = "http://www.flickr.com/tools/uploader_edit.gne?ids";
				foreach (string id in ids)
					view_url = view_url + "," + id;

				Gnome.Url.Show (view_url);
			}
		}
		
		private void HandleResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId != Gtk.ResponseType.Ok) {
				Dialog.Destroy ();
				return;
			}
			
			fr.ExportTags = tag_check.Active;
			open = open_check.Active;
			
			if (Login ()) {
				command_thread = new  System.Threading.Thread (new System.Threading.ThreadStart (this.Upload));
				command_thread.Name = Mono.Posix.Catalog.GetString ("Uploading Pictures");
				
				Dialog.Destroy ();
				progress_dialog = new FSpot.ThreadProgressDialog (command_thread, selection.Photos.Length);
				progress_dialog.Start ();
			} else {
				Gtk.MessageDialog md = new Gtk.MessageDialog (Dialog, 
									      Gtk.DialogFlags.Modal |
									      Gtk.DialogFlags.DestroyWithParent,
									      Gtk.MessageType.Error, Gtk.ButtonsType.Ok, 
									      Mono.Posix.Catalog.GetString ("Unable to log on"));
				md.Run ();
				md.Destroy ();
				return;
			}
		}

		public Gtk.Dialog Dialog {
			get {
				return flickr_export_dialog;
			}
		}
	}

}
