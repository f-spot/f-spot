using System.Runtime.InteropServices;

namespace FSpot {
	public class CDExport : GladeDialog {
		IPhotoCollection selection;

		[Glade.Widget] Gtk.ScrolledWindow thumb_scrolledwindow;
		[Glade.Widget] Gtk.CheckButton remove_check;
		

		Gnome.Vfs.Uri dest = new Gnome.Vfs.Uri ("burn:///");
		
		int photo_index;
		bool clean;

		FSpot.ThreadProgressDialog progress_dialog;
		System.Threading.Thread command_thread;

		public CDExport (IPhotoCollection selection) : base ("cd_export_dialog")
		{
			this.selection = selection;
			
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

		void HandleBrowseExisting (object sender, System.EventArgs args)
		{
			GnomeUtil.UrlShow (null, dest.ToString ());
		}

		[DllImport ("libc")] 
		extern static int system (string program);

		public void Transfer () {
			try {
				Dialog.Destroy ();
				Gnome.Vfs.Result result = Gnome.Vfs.Result.Ok;

				foreach (Photo photo in selection.Photos) {
					Gnome.Vfs.Uri source = new Gnome.Vfs.Uri (photo.DefaultVersionUri.ToString ());
					Gnome.Vfs.Uri target = dest.Clone ();
					target = target.AppendFileName (source.ExtractShortName ());
					Gnome.Vfs.XferProgressCallback cb = new Gnome.Vfs.XferProgressCallback (Progress);
					
					progress_dialog.Message = System.String.Format (Mono.Posix.Catalog.GetString ("Transferring picture \"{0}\" To CD"), photo.Name);
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
					progress_dialog.Hide ();
					system ("nautilus-cd-burner");
				} else {
					progress_dialog.ProgressText = result.ToString ();
					progress_dialog.Message = Mono.Posix.Catalog.GetString ("Error While Transferring");
				}

			} catch (System.Exception e) {
				progress_dialog.Message = e.ToString ();
				progress_dialog.ProgressText = Mono.Posix.Catalog.GetString ("Error Transferring");
			}
			progress_dialog.Destroy ();
		}
	     
		private int Progress (Gnome.Vfs.XferProgressInfo info)
		{
			progress_dialog.ProgressText = info.Phase.ToString ();

			if (info.BytesTotal > 0) {
				progress_dialog.Fraction = info.BytesCopied / (double)info.BytesTotal;
			}
			
			switch (info.Status) {
			case Gnome.Vfs.XferProgressStatus.Vfserror:
				progress_dialog.Message = Mono.Posix.Catalog.GetString ("Error: Error while transferring; Aborting");
				return (int)Gnome.Vfs.XferErrorAction.Abort;
			case Gnome.Vfs.XferProgressStatus.Overwrite:
				progress_dialog.ProgressText = Mono.Posix.Catalog.GetString ("Error: File Already Exists; Aborting");
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

			clean = remove_check.Active;

			command_thread = new System.Threading.Thread (new System.Threading.ThreadStart (Transfer));
			command_thread.Name = Mono.Posix.Catalog.GetString ("Transferring Pictures");

			progress_dialog = new FSpot.ThreadProgressDialog (command_thread, selection.Photos.Length);
			progress_dialog.Start ();
		}
	}
}
