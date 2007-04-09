using System.IO;
using System.Runtime.InteropServices;
using Mono.Unix;

namespace FSpot {
	public class CDExport : GladeDialog, FSpot.Extensions.IExporter {
		IBrowsableCollection selection;

		[Glade.Widget] Gtk.ScrolledWindow thumb_scrolledwindow;
		[Glade.Widget] Gtk.CheckButton remove_check;
		[Glade.Widget] Gtk.CheckButton rotate_check;
		[Glade.Widget] Gtk.Label size_label;

		Gnome.Vfs.Uri dest = new Gnome.Vfs.Uri ("burn:///");
		
		int photo_index;
		bool clean;
		bool rotate;

		FSpot.ThreadProgressDialog progress_dialog;
		System.Threading.Thread command_thread;

		public CDExport () : base ("cd_export_dialog")
		{
		}

		public void Run (IBrowsableCollection selection)
		{
			this.selection = selection;

			// Calculate the total size
			long total_size = 0;
			string path;
			System.IO.FileInfo file_info;

			foreach (IBrowsableItem item in selection.Items) {
				path = item.DefaultVersionUri.LocalPath;
				if (System.IO.File.Exists (path)) {
					file_info = new System.IO.FileInfo (path);
					total_size += file_info.Length;
				}
			}

			IconView view = new IconView (selection);
			view.DisplayDates = false;
			view.DisplayTags = false;

			Dialog.Modal = false;
			Dialog.TransientFor = null;
			
			size_label.Text = SizeUtil.ToHumanReadable (total_size);

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

		//FIXME: rewrite this as a Filter
	        public static Gnome.Vfs.Uri UniqueName (Gnome.Vfs.Uri path, string shortname)
	        {
	                int i = 1;
			Gnome.Vfs.Uri target = path.Clone();
	                Gnome.Vfs.Uri dest = target.AppendFileName(shortname);
	
	                while (dest.Exists) {
	                        string numbered_name = System.String.Format ("{0}-{1}{2}",
	                                                              System.IO.Path.GetFileNameWithoutExtension (shortname),
	                                                              i++,
	                                                              System.IO.Path.GetExtension (shortname));
	
	                	dest = target.AppendFileName(numbered_name);
	                }
	
	                return dest;
	        }

		void Clean ()
		{
			Gnome.Vfs.Uri target = dest.Clone ();
			Gnome.Vfs.XferProgressCallback cb = new Gnome.Vfs.XferProgressCallback (Progress);
			Gnome.Vfs.Xfer.XferDeleteList (new Gnome.Vfs.Uri [] {target}, Gnome.Vfs.XferErrorMode.Query, Gnome.Vfs.XferOptions.Recursive, cb);
			
		}

		public void Transfer () {
			try {
				Gnome.Vfs.Result result = Gnome.Vfs.Result.Ok;

				if (clean)
					Clean ();

				foreach (IBrowsableItem photo in selection.Items) {

 				//FIXME need to implement the uniquename as a filter	
					using (Filters.FilterRequest request = new Filters.FilterRequest (photo.DefaultVersionUri)) {
						if (rotate)
							new Filters.OrientationFilter ().Convert (request);
						
						Gnome.Vfs.Uri source = new Gnome.Vfs.Uri (request.Current.ToString ());
						Gnome.Vfs.Uri target = dest.Clone ();
						target = UniqueName (target, source.ExtractShortName ());
						
						Gnome.Vfs.XferProgressCallback cb = new Gnome.Vfs.XferProgressCallback (Progress);
						
						progress_dialog.Message = System.String.Format (Catalog.GetString ("Transferring picture \"{0}\" To CD"), photo.Name);
						progress_dialog.Fraction = photo_index / (double) selection.Count;
						progress_dialog.ProgressText = System.String.Format (Catalog.GetString ("{0} of {1}"), 
												     photo_index, selection.Count);
						result = Gnome.Vfs.Xfer.XferUri (source, target, 
										 Gnome.Vfs.XferOptions.Default, 
										 Gnome.Vfs.XferErrorMode.Abort, 
										 Gnome.Vfs.XferOverwriteMode.Replace, 
										 cb);
					}
					photo_index++;
				}

				// FIXME the error dialog here is ugly and needs improvement when strings are not frozen.
				if (result == Gnome.Vfs.Result.Ok) {
					progress_dialog.Message = Catalog.GetString ("Done Sending Photos");
					progress_dialog.Fraction = 1.0;
					progress_dialog.ProgressText = Catalog.GetString ("Transfer Complete");
					progress_dialog.ButtonLabel = Gtk.Stock.Ok;
					progress_dialog.Hide ();
					system ("nautilus-cd-burner");
				} else {
					throw new System.Exception (System.String.Format ("{0}{3}{1}{3}{2}", 
											  progress_dialog.Message,
											  Catalog.GetString ("Error While Transferring"), 
											  result.ToString (),
											  System.Environment.NewLine));
				}

			} catch (System.Exception e) {
				progress_dialog.Message = e.ToString ();
				progress_dialog.ProgressText = Catalog.GetString ("Error Transferring");
				return;
			}
			Gtk.Application.Invoke (this.Destroy);
		}
		
		private void Destroy (object sender, System.EventArgs args)
		{
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
				progress_dialog.Message = Catalog.GetString ("Error: Error while transferring; Aborting");
				return (int)Gnome.Vfs.XferErrorAction.Abort;
			case Gnome.Vfs.XferProgressStatus.Overwrite:
				progress_dialog.ProgressText = Catalog.GetString ("Error: File Already Exists; Aborting");
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
			rotate = rotate_check.Active;
			Dialog.Destroy ();

			command_thread = new System.Threading.Thread (new System.Threading.ThreadStart (Transfer));
			command_thread.Name = Catalog.GetString ("Transferring Pictures");

			progress_dialog = new FSpot.ThreadProgressDialog (command_thread, selection.Count);
			progress_dialog.Start ();
		}
	}
}
