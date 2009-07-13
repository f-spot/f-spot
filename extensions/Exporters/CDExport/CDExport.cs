/*
 * CDExport.cs
 *
 * Authors:
 *   Larry Ewing <lewing@novell.com>
 *   Lorenzo Milesi <maxxer@yetopen.it>
 *
 * Copyright (c) 2007-2009 Novell, Inc.
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using Mono.Unix;

using FSpot;
using FSpot.Filters;
using FSpot.Widgets;
using FSpot.Utils;
using FSpot.UI.Dialog;

using GLib;
using Gtk;
using GtkBeans;

namespace FSpotCDExport {
	class CDExportDialog : BuilderDialog {
		IBrowsableCollection selection;
		Gtk.Window listwindow;
		System.Uri dest;

		[GtkBeans.Builder.Object] ScrolledWindow thumb_scrolledwindow;
		[GtkBeans.Builder.Object] CheckButton remove_check;
		[GtkBeans.Builder.Object] CheckButton rotate_check;
		[GtkBeans.Builder.Object] Label size_label;
		[GtkBeans.Builder.Object] Frame previous_frame;

		public bool Clean {
			get { return remove_check.Active; }
		}

		public bool Rotate {
			get { return rotate_check.Active; }
		}

		public CDExportDialog (IBrowsableCollection selection, System.Uri dest) : base (Assembly.GetExecutingAssembly (), "CDExport.ui", "cd_export_dialog")
		{
			this.selection = selection;
			this.dest = dest;

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

			FSpot.Widgets.IconView view = new FSpot.Widgets.IconView (selection);
			view.DisplayDates = false;
			view.DisplayTags = false;
			view.DisplayRatings = false;

			this.Modal = false;
			this.TransientFor = null;

			size_label.Text = Format.SizeForDisplay (total_size);

			thumb_scrolledwindow.Add (view);
			this.ShowAll ();

			previous_frame.Visible = IsEmpty (dest);
			//LoadHistory ();

		}

		bool IsEmpty (System.Uri path)
		{
			foreach (GLib.FileInfo fi in FileFactory.NewForUri (path).EnumerateChildren ("*", FileQueryInfoFlags.None, null))
				return true;
			return false;
		}

		void HandleBrowseExisting (object sender, System.EventArgs args)
		{
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
		}

		void ListAll (Gtk.TextBuffer t, System.Uri path)
		{
			GLib.File f = FileFactory.NewForUri (path);
			foreach (GLib.FileInfo info in f.EnumerateChildren ("*", FileQueryInfoFlags.None, null)) {
				t.Text += new System.Uri (path, info.Name).ToString () + Environment.NewLine;
				if (info.FileType == FileType.Directory)
					ListAll (t, new System.Uri (path, info.Name + "/"));
			}
		}
		
		~CDExportDialog ()
		{
			if (listwindow != null)
				listwindow.Destroy ();
		}

	}


	public class CDExport : FSpot.Extensions.IExporter {
		IBrowsableCollection selection;

		System.Uri dest = new System.Uri ("burn:///");

		int photo_index;
		bool clean;
		bool rotate;

		CDExportDialog dialog;
		ThreadProgressDialog progress_dialog;
		System.Threading.Thread command_thread;

		public CDExport ()
		{
		}

		public void Run (IBrowsableCollection selection)
		{
			this.selection = selection;
			dialog = new CDExportDialog (selection, dest);
			//LoadHistory ();

                        if (dialog.Run () != (int)ResponseType.Ok) {
                                dialog.Destroy ();
                                return;
                        }

			clean = dialog.Clean;
			rotate = dialog.Rotate;

			command_thread = new System.Threading.Thread (new System.Threading.ThreadStart (Transfer));
			command_thread.Name = Catalog.GetString ("Transferring Pictures");

			progress_dialog = new ThreadProgressDialog (command_thread, selection.Count);
			progress_dialog.Start ();

			dialog.Destroy ();
		}

		[DllImport ("libc")]
		extern static int system (string program);

//		//FIXME: rewrite this as a Filter
	        public static GLib.File UniqueName (System.Uri path, string shortname)
	        {
	                int i = 1;
			GLib.File dest = FileFactory.NewForUri (new System.Uri (path, shortname));
	                while (dest.Exists) {
	                        string numbered_name = System.String.Format ("{0}-{1}{2}",
	                                                              System.IO.Path.GetFileNameWithoutExtension (shortname),
	                                                              i++,
	                                                              System.IO.Path.GetExtension (shortname));

				dest = FileFactory.NewForUri (new System.Uri (path, numbered_name));
	                }

	                return dest;
	        }

		void Clean (System.Uri path)
		{
			GLib.File source = FileFactory.NewForUri (path);
			foreach (GLib.FileInfo info in source.EnumerateChildren ("*", FileQueryInfoFlags.None, null)) {
				if (info.FileType == FileType.Directory)
					Clean (new System.Uri(path, info.Name + "/"));
				FileFactory.NewForUri (new System.Uri (path, info.Name)).Delete ();
			}
		}

		public void Transfer () {
			try {
				bool result = true;

				if (clean)
					Clean (dest);

				foreach (IBrowsableItem photo in selection.Items) {

				//FIXME need to implement the uniquename as a filter
					using (FilterRequest request = new FilterRequest (photo.DefaultVersionUri)) {
						if (rotate)
							new OrientationFilter ().Convert (request);

						GLib.File source = FileFactory.NewForUri (request.Current.ToString ());
						GLib.File target = UniqueName (dest, photo.Name);
						FileProgressCallback cb = Progress;

						progress_dialog.Message = System.String.Format (Catalog.GetString ("Transferring picture \"{0}\" To CD"), photo.Name);
						progress_dialog.Fraction = photo_index / (double) selection.Count;
						progress_dialog.ProgressText = System.String.Format (Catalog.GetString ("{0} of {1}"),
											     photo_index, selection.Count);

						result &= source.Copy (target,
									FileCopyFlags.None,
									null,
									cb);
					}
					photo_index++;
				}

				// FIXME the error dialog here is ugly and needs improvement when strings are not frozen.
				if (result) {
					progress_dialog.Message = Catalog.GetString ("Done Sending Photos");
					progress_dialog.Fraction = 1.0;
					progress_dialog.ProgressText = Catalog.GetString ("Transfer Complete");
					progress_dialog.ButtonLabel = Gtk.Stock.Ok;
					progress_dialog.Hide ();
					system ("brasero -n");
				} else {
					throw new System.Exception (System.String.Format ("{0}{3}{1}{3}{2}",
											  progress_dialog.Message,
											  Catalog.GetString ("Error While Transferring"),
											  result.ToString (),
											  System.Environment.NewLine));
				}

			} catch (System.Exception e) {
				FSpot.Utils.Log.DebugException (e);
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

		private void Progress (long current_num_bytes, long total_num_bytes)
		{
			progress_dialog.ProgressText = Catalog.GetString ("copying...");

			if (total_num_bytes > 0)
				progress_dialog.Fraction = current_num_bytes / (double)total_num_bytes;

		}

		private void HandleMsg (Gnome.Vfs.ModuleCallback cb)
		{
			Gnome.Vfs.ModuleCallbackStatusMessage msg = cb as Gnome.Vfs.ModuleCallbackStatusMessage;
			FSpot.Utils.Log.Debug ("CDExport: " + msg.Message);
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

	}
}
