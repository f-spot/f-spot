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
		Window listwindow;
		Uri dest;

		[Builder.Object] Button browse_button;
		[Builder.Object] ScrolledWindow thumb_scrolledwindow;
		[Builder.Object] CheckButton remove_check;
		[Builder.Object] Label size_label;

		// This is a frame for any photos that are still in the queue
		// to be burned to disc.  As of now(March 3, 2012), that's burn:///
		[Builder.Object] Frame previous_frame;

		public bool RemovePreviousPhotos {
			get { return remove_check.Active; }
		}

		public CDExportDialog (IBrowsableCollection collection, Uri dest) :
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

			Modal = false;
			TransientFor = null;

			size_label.Text = Format.SizeForDisplay (total_size);

			thumb_scrolledwindow.Add (view);
			ShowAll ();

			previous_frame.Visible = !IsDestEmpty (dest);

			browse_button.Clicked += HandleBrowseExisting;
		}

		bool IsDestEmpty (Uri path)
		{
			IFile f = FileFactory.NewForUri (path);
			foreach (FileInfo info in f.EnumerateChildren ("*", FileQueryInfoFlags.None, null)) {
				return false;
			}
			return true;
		}

		void HandleBrowseExisting (object sender, EventArgs args)
		{
			if (listwindow == null) {
				listwindow = new Window ("Pending files to write");
				listwindow.SetDefaultSize (400, 200);
				listwindow.DeleteEvent += delegate (object o, DeleteEventArgs e) {(o as Window).Destroy (); listwindow = null;};
				TextView view = new TextView ();
				ScrolledWindow sw = new ScrolledWindow ();
				sw.Add (view);
				listwindow.Add (sw);
			} else {
				((listwindow.Child as ScrolledWindow).Child as TextView).Buffer.Text = "";
			}
			ListAll (((listwindow.Child as ScrolledWindow).Child as TextView).Buffer, dest);
			listwindow.ShowAll ();
		}

		void ListAll (TextBuffer t, Uri path)
		{
			IFile f = FileFactory.NewForUri (path);
			foreach (FileInfo info in f.EnumerateChildren ("*", FileQueryInfoFlags.None, null)) {
				t.Text += new Uri (path, info.Name) + Environment.NewLine;
				if (info.FileType == FileType.Directory)
					ListAll (t, new Uri (path, info.Name + "/"));
			}
		}
		
		~CDExportDialog ()
		{
			if (listwindow != null)
				listwindow.Destroy ();
		}

	}
}
