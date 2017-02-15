//
// FolderExport.cs
//
// Author:
//   Lorenzo Milesi <maxxer@yetopen.it>
//   Stephane Delcroix <stephane@delcroix.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2008-2009 Novell, Inc.
// Copyright (C) 2008 Lorenzo Milesi
// Copyright (C) 2008-2009 Stephane Delcroix
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

/*
 * Copyright (C) 2005 Alessandro Gervaso <gervystar@gervystar.net>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License as
 * published by the Free Software Foundation; either version 2 of the
 * License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public
 * License along with this program; if not, write to the
 * Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
 * Boston, MA 02110-1301
 */

//This should be used to export the selected pics to an original gallery
//located on a GIO location.

using System;
using System.IO;

using Hyena;

using Mono.Unix;

using FSpot.Core;
using FSpot.Filters;
using FSpot.Settings;
using FSpot.Widgets;
using FSpot.Utils;
using FSpot.UI.Dialog;

namespace FSpot.Exporters.Folder
{
	public class FolderExport : FSpot.Extensions.IExporter
	{
		IBrowsableCollection selection;

		[GtkBeans.Builder.Object] Gtk.Dialog dialog;
		[GtkBeans.Builder.Object] Gtk.ScrolledWindow thumb_scrolledwindow;
		[GtkBeans.Builder.Object] Gtk.Entry name_entry;
		[GtkBeans.Builder.Object] Gtk.Entry description_entry;

		[GtkBeans.Builder.Object] Gtk.CheckButton scale_check;
		[GtkBeans.Builder.Object] Gtk.CheckButton export_tags_check;
		[GtkBeans.Builder.Object] Gtk.CheckButton export_tag_icons_check;
		[GtkBeans.Builder.Object] Gtk.CheckButton open_check;

		[GtkBeans.Builder.Object] Gtk.RadioButton static_radio;
		[GtkBeans.Builder.Object] Gtk.RadioButton original_radio;
		[GtkBeans.Builder.Object] Gtk.RadioButton plain_radio;

		[GtkBeans.Builder.Object] Gtk.SpinButton size_spin;

		[GtkBeans.Builder.Object] Gtk.HBox chooser_hbox;

		public const string EXPORT_SERVICE = "folder/";
		public const string SCALE_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "scale";
		public const string SIZE_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "size";
		public const string OPEN_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "browser";
		public const string EXPORT_TAGS_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "export_tags";
		public const string EXPORT_TAG_ICONS_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "export_tag_icons";
		public const string METHOD_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "method";
		public const string URI_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "uri";
		public const string SHARPEN_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "sharpen";
		public const string INCLUDE_TARBALLS_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "include_tarballs";

		private GtkBeans.Builder builder;
		private string dialog_name = "folder_export_dialog";
		GLib.File dest;
		Gtk.FileChooserButton uri_chooser;

		bool open;
		bool scale;
		bool exportTags;
		bool exportTagIcons;
		int size;

		string description;
		string gallery_name = Catalog.GetString("Gallery");
		// FIXME: this needs to be a real temp directory
		string gallery_path = Path.Combine (Path.GetTempPath (), "f-spot-original-" + System.DateTime.Now.Ticks.ToString ());

		ThreadProgressDialog progress_dialog;
		System.Threading.Thread command_thread;

		public FolderExport () {}

		public void Run (IBrowsableCollection selection)
		{
			this.selection = selection;

			var view = new TrayView (selection);
			view.DisplayDates = false;
			view.DisplayTags = false;

			builder = new GtkBeans.Builder (null, "folder_export.ui", null);
			builder.Autoconnect (this);
			Dialog.Modal = false;
			Dialog.TransientFor = null;

			thumb_scrolledwindow.Add (view);
			HandleSizeActive (null, null);
			name_entry.Text = gallery_name;

			string uri_path = System.IO.Path.Combine (FSpot.Settings.Global.HomeDirectory, "Desktop");
			if (!System.IO.Directory.Exists (uri_path))
				uri_path = FSpot.Settings.Global.HomeDirectory;

			uri_chooser = new Gtk.FileChooserButton (Catalog.GetString ("Select Export Folder"),
								 Gtk.FileChooserAction.SelectFolder);

			uri_chooser.LocalOnly = false;

			if (!string.IsNullOrEmpty (Preferences.Get<string> (URI_KEY)))
				uri_chooser.SetCurrentFolderUri (Preferences.Get<string> (URI_KEY));
			else
				uri_chooser.SetFilename (uri_path);

			chooser_hbox.PackStart (uri_chooser);

			Dialog.ShowAll ();
			Dialog.Response += HandleResponse;

			LoadPreference (SCALE_KEY);
			LoadPreference (SIZE_KEY);
			LoadPreference (OPEN_KEY);
			LoadPreference (EXPORT_TAGS_KEY);
			LoadPreference (EXPORT_TAG_ICONS_KEY);
			LoadPreference (METHOD_KEY);
		}

		public void HandleSizeActive (object sender, System.EventArgs args)
		{
			size_spin.Sensitive = scale_check.Active;
		}

		public void HandleStandaloneActive (object sender, System.EventArgs args)
		{
			export_tags_check.Sensitive = static_radio.Active;
			HandleExportTagsActive (sender, args);
		}

		public void HandleExportTagsActive (object sender, System.EventArgs args)
		{
			export_tag_icons_check.Sensitive = export_tags_check.Active && static_radio.Active;
		}

		public void Upload ()
		{
			// FIXME: use mkstemp

			try {
				ThreadAssist.ProxyToMain (Dialog.Hide);

				GLib.File source = GLib.FileFactory.NewForPath (Path.Combine (gallery_path, gallery_name));
				GLib.File target = GLib.FileFactory.NewForPath (Path.Combine (dest.Path, source.Basename));

				if (dest.IsNative)
					gallery_path = dest.Path;

				progress_dialog.Message = Catalog.GetString ("Building Gallery");
				progress_dialog.Fraction = 0.0;

				FolderGallery gallery;
				if (static_radio.Active) {
					gallery = new HtmlGallery (selection, gallery_path, gallery_name);
				} else if (original_radio.Active) {
					gallery = new OriginalGallery (selection, gallery_path, gallery_name);
				} else {
					gallery = new FolderGallery (selection, gallery_path, gallery_name);
				}

				if (scale) {
					Log.DebugFormat ("Resize Photos to {0}.", size);
					gallery.SetScale (size);
				} else {
					Log.Debug ("Exporting full size.");
				}

				if (exportTags)
					gallery.ExportTags = true;

				if (exportTagIcons)
					gallery.ExportTagIcons = true;

				gallery.Description = description;
				gallery.GenerateLayout ();
				
				FilterSet filter_set = new FilterSet ();
				if (scale)
					filter_set.Add (new ResizeFilter ((uint) size));
				filter_set.Add (new ChmodFilter ());
				filter_set.Add (new UniqueNameFilter (new SafeUri (gallery_path)));

				for (int photo_index = 0; photo_index < selection.Count; photo_index++)
				{
					try {
						progress_dialog.Message = string.Format (Catalog.GetString ("Exporting \"{0}\"..."), selection[photo_index].Name);
						progress_dialog.Fraction = photo_index / (double) selection.Count;
						gallery.ProcessImage (photo_index, filter_set);
						progress_dialog.ProgressText = string.Format (Catalog.GetString ("{0} of {1}"), (photo_index + 1), selection.Count);
					}
					catch (Exception e) {
						Log.Error (e.ToString ());
						progress_dialog.Message = string.Format (Catalog.GetString ("Error Copying \"{0}\" to Gallery:{2}{1}"),
							selection[photo_index].Name, e.Message, Environment.NewLine);
						progress_dialog.ProgressText = Catalog.GetString ("Error");

						if (progress_dialog.PerformRetrySkip ())
							photo_index--;
					}
				}

				// create the zip tarballs for original
				if (gallery is OriginalGallery) {
					bool include_tarballs;
					try {
						include_tarballs = Preferences.Get<bool> (INCLUDE_TARBALLS_KEY);
					} catch (NullReferenceException){
						include_tarballs = true;
						Preferences.Set (INCLUDE_TARBALLS_KEY, true);
					}
					if (include_tarballs)
						(gallery as OriginalGallery).CreateZip ();
				}

				// we've created the structure, now if the destination was local (native) we are done
				// otherwise we xfer
				if (!dest.IsNative) {
					Log.DebugFormat ("Transferring \"{0}\" to \"{1}\"", source.Path, target.Path);
					progress_dialog.Message = string.Format (Catalog.GetString ("Transferring to \"{0}\""), target.Path);
					progress_dialog.ProgressText = Catalog.GetString ("Transferring...");
					source.CopyRecursive (target, GLib.FileCopyFlags.Overwrite, new GLib.Cancellable (), Progress);
				}
				
				// No need to check result here as if result is not true, an Exception will be thrown before
				progress_dialog.Message = Catalog.GetString ("Export Complete.");
				progress_dialog.Fraction = 1.0;
				progress_dialog.ProgressText = Catalog.GetString ("Exporting Photos Completed.");
				progress_dialog.ButtonLabel = Gtk.Stock.Ok;

				if (open) {
					Log.DebugFormat (string.Format ("Open URI \"{0}\"", target.Uri.ToString ()));
					ThreadAssist.ProxyToMain (() => { GtkBeans.Global.ShowUri (Dialog.Screen, target.Uri.ToString () ); });
				}

				// Save these settings for next time
				Preferences.Set (SCALE_KEY, scale);
				Preferences.Set (SIZE_KEY, size);
				Preferences.Set (OPEN_KEY, open);
				Preferences.Set (EXPORT_TAGS_KEY, exportTags);
				Preferences.Set (EXPORT_TAG_ICONS_KEY, exportTagIcons);
				Preferences.Set (METHOD_KEY, static_radio.Active ? "static" : original_radio.Active ? "original" : "folder" );
				Preferences.Set (URI_KEY, uri_chooser.Uri);
			} catch (System.Exception e) {
				Log.Error (e.ToString ());
				progress_dialog.Message = e.ToString ();
				progress_dialog.ProgressText = Catalog.GetString ("Error Transferring");
			} finally {
				// if the destination isn't local then we want to remove the temp directory we
				// created.
				if (!dest.IsNative)
					System.IO.Directory.Delete (gallery_path, true);

				ThreadAssist.ProxyToMain (() => { Dialog.Destroy(); });
			}
		}

		private void Progress (long current_num_bytes, long total_num_bytes)
		{
			if (total_num_bytes > 0)
				progress_dialog.Fraction = current_num_bytes / (double)total_num_bytes;
		}

		private void HandleResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId != Gtk.ResponseType.Ok) {
				// FIXME this is to work around a bug in gtk+ where
				// the filesystem events are still listened to when
				// a FileChooserButton is destroyed but not finalized
				// and an event comes in that wants to update the child widgets.
				Dialog.Destroy ();
				uri_chooser.Dispose ();
				uri_chooser = null;
				return;
			}

			dest = GLib.FileFactory.NewForUri (uri_chooser.Uri);
			open = open_check.Active;
			scale = scale_check.Active;
			exportTags = export_tags_check.Active;
			exportTagIcons = export_tag_icons_check.Active;

			gallery_name = name_entry.Text;

			if (description_entry != null)
				description = description_entry.Text;

			if (scale)
				size = size_spin.ValueAsInt;

			command_thread = new System.Threading.Thread (new System.Threading.ThreadStart (Upload));
			command_thread.Name = Catalog.GetString ("Exporting Photos");

			progress_dialog = new ThreadProgressDialog (command_thread, 1);
			progress_dialog.Start ();
		}

		void LoadPreference (string key)
		{
			switch (key) {
			case SCALE_KEY:
				if (scale_check.Active != Preferences.Get<bool> (key))
					scale_check.Active = Preferences.Get<bool> (key);
				break;

			case SIZE_KEY:
				int size;
				if (Preferences.TryGet<int> (key, out size))
					size_spin.Value = (double) size;
				else
					size_spin.Value = 400;
				break;

			case OPEN_KEY:
				if (open_check.Active != Preferences.Get<bool> (key))
					open_check.Active = Preferences.Get<bool> (key);
				break;

			case EXPORT_TAGS_KEY:
				if (export_tags_check.Active != Preferences.Get<bool> (key))
					export_tags_check.Active = Preferences.Get<bool> (key);
				break;

			case EXPORT_TAG_ICONS_KEY:
				if (export_tag_icons_check.Active != Preferences.Get<bool> (key))
					export_tag_icons_check.Active = Preferences.Get<bool> (key);
				break;

			case METHOD_KEY:
				static_radio.Active = (Preferences.Get<string> (key) == "static");
				original_radio.Active = (Preferences.Get<string> (key) == "original");
				plain_radio.Active = (Preferences.Get<string> (key) == "folder");
				break;
			}
		}

		private Gtk.Dialog Dialog {
			get {
				if (dialog == null)
					dialog = new Gtk.Dialog (builder.GetRawObject (dialog_name));

				return dialog;
			}
		}
	}
}
