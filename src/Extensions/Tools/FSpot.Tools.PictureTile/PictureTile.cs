//
// PictureTile.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
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
using System.IO;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using Gtk;

using FSpot;
using FSpot.Extensions;
using FSpot.Widgets;
using FSpot.Filters;
using FSpot.UI.Dialog;
using Hyena;
using Mono.Unix;

namespace PictureTileExtension {
	public class PictureTile: ICommand
	{
		[Glade.Widget] Gtk.Dialog picturetile_dialog;
		[Glade.Widget] Gtk.SpinButton x_max_size;
		[Glade.Widget] Gtk.SpinButton y_max_size;
		[Glade.Widget] Gtk.SpinButton space_between_images;
		[Glade.Widget] Gtk.SpinButton outside_border;
		[Glade.Widget] Gtk.ComboBox background_color;
		[Glade.Widget] Gtk.SpinButton image_scale;
		[Glade.Widget] Gtk.CheckButton uniform_images;
		[Glade.Widget] Gtk.RadioButton jpeg_radio;
		[Glade.Widget] Gtk.RadioButton tiff_radio;
		[Glade.Widget] Gtk.SpinButton pages;
		[Glade.Widget] Gtk.SpinButton jpeg_quality;
		string dir_tmp;
		string destfile_tmp;
		string [] colors = {"white", "black"};
		Tag [] photo_tags;

		public void Run (object o, EventArgs e) {
			Log.Information ("Executing PictureTile extension");
			if (App.Instance.Organizer.SelectedPhotos ().Length == 0) {
				InfoDialog (Catalog.GetString ("No selection available"),
					    Catalog.GetString ("This tool requires an active selection. Please select one or more pictures and try again"),
					    Gtk.MessageType.Error);
				return;
			} else {
				//Check for PictureTile executable
				string output = "";
				try {
					System.Diagnostics.Process mp_check = new System.Diagnostics.Process ();
					mp_check.StartInfo.RedirectStandardOutput = true;
					mp_check.StartInfo.RedirectStandardError = true;
					mp_check.StartInfo.UseShellExecute = false;
					mp_check.StartInfo.FileName = "picturetile.pl";
					mp_check.Start ();
					mp_check.WaitForExit ();
					StreamReader sroutput = mp_check.StandardError;
					output = sroutput.ReadLine ();
				} catch (System.Exception) {
				}
				if (!System.Text.RegularExpressions.Regex.IsMatch (output, "^picturetile")) {
					InfoDialog (Catalog.GetString ("PictureTile not available"),
						    Catalog.GetString ("The picturetile.pl executable was not found in path. Please check that you have it installed and that you have permissions to execute it"),
						    Gtk.MessageType.Error);
					return;
				}

				ShowDialog ();
			}
		}

		public void ShowDialog () {
			Glade.XML xml = new Glade.XML (null, "PictureTile.glade", "picturetile_dialog", "f-spot");
			xml.Autoconnect (this);
			picturetile_dialog.Modal = true;
			picturetile_dialog.TransientFor = null;

			picturetile_dialog.Response += OnDialogReponse;
			jpeg_radio.Active = true;
			jpeg_radio.Toggled += HandleFormatRadioToggled;
			HandleFormatRadioToggled (null, null);
			PopulateCombo ();
			picturetile_dialog.ShowAll ();
		}

		void OnDialogReponse (object obj, ResponseArgs args) {
			if (args.ResponseId == ResponseType.Ok) {
				CreatePhotoWall ();
			}
			picturetile_dialog.Destroy ();
		}

		void CreatePhotoWall () {
			dir_tmp = System.IO.Path.GetTempFileName ();
			System.IO.File.Delete (dir_tmp);
			System.IO.Directory.CreateDirectory (dir_tmp);
			dir_tmp += "/";

			//Prepare the pictures
			ProgressDialog progress_dialog = null;
			progress_dialog = new ProgressDialog (Catalog.GetString ("Preparing selected pictures"),
							      ProgressDialog.CancelButtonType.Stop,
							      App.Instance.Organizer.SelectedPhotos ().Length, picturetile_dialog);

			FilterSet filters = new FilterSet ();
			filters.Add (new JpegFilter ());
			uint counter = 0;
			List<Tag> all_tags = new List<Tag> ();
			foreach (Photo p in App.Instance.Organizer.SelectedPhotos ()) {
				if (progress_dialog.Update (String.Format (Catalog.GetString ("Processing \"{0}\""), p.Name))) {
					progress_dialog.Destroy ();
					DeleteTmp ();
					return;
				}

				//Store photo tags, to attach them later on import
				foreach (Tag tag in p.Tags) {
					if (! all_tags.Contains (tag))
						all_tags.Add (tag);
				}

				//FIXME should switch to retry/skip
				if (!GLib.FileFactory.NewForUri (p.DefaultVersion.Uri).Exists) {
					Log.WarningFormat ("Couldn't access photo {0} while creating mosaics", p.DefaultVersion.Uri.LocalPath);
					continue;
				}

				using (FilterRequest freq = new FilterRequest (p.DefaultVersion.Uri)) {
					filters.Convert (freq);
					File.Copy (freq.Current.LocalPath, String.Format ("{0}{1}.jpg", dir_tmp, counter ++));
				}
			}
			if (progress_dialog != null)
				progress_dialog.Destroy ();

			photo_tags = all_tags.ToArray ();

			string uniform = "";
			if (uniform_images.Active)
				uniform = "--uniform";
			string output_format = "jpeg";
			if (tiff_radio.Active)
				output_format = "tiff";
			string scale = String.Format (CultureInfo.InvariantCulture, "{0,4}", (double) image_scale.Value / (double) 100);

			destfile_tmp = String.Format ("{0}.{1}", System.IO.Path.GetTempFileName (), output_format);

			//Execute picturetile
			string picturetile_command = String.Format ("--size {0}x{1} " +
									"--directory {2} " +
									"--scale {3} " +
									"--margin {4} " +
									"--border {5} " +
									"--background {6} " +
									"--pages {7} " +
									"{8} " +
									"{9}",
								x_max_size.Text,
								y_max_size.Text,
								dir_tmp,
								scale,
								space_between_images.Text,
								outside_border.Text,
								colors [background_color.Active],
								pages.Text,
								uniform,
								destfile_tmp);
			Log.Debug ("Executing: picturetile.pl " + picturetile_command);
			System.Diagnostics.Process pt_exe = System.Diagnostics.Process.Start ("picturetile.pl", picturetile_command);
			pt_exe.WaitForExit ();

			// Handle multiple files generation (pages).
			// If the user wants 2 pages (images), and the output filename is out.jpg, picturetile will create
			// /tmp/out1.jpg and /tmp/out2.jpg.
			System.IO.DirectoryInfo di = new System.IO.DirectoryInfo (System.IO.Path.GetDirectoryName (destfile_tmp));
			string filemask = System.IO.Path.GetFileNameWithoutExtension (destfile_tmp) + "*" + System.IO.Path.GetExtension (destfile_tmp);
			FileInfo [] fi = di.GetFiles (filemask);

			// Move generated files to f-spot photodir
			string [] photo_import_list = new string [fi.Length];
			counter = 0;
			foreach (FileInfo f in fi) {
				string orig = System.IO.Path.Combine (f.DirectoryName, f.Name);
				photo_import_list [counter ++] = MoveFile (orig);
			}

			//Add the pic(s) to F-Spot!
			Db db = App.Instance.Database;
			ImportCommand command = new ImportCommand (null);
			if (command.ImportFromPaths (db.Photos, photo_import_list, photo_tags) > 0) {
				InfoDialog (Catalog.GetString ("PhotoWall generated!"),
					    Catalog.GetString ("Your photo wall have been generated and imported in F-Spot. Select the last roll to see it"),
					    Gtk.MessageType.Info);
			} else {
				InfoDialog (Catalog.GetString ("Error importing photowall"),
					    Catalog.GetString ("An error occurred while importing the newly generated photowall to F-Spot"),
					    Gtk.MessageType.Error);
			}
			DeleteTmp ();

		}

		private void HandleFormatRadioToggled (object o, EventArgs e) {
			jpeg_quality.Sensitive = jpeg_radio.Active;
		}

		private void DeleteTmp ()
		{
			//Clean temp workdir
			DirectoryInfo dir = new DirectoryInfo(dir_tmp);
			FileInfo[] tmpfiles = dir.GetFiles();
			foreach (FileInfo f in tmpfiles) {
				if (System.IO.File.Exists(dir_tmp + f.Name)) {
					System.IO.File.Delete (dir_tmp + f.Name);
				}
			}
			if (System.IO.Directory.Exists(dir_tmp)) {
				System.IO.Directory.Delete(dir_tmp);
			}
		}

		private void PopulateCombo () {
			foreach (string c in colors) {
				background_color.AppendText (c);
			}
			background_color.Active = 0;
		}

		private string MoveFile (string orig) {
			string dest = FileImportBackend.ChooseLocation (orig);
			System.IO.File.Move (orig, dest);
			return dest;
		}

		private void InfoDialog (string title, string msg, Gtk.MessageType type) {
			HigMessageDialog md = new HigMessageDialog (App.Instance.Organizer.Window, DialogFlags.DestroyWithParent,
						  type, ButtonsType.Ok, title, msg);

			md.Run ();
			md.Destroy ();

		}

	}
}
