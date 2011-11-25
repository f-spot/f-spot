//
// MetaPixel.cs
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
using System.Collections.Generic;
using Gtk;

using FSpot;
using FSpot.Extensions;
using FSpot.Widgets;
using FSpot.Filters;
using FSpot.UI.Dialog;
using Hyena;
using Mono.Unix;

namespace MetaPixelExtension {
	public class MetaPixel: ICommand
	{
		[Glade.Widget] Gtk.Dialog metapixel_dialog;
		[Glade.Widget] Gtk.HBox tagentry_box;
		[Glade.Widget] Gtk.SpinButton icon_x_size;
		[Glade.Widget] Gtk.SpinButton icon_y_size;
		[Glade.Widget] Gtk.RadioButton tags_radio;
		[Glade.Widget] Gtk.RadioButton current_radio;
		FSpot.Widgets.TagEntry miniatures_tags;
		string minidir_tmp;

		public void Run (object o, EventArgs e) {
			Log.Information ("Executing MetaPixel extension");
			if (App.Instance.Organizer.SelectedPhotos ().Length == 0) {
				InfoDialog (Catalog.GetString ("No selection available"),
					    Catalog.GetString ("This tool requires an active selection. Please select one or more pictures and try again"),
					    Gtk.MessageType.Error);
				return;
			} else {
				//Check for MetaPixel executable
				string output = "";
				try {
					System.Diagnostics.Process mp_check = new System.Diagnostics.Process ();
					mp_check.StartInfo.RedirectStandardOutput = true;
					mp_check.StartInfo.UseShellExecute = false;
					mp_check.StartInfo.FileName = "metapixel";
					mp_check.StartInfo.Arguments = "--version";
					mp_check.Start ();
					mp_check.WaitForExit ();
					StreamReader sroutput = mp_check.StandardOutput;
					output = sroutput.ReadLine ();
				} catch (System.Exception) {
				}
				if (!System.Text.RegularExpressions.Regex.IsMatch (output, "^metapixel")) {
					InfoDialog (Catalog.GetString ("Metapixel not available"),
						    Catalog.GetString ("The metapixel executable was not found in path. Please check that you have it installed and that you have permissions to execute it"),
						    Gtk.MessageType.Error);
					return;
				}

				ShowDialog ();
			}
		}

		public void ShowDialog () {
			Glade.XML xml = new Glade.XML (null, "MetaPixel.glade", "metapixel_dialog", "f-spot");
			xml.Autoconnect (this);
			metapixel_dialog.Modal = false;
			metapixel_dialog.TransientFor = null;

			miniatures_tags = new FSpot.Widgets.TagEntry (App.Instance.Database.Tags, false);
			miniatures_tags.UpdateFromTagNames (new string []{});
			tagentry_box.Add (miniatures_tags);

			metapixel_dialog.Response += on_dialog_response;
			current_radio.Active = true;
			tags_radio.Toggled += HandleTagsRadioToggled;
			HandleTagsRadioToggled (null, null);
			metapixel_dialog.ShowAll ();
		}

		void on_dialog_response (object obj, ResponseArgs args) {
			if (args.ResponseId == ResponseType.Ok) {
				create_mosaics ();
			}
			metapixel_dialog.Destroy ();
		}

		void create_mosaics () {
			//Prepare the query
			Db db = App.Instance.Database;
			FSpot.PhotoQuery mini_query = new FSpot.PhotoQuery (db.Photos);
			Photo [] photos;

			if (tags_radio.Active) {
				//Build tag array
				List<Tag> taglist = new List<Tag> ();
				foreach (string tag_name in miniatures_tags.GetTypedTagNames ()) {
					Tag t = db.Tags.GetTagByName (tag_name);
					if (t != null)
						taglist.Add(t);
				}
				mini_query.Terms = FSpot.OrTerm.FromTags (taglist.ToArray ());
				photos = mini_query.Photos;
			} else {
				photos = App.Instance.Organizer.Query.Photos;
			}

			if (photos.Length == 0) {
				//There is no photo for the selected tags! :(
				InfoDialog (Catalog.GetString ("No photos for the selection"),
					    Catalog.GetString ("The tags selected provided no pictures. Please select different tags"),
					    Gtk.MessageType.Error);
				return;
			}

			//Create minis
			ProgressDialog progress_dialog = null;
			progress_dialog = new ProgressDialog (Catalog.GetString ("Creating miniatures"),
							      ProgressDialog.CancelButtonType.Stop,
							      photos.Length, metapixel_dialog);

			minidir_tmp = System.IO.Path.GetTempFileName ();
			System.IO.File.Delete (minidir_tmp);
			System.IO.Directory.CreateDirectory (minidir_tmp);
			minidir_tmp += "/";

			//Call MetaPixel to create the minis
			foreach (Photo p in photos) {
				if (progress_dialog.Update (String.Format (Catalog.GetString ("Preparing photo \"{0}\""), p.Name))) {
					progress_dialog.Destroy ();
					DeleteTmp ();
					return;
				}
				//FIXME should switch to retry/skip
				if (!GLib.FileFactory.NewForUri (p.DefaultVersion.Uri).Exists) {
					Log.WarningFormat (String.Format ("Couldn't access photo {0} while creating miniatures", p.DefaultVersion.Uri.LocalPath));
					continue;
				}
				//FIXME Check if the picture's format is supproted (jpg, gif)

				FilterSet filters = new FilterSet ();
				filters.Add (new JpegFilter ());
				FilterRequest freq = new FilterRequest (p.DefaultVersion.Uri);
				filters.Convert (freq);

				//We use photo id for minis, instead of photo names, to avoid duplicates
				string minifile = minidir_tmp + p.Id.ToString() + System.IO.Path.GetExtension (p.DefaultVersion.Uri.ToString ());
				string prepare_command = String.Format ("--prepare -w {0} -h {1} {2} {3} {4}tables.mxt",
									icon_x_size.Text, //Minis width
									icon_y_size.Text, //Minis height
									GLib.Shell.Quote (freq.Current.LocalPath), //Source image
									GLib.Shell.Quote (minifile),  //Dest image
									minidir_tmp);  //Table file
				Log.Debug ("Executing: metapixel " + prepare_command);

				System.Diagnostics.Process mp_prep = System.Diagnostics.Process.Start ("metapixel", prepare_command);
				mp_prep.WaitForExit ();
				if (!System.IO.File.Exists (minifile)) {
					Log.DebugFormat ("No mini? No party! {0}", minifile);
					continue;
				}

			} //Finished preparing!
			if (progress_dialog != null)
				progress_dialog.Destroy ();

			progress_dialog = null;
			progress_dialog = new ProgressDialog (Catalog.GetString ("Creating photomosaics"),
							      ProgressDialog.CancelButtonType.Stop,
							      App.Instance.Organizer.SelectedPhotos ().Length, metapixel_dialog);

			//Now create the mosaics!
			uint error_count = 0;
			foreach (Photo p in App.Instance.Organizer.SelectedPhotos ()) {
				if (progress_dialog.Update (String.Format (Catalog.GetString ("Processing \"{0}\""), p.Name))) {
					progress_dialog.Destroy ();
					DeleteTmp ();
					return;
				}
				//FIXME should switch to retry/skip
				if (!GLib.FileFactory.NewForUri (p.DefaultVersion.Uri).Exists) {
					Log.WarningFormat (String.Format ("Couldn't access photo {0} while creating mosaics", p.DefaultVersion.Uri.LocalPath));
					error_count ++;
					continue;
				}

				//FIXME Check if the picture's format is supproted (jpg, gif)

				FilterSet filters = new FilterSet ();
				filters.Add (new JpegFilter ());
				FilterRequest freq = new FilterRequest (p.DefaultVersion.Uri);
				filters.Convert (freq);

				string name = GetVersionName (p);
				System.Uri mosaic = GetUriForVersionName (p, name);

				string mosaic_command = String.Format ("--metapixel -l {0} {1} {2}",
									minidir_tmp,
									GLib.Shell.Quote (freq.Current.LocalPath),
									GLib.Shell.Quote (mosaic.LocalPath));
				Log.Debug ("Executing: metapixel " + mosaic_command);
				System.Diagnostics.Process mp_exe = System.Diagnostics.Process.Start ("metapixel", mosaic_command);
				mp_exe.WaitForExit ();
				if (!GLib.FileFactory.NewForUri (mosaic).Exists) {
					Log.Warning ("Error in processing image " + p.Name);
					error_count ++;
					continue;
				}

				p.DefaultVersionId = p.AddVersion (mosaic, name, true);
				p.Changes.DataChanged = true;
				Core.Database.Photos.Commit (p);

			} //Finished creating mosaics
			if (progress_dialog != null)
				progress_dialog.Destroy ();


			string final_message = "Your mosaics have been generated as new versions of the pictures you selected";
			if (error_count > 0)
				final_message += String.Format (".\n{0} images out of {1} had errors",
							error_count, App.Instance.Organizer.SelectedPhotos ().Length);
			InfoDialog (Catalog.GetString ("PhotoMosaics generated!"),
				    Catalog.GetString (final_message),
				    (error_count == 0 ? Gtk.MessageType.Info : Gtk.MessageType.Warning));
			DeleteTmp ();

		}

		private void HandleTagsRadioToggled (object o, EventArgs e) {
			miniatures_tags.Sensitive = tags_radio.Active;
		}

                private static string GetVersionName (Photo p)
                {
                        return GetVersionName (p, 1);
                }

                private static string GetVersionName (Photo p, int i)
                {
                        string name = Catalog.GetPluralString ("PhotoMosaic", "PhotoMosaic ({0})", i);
                        name = String.Format (name, i);
                        if (p.VersionNameExists (name))
                                return GetVersionName (p, i + 1);
                        return name;
                }

                private System.Uri GetUriForVersionName (Photo p, string version_name)
                {
                        string name_without_ext = System.IO.Path.GetFileNameWithoutExtension (p.Name);
                        return new System.Uri (System.IO.Path.Combine (DirectoryPath (p),  name_without_ext
                                               + " (" + version_name + ")" + ".jpg"));
                }

                private static string DirectoryPath (Photo p)
                {
                        System.Uri uri = p.VersionUri (Photo.OriginalVersionId);
                        return uri.Scheme + "://" + uri.Host + System.IO.Path.GetDirectoryName (uri.AbsolutePath);
                }

		private void DeleteTmp ()
		{
			//Clean temp workdir
			DirectoryInfo dir = new DirectoryInfo(minidir_tmp);
			FileInfo[] tmpfiles = dir.GetFiles();
			foreach (FileInfo f in tmpfiles) {
				if (System.IO.File.Exists (minidir_tmp + f.Name)) {
					System.IO.File.Delete (minidir_tmp + f.Name);
				}
			}
			if (System.IO.Directory.Exists (minidir_tmp)) {
				System.IO.Directory.Delete(minidir_tmp);
			}
		}

		private void InfoDialog (string title, string msg, Gtk.MessageType type) {
			HigMessageDialog md = new HigMessageDialog (App.Instance.Organizer.Window, DialogFlags.DestroyWithParent,
						  type, ButtonsType.Ok, title, msg);

			md.Run ();
			md.Destroy ();

		}

	}
}
