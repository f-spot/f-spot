//
// CDExportDialog.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (c) 2012 SUSE LINUX Products GmbH, Nuernberg, Germany.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Reflection;

using FSpot.Core;
using FSpot.Widgets;
using FSpot.UI.Dialog;

using GLib;
using Gtk;

namespace FSpot.Exporters.CD
{
	class CDExportDialog : BuilderDialog
	{
		Gtk.Window listwindow;
		System.Uri dest;

#pragma warning disable 649
		[GtkBeans.Builder.Object] Button browse_button;
		[GtkBeans.Builder.Object] ScrolledWindow thumb_scrolledwindow;
		[GtkBeans.Builder.Object] CheckButton remove_check;
		[GtkBeans.Builder.Object] Label size_label;

		// This is a frame for any photos that are still in the queue
		// to be burned to disc.  As of now(March 3, 2012), that's burn:///
		[GtkBeans.Builder.Object] Frame previous_frame;
#pragma warning restore 649

		public bool RemovePreviousPhotos {
			get { return remove_check.Active; }
		}

		public CDExportDialog (IBrowsableCollection collection, System.Uri dest) :
			base (Assembly.GetExecutingAssembly (), "CDExport.ui", "cd_export_dialog")
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

			previous_frame.Visible = !IsDestEmpty (dest);

			browse_button.Clicked += HandleBrowseExisting;
		}

		bool IsDestEmpty (System.Uri path)
		{
			GLib.File f = FileFactory.NewForUri (path);
			using (var children = f.EnumerateChildren ("*", FileQueryInfoFlags.None, null)) {
				foreach (GLib.FileInfo info in children) {
					return false;
				}
			}
			return true;
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
			using (var children = f.EnumerateChildren ("*", FileQueryInfoFlags.None, null)) {
				foreach (GLib.FileInfo info in children) {
					t.Text += new System.Uri (path, info.Name).ToString () + Environment.NewLine;
					if (info.FileType == FileType.Directory)
						ListAll (t, new System.Uri (path, info.Name + "/"));
				}
			}
		}
		
		~CDExportDialog ()
		{
			if (listwindow != null)
				listwindow.Destroy ();
		}

	}
}
