using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Mono.Unix;
using FSpot;
using FSpot.Core;
using FSpot.Filters;
using FSpot.Widgets;
using Hyena;
using FSpot.UI.Dialog;
using GLib;
using Gtk;
using GtkBeans;

namespace FSpot.Exporters.CD
{
	class CDExportDialog : BuilderDialog {
		Gtk.Window listwindow;
		System.Uri dest;

        [GtkBeans.Builder.Object] Button browse_button;
		[GtkBeans.Builder.Object] ScrolledWindow thumb_scrolledwindow;
		[GtkBeans.Builder.Object] CheckButton remove_check;
		[GtkBeans.Builder.Object] Label size_label;
		[GtkBeans.Builder.Object] Frame previous_frame;

		public bool Clean {
			get { return remove_check.Active; }
		}

		public CDExportDialog (IBrowsableCollection collection, System.Uri dest) : base (Assembly.GetExecutingAssembly (), "CDExport.ui", "cd_export_dialog")
		{
			this.dest = dest;

			// Calculate the total size
			long total_size = 0;
			string path;
			System.IO.FileInfo file_info;

			foreach (IPhoto item in collection.Items) {
				path = item.DefaultVersion.Uri.LocalPath;
				if (System.IO.File.Exists (path)) {
					file_info = new System.IO.FileInfo (path);
					total_size += file_info.Length;
				}
			}

			var view = new TrayView (collection);
			view.DisplayDates = false;
			view.DisplayTags = false;
			view.DisplayRatings = false;

			this.Modal = false;
			this.TransientFor = null;

			size_label.Text = Format.SizeForDisplay (total_size);

			thumb_scrolledwindow.Add (view);
			this.ShowAll ();

			previous_frame.Visible = IsEmpty (dest);

            browse_button.Clicked += HandleBrowseExisting;

		}

		bool IsEmpty (System.Uri path)
		{
			foreach (GLib.FileEnumerator f in FileFactory.NewForUri (path).EnumerateChildren ("*", FileQueryInfoFlags.None, null))
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
}
