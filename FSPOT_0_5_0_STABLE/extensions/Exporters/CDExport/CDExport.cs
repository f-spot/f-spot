using System;
using System.IO;
using System.Runtime.InteropServices;
using Mono.Unix;
using FSpot;
using FSpot.Filters;
using FSpot.Widgets;
using FSpot.Utils;
#if GIO_2_16
using GLib;
#endif

namespace FSpotCDExport {
	public class CDExport : FSpot.Extensions.IExporter {
		IBrowsableCollection selection;

		[Glade.Widget] Gtk.Dialog dialog;
		[Glade.Widget] Gtk.ScrolledWindow thumb_scrolledwindow;
		[Glade.Widget] Gtk.CheckButton remove_check;
		[Glade.Widget] Gtk.CheckButton rotate_check;
		[Glade.Widget] Gtk.Label size_label;
		[Glade.Widget] Gtk.Frame previous_frame;

#if GIO_2_16
		Gtk.Window listwindow;
		System.Uri dest = new System.Uri ("burn:///");
#else
		Gnome.Vfs.Uri dest = new Gnome.Vfs.Uri ("burn:///");
#endif

		int photo_index;
		bool clean;
		bool rotate;

		FSpot.ThreadProgressDialog progress_dialog;
		System.Threading.Thread command_thread;

		private Glade.XML xml;
		private string dialog_name = "cd_export_dialog";

		public CDExport ()
		{
		}

		public void Run (IBrowsableCollection selection)
		{

			xml = new Glade.XML (null, "CDExport.glade", dialog_name, "f-spot");
			xml.Autoconnect (this);

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

			previous_frame.Visible = IsEmpty (dest);
			//LoadHistory ();

			Dialog.Response += HandleResponse;
		}

		void HandleBrowseExisting (object sender, System.EventArgs args)
		{
#if GIO_2_16
			if (listwindow == null) {
				listwindow = new Gtk.Window ("Pending files to write");
				listwindow.SetDefaultSize (400, 200);
				listwindow.DeleteEvent += delegate (object o, Gtk.DeleteEventArgs e) {(o as Gtk.Window).Destroy (); listwindow = null;};
				Gtk.TextView view = new Gtk.TextView ();
				Gtk.TextBuffer buffer = view.Buffer;
				Gtk.ScrolledWindow sw = new Gtk.ScrolledWindow ();
				sw.Add (view);
				listwindow.Add (sw);
			} else {
				((listwindow.Child as Gtk.ScrolledWindow).Child as Gtk.TextView).Buffer.Text = "";
			}
			ListAll (((listwindow.Child as Gtk.ScrolledWindow).Child as Gtk.TextView).Buffer, dest);
			listwindow.ShowAll ();
#else
			GnomeUtil.UrlShow (dest.ToString ());
#endif
		}

#if GIO_2_16
		void ListAll (Gtk.TextBuffer t, System.Uri path)
		{
			GLib.File f = FileFactory.NewForUri (path);
			foreach (GLib.FileInfo info in f.EnumerateChildren ("*", FileQueryInfoFlags.None, null)) {
				t.Text += new System.Uri (path, info.Name).ToString () + Environment.NewLine;
				if (info.FileType == FileType.Directory)
					ListAll (t, new System.Uri (path, info.Name + "/"));
			}
		}
#endif
		[DllImport ("libc")]
		extern static int system (string program);

//		//FIXME: rewrite this as a Filter
#if GIO_2_16
	        public static GLib.File UniqueName (System.Uri path, string shortname)
#else
	        public static Gnome.Vfs.Uri UniqueName (Gnome.Vfs.Uri path, string shortname)
#endif
	        {
	                int i = 1;
#if GIO_2_16
			GLib.File dest = FileFactory.NewForUri (new System.Uri (path, shortname));
#else
			Gnome.Vfs.Uri target = path.Clone();
	                Gnome.Vfs.Uri dest = target.AppendFileName(shortname);
#endif
	                while (dest.Exists) {
	                        string numbered_name = System.String.Format ("{0}-{1}{2}",
	                                                              System.IO.Path.GetFileNameWithoutExtension (shortname),
	                                                              i++,
	                                                              System.IO.Path.GetExtension (shortname));

#if GIO_2_16
				dest = FileFactory.NewForUri (new System.Uri (path, numbered_name));
#else
				dest = target.AppendFileName(numbered_name);
#endif
	                }

	                return dest;
	        }

#if GIO_2_16
		void Clean (System.Uri path)
		{
			GLib.File source = FileFactory.NewForUri (path);
			foreach (GLib.FileInfo info in source.EnumerateChildren ("*", FileQueryInfoFlags.None, null)) {
				if (info.FileType == FileType.Directory)
					Clean (new System.Uri(path, info.Name + "/"));
				FileFactory.NewForUri (new System.Uri (path, info.Name)).Delete ();
			}
		}
#else
		void Clean ()
		{
			Gnome.Vfs.Uri target = dest.Clone ();
			Gnome.Vfs.XferProgressCallback cb = new Gnome.Vfs.XferProgressCallback (Progress);
			Gnome.Vfs.Xfer.XferDeleteList (new Gnome.Vfs.Uri [] {target}, Gnome.Vfs.XferErrorMode.Query, Gnome.Vfs.XferOptions.Recursive, cb);
		}
#endif

#if GIO_2_16
		bool IsEmpty (System.Uri path)
		{
			foreach (GLib.FileInfo fi in FileFactory.NewForUri (path).EnumerateChildren ("*", FileQueryInfoFlags.None, null))
				return true;
			return false;
		}
#else
		bool IsEmpty (Gnome.Vfs.Uri path)
		{
			return Gnome.Vfs.Directory.GetEntries (path).Length != 0;
		}
#endif

		public void Transfer () {
			try {
#if GIO_2_16
				bool result = true;
#else
				Gnome.Vfs.Result result = Gnome.Vfs.Result.Ok;
#endif

				if (clean)
#if GIO_2_16
					Clean (dest);
#else
					Clean ();
#endif

				foreach (IBrowsableItem photo in selection.Items) {

				//FIXME need to implement the uniquename as a filter
					using (FilterRequest request = new FilterRequest (photo.DefaultVersionUri)) {
						if (rotate)
							new OrientationFilter ().Convert (request);

#if GIO_2_16
						GLib.File source = FileFactory.NewForUri (request.Current.ToString ());
#else
						Gnome.Vfs.Uri source = new Gnome.Vfs.Uri (request.Current.ToString ());
						Gnome.Vfs.Uri target = dest.Clone ();
#endif
#if GIO_2_16
						GLib.File target = UniqueName (dest, photo.Name);
						FileProgressCallback cb = Progress;
#else
						target = UniqueName (target, photo.Name);
						Gnome.Vfs.XferProgressCallback cb = new Gnome.Vfs.XferProgressCallback (Progress);
#endif

						progress_dialog.Message = System.String.Format (Catalog.GetString ("Transferring picture \"{0}\" To CD"), photo.Name);
						progress_dialog.Fraction = photo_index / (double) selection.Count;
						progress_dialog.ProgressText = System.String.Format (Catalog.GetString ("{0} of {1}"),
											     photo_index, selection.Count);

#if GIO_2_16
						result &= source.Copy (target,
									FileCopyFlags.None,
									null,
									cb);
#else
						result = Gnome.Vfs.Xfer.XferUri (source, target,
										 Gnome.Vfs.XferOptions.Default,
										 Gnome.Vfs.XferErrorMode.Abort,
										 Gnome.Vfs.XferOverwriteMode.Replace,
										 cb);
#endif
					}
					photo_index++;
				}

				// FIXME the error dialog here is ugly and needs improvement when strings are not frozen.
#if GIO_2_16
				if (result) {
#else
				if (result == Gnome.Vfs.Result.Ok) {
#endif
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

#if GIO_2_16
		private void Progress (long current_num_bytes, long total_num_bytes)
#else
		private int Progress (Gnome.Vfs.XferProgressInfo info)
#endif
		{
#if GIO_2_16
			progress_dialog.ProgressText = Catalog.GetString ("copying...");
#else
			progress_dialog.ProgressText = info.Phase.ToString ();
#endif

#if GIO_2_16
			if (total_num_bytes > 0)
				progress_dialog.Fraction = current_num_bytes / (double)total_num_bytes;
#else
			if (info.BytesTotal > 0)
				progress_dialog.Fraction = info.BytesCopied / (double)info.BytesTotal;
#endif

#if !GIO_2_16
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
#endif
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
#if GIO_2_16
			if (listwindow != null)
				listwindow.Destroy ();
#endif
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

		private Gtk.Dialog Dialog {
			get {
				if (dialog == null)
					dialog = (Gtk.Dialog) xml.GetWidget (dialog_name);

				return dialog;
			}
		}
	}
}
