namespace FSpot {
	public class VfsExport {
		IPhotoCollection selection;
		[Glade.Widget] Gtk.Dialog vfs_export_dialog;

		[Glade.Widget] Gtk.ScrolledWindow thumb_scrolledwindow;
		[Glade.Widget] Gtk.Entry uri_entry;

		[Glade.Widget] Gtk.CheckButton meta_check;
		[Glade.Widget] Gtk.CheckButton scale_check;
		[Glade.Widget] Gtk.CheckButton open_check;

		[Glade.Widget] Gtk.Entry width_entry;
		[Glade.Widget] Gtk.Entry height_entry;

		Gnome.Vfs.Uri dest;
		
		int photo_index;
		bool open;

		FSpot.ThreadProgressDialog progress_dialog;
		System.Threading.Thread command_thread;
		
		public VfsExport (IPhotoCollection selection)
		{
			Gnome.Vfs.ModuleCallbackFullAuthentication auth = new Gnome.Vfs.ModuleCallbackFullAuthentication ();
			auth.Callback += new Gnome.Vfs.ModuleCallbackHandler (HandleAuth);
			auth.SetDefault ();
			auth.Push ();
			
			Gnome.Vfs.ModuleCallbackAuthentication mauth = new Gnome.Vfs.ModuleCallbackAuthentication ();
			mauth.Callback += new Gnome.Vfs.ModuleCallbackHandler (HandleAuth);
			mauth.SetDefault ();
			mauth.Push ();
			
			Gnome.Vfs.ModuleCallbackSaveAuthentication sauth = new Gnome.Vfs.ModuleCallbackSaveAuthentication ();
			sauth.Callback += new Gnome.Vfs.ModuleCallbackHandler (HandleAuth);
			sauth.SetDefault ();
			sauth.Push ();
			
			Gnome.Vfs.ModuleCallbackStatusMessage msg = new Gnome.Vfs.ModuleCallbackStatusMessage ();
			msg.Callback += new Gnome.Vfs.ModuleCallbackHandler (HandleMsg);
			msg.SetDefault ();
			msg.Push ();
			
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
			try {
				Dialog.Destroy ();
				Gnome.Vfs.Result result = Gnome.Vfs.Result.Ok;

				foreach (Photo photo in selection.Photos) {
					Gnome.Vfs.Uri source = new Gnome.Vfs.Uri (photo.DefaultVersionUri.ToString ());
					Gnome.Vfs.Uri target = dest.Clone ();
					target = target.AppendFileName (source.ExtractShortName ());
					Gnome.Vfs.XferProgressCallback cb = new Gnome.Vfs.XferProgressCallback (Progress);
					
					//System.Console.WriteLine ("Xfering {0} to {1}", source.ToString (), target.ToString ());
					
					progress_dialog.Message = System.String.Format (Mono.Posix.Catalog.GetString ("Transferring picture \"{0}\""), photo.Name);
					progress_dialog.Fraction = photo_index / (double) selection.Photos.Length;
					progress_dialog.ProgressText = System.String.Format (Mono.Posix.Catalog.GetString ("{0} of {1}"), 
											     photo_index, selection.Photos.Length);
					result = Gnome.Vfs.Xfer.XferUri (source, target, 
									 Gnome.Vfs.XferOptions.Default, 
									 Gnome.Vfs.XferErrorMode.Abort, 
									 Gnome.Vfs.XferOverwriteMode.Replace, 
									 cb);
					
					photo_index++;
				}

				if (result == Gnome.Vfs.Result.Ok) {
					progress_dialog.Message = Mono.Posix.Catalog.GetString ("Done Sending Photos");
					progress_dialog.Fraction = 1.0;
					progress_dialog.ProgressText = Mono.Posix.Catalog.GetString ("Transfer Complete");
					progress_dialog.ButtonLabel = Gtk.Stock.Ok;

								} else {
					progress_dialog.ProgressText = result.ToString ();
					progress_dialog.Message = Mono.Posix.Catalog.GetString ("Error While Transferring");
				}

				if (open && photo_index > 0)
					Gnome.Url.Show (dest.ToString ());
			} catch (System.Exception e) {
				progress_dialog.Message = e.ToString ();
				progress_dialog.ProgressText = Mono.Posix.Catalog.GetString ("Error Transferring");
			}
		}
		
		private int Progress (Gnome.Vfs.XferProgressInfo info)
		{
			progress_dialog.ProgressText = info.Phase.ToString ();

			if (info.BytesTotal > 0) {
				progress_dialog.Fraction = info.BytesCopied / (double)info.BytesTotal;
			}
			
			switch (info.Status) {
			case Gnome.Vfs.XferProgressStatus.Vfserror:
				progress_dialog.Message = Mono.Posix.Catalog.GetString ("Error: Error while transfering, Aborting");
				return (int)Gnome.Vfs.XferErrorAction.Abort;
			case Gnome.Vfs.XferProgressStatus.Overwrite:
				progress_dialog.ProgressText = Mono.Posix.Catalog.GetString ("Error: File Already Exists, Aborting");
				return (int)Gnome.Vfs.XferOverwriteAction.Abort;
			default:
				return 1;
			}

		}

		private void HandleMsg (Gnome.Vfs.ModuleCallback cb)
		{
			Gnome.Vfs.ModuleCallbackStatusMessage msg = cb as Gnome.Vfs.ModuleCallbackStatusMessage;
			System.Console.WriteLine ("{0}", msg.Message);
		}

		private void HandleAuth (Gnome.Vfs.ModuleCallback cb)
		{
			Gnome.Vfs.ModuleCallbackFullAuthentication fcb = cb as Gnome.Vfs.ModuleCallbackFullAuthentication;
			System.Console.Write ("Enter your username ({0}): ", fcb.Username);
			string username = System.Console.ReadLine ();
			System.Console.Write ("Enter your password : ");
			string passwd = System.Console.ReadLine ();
			
			if (username.Length > 0)
				fcb.Username = username;
			fcb.Password = passwd;
		}

		private void HandleResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId != Gtk.ResponseType.Ok) {
				Dialog.Destroy ();
				return;
			}

			dest = new Gnome.Vfs.Uri (uri_entry.Text);
			open = open_check.Active;

#if false
			Upload ();
#else 	
			command_thread = new System.Threading.Thread (new System.Threading.ThreadStart (Upload));
			command_thread.Name = Mono.Posix.Catalog.GetString ("Transferring Pictures");
			//command_thread.Start ();
			progress_dialog = new FSpot.ThreadProgressDialog (command_thread, selection.Photos.Length);
			progress_dialog.Start ();
#endif
		}
	}
}
