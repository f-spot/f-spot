namespace FSpot {
	public class VfsExport {
		IPhotoCollection selection;
		[Glade.Widget] Gtk.Dialog vfs_export_dialog;

		[Glade.Widget] Gtk.ScrolledWindow thumb_scrolledwindow;
		[Glade.Widget] Gtk.Entry uri_entry;

		[Glade.Widget] Gtk.CheckButton meta_check;
		[Glade.Widget] Gtk.CheckButton scale_check;

		[Glade.Widget] Gtk.Entry width_entry;
		[Glade.Widget] Gtk.Entry height_entry;

		Gnome.Vfs.Uri dest;
		
		int photo_index;

		FSpot.ThreadProgressDialog progress_dialog;
		System.Threading.Thread command_thread;
		
		public VfsExport (IPhotoCollection selection)
		{
			this.selection = selection;
		
			// FIXME this xml file path should be be retrieved from a central location not hard coded there
			Glade.XML xml = new Glade.XML (null, "f-spot.glade", "vfs_export_dialog", null);
			xml.Autoconnect (this);
			
			IconView view = new IconView (selection);
			view.DisplayDates = false;
			view.DisplayTags = false;

			Dialog.Modal = false;
			Dialog.TransientFor = null;

			thumb_scrolledwindow.Add (view);
			Dialog.ShowAll ();

			//LoadHistory ();

			Dialog.Response += HandleResponse;
		}

		public Gtk.Dialog Dialog {
			get {
				return this.vfs_export_dialog;
			}
		}

		public void Upload ()
		{
			foreach (Photo photo in selection.Photos) {
				Gnome.Vfs.Uri source = new Gnome.Vfs.Uri (photo.DefaultVersionUri.ToString ());
				Gnome.Vfs.Uri target = dest.Clone ();
				target = target.AppendFileName (source.ExtractShortName ());
				Gnome.Vfs.XferProgressCallback cb = new Gnome.Vfs.XferProgressCallback (Progress);

				System.Console.WriteLine ("Xfering {0} to {1}", source.ToString (), target.ToString ());
				
				//progress_dialog.Message = System.String.Format (Mono.Posix.Catalog.GetString ("Uploading picture \"{0}\""), photo.Name);
				//progress_dialog.Fraction = photo_index / (double) selection.Photos.Length;
				//progress_dialog.ProgressText = System.String.Format (Mono.Posix.Catalog.GetString ("{0} of {1}"), 
				//						     photo_index, selection.Photos.Length);
				Gnome.Vfs.Xfer.XferUri (source, target, 
							Gnome.Vfs.XferOptions.Default, 
							Gnome.Vfs.XferErrorMode.Abort, 
							Gnome.Vfs.XferOverwriteMode.Replace, 
							cb);
			
				photo_index++;
			}
			Dialog.Destroy ();
		}

		private int Progress (Gnome.Vfs.XferProgressInfo info)
		{
			//progress_dialog.Fraction = info.BytesCopied / 100;
			System.Console.WriteLine ("Progress: {0} {2} {1}", (info.BytesTotal + 1), info.Status.ToString (), info.BytesCopied);

			switch (info.Status) {
			case Gnome.Vfs.XferProgressStatus.Vfserror:
				System.Console.WriteLine ("Error: Vfs error, Aborting");
				return (int)Gnome.Vfs.XferErrorAction.Abort;
			case Gnome.Vfs.XferProgressStatus.Overwrite:
				System.Console.WriteLine ("Error: file already Exists, Aborting");
				return (int)Gnome.Vfs.XferOverwriteAction.Abort;
			default:
				return 1;
			}

		}

		private void HandleResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId != Gtk.ResponseType.Ok) {
				Dialog.Destroy ();
				return;
			}

			dest = new Gnome.Vfs.Uri (uri_entry.Text);

			//Upload ();
			command_thread = new System.Threading.Thread (new System.Threading.ThreadStart (Upload));
			command_thread.Name = Mono.Posix.Catalog.GetString ("Uploading Pictures");
			command_thread.Start ();

			//progress_dialog = new FSpot.ThreadProgressDialog (command_thread, selection.Photos.Length);
			//progress_dialog.Start ();
		}
	}
}
