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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;

using FSpot.Core;
using FSpot.UI.Dialog;
using FSpot.Widgets;

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

		public CDExportDialog (IBrowsableCollection collection, Uri dest) :
			base (Assembly.GetExecutingAssembly (), "CDExport.ui", "cd_export_dialog")
		{
			this.dest = dest;

			// Calculate the total size
			long total_size = 0;
			string path;
			FileInfo file_info;

			foreach (var item in collection.Items) {
				path = item.DefaultVersion.Uri.LocalPath;
				if (File.Exists (path)) {
					file_info = new FileInfo (path);
					total_size += file_info.Length;
				}
			}

			var view = new TrayView (collection) {
				DisplayDates = false,
				DisplayTags = false,
				DisplayRatings = false
			};

			Modal = false;
			TransientFor = null;

			// FIXME, pull in Humanizer?
			size_label.Text = total_size.ToString ();

			thumb_scrolledwindow.Add (view);
			ShowAll ();

			previous_frame.Visible = !IsDestEmpty (dest);

			browse_button.Clicked += HandleBrowseExisting;
		}

		bool IsDestEmpty (Uri path)
		{
			return !Directory.EnumerateFileSystemEntries (path.AbsolutePath).Any ();
		}

		void HandleBrowseExisting (object sender, System.EventArgs args)
		{
			if (listwindow == null) {
				listwindow = new Gtk.Window ("Pending files to write");
				listwindow.SetDefaultSize (400, 200);
				listwindow.DeleteEvent += delegate (object o, Gtk.DeleteEventArgs e) { (o as Gtk.Window).Destroy (); listwindow = null; };
				var view = new Gtk.TextView ();
				var sw = new Gtk.ScrolledWindow {
					view
				};
				listwindow.Add (sw);
			} else {
				((listwindow.Child as Gtk.ScrolledWindow).Child as Gtk.TextView).Buffer.Text = "";
			}
			ListAll (((listwindow.Child as Gtk.ScrolledWindow).Child as Gtk.TextView).Buffer, dest);
			listwindow.ShowAll ();
		}

		void ListAll (Gtk.TextBuffer t, Uri path)
		{
			// FIXME, file IO work
			//foreach (var info in Directory.EnumerateFileSystemEntries (path.AbsolutePath)) {
			//	t.Text += new Uri (info.path, info.Name).ToString () + Environment.NewLine;
			//	if (.FileType == FileType.Directory)
			//		ListAll (t, new Uri (path, info.Name + "/"));
			//}
		}

		~CDExportDialog ()
		{
			if (listwindow != null)
				listwindow.Destroy ();
		}
	}
}
