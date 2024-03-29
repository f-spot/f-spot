//
// ZipExport.cs
//
// Author:
//   Lorenzo Milesi <maxxer@yetopen.it>
//   Stephane Delcroix <sdelcroix*novell.com>
//
// Copyright (C) 2007-2009 Novell, Inc.
// Copyright (C) 2008-2009 Lorenzo Milesi
// Copyright (C) 2007-2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;

using FSpot.Core;
using FSpot.Extensions;
using FSpot.Filters;
using FSpot.Resources.Lang;
using FSpot.Settings;
using FSpot.UI.Dialog;

using Gtk;

using Hyena.Widgets;

using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip;


namespace FSpot.Exporters.Zip
{
	public class Zip : IExporter
	{
#pragma warning disable 649
		[GtkBeans.Builder.Object] Gtk.Dialog zipdiag;
		[GtkBeans.Builder.Object] Gtk.HBox dirchooser_hbox;
		[GtkBeans.Builder.Object] Gtk.CheckButton scale_check;
		[GtkBeans.Builder.Object] Gtk.Entry filename;
		[GtkBeans.Builder.Object] Gtk.SpinButton scale_size;
		[GtkBeans.Builder.Object] Gtk.Button create_button;
#pragma warning restore 649

		IPhoto[] photos;
		Gtk.FileChooserButton uri_chooser;

		public void Run (IBrowsableCollection p)
		{
			Logger.Log.Information ("Executing ZipExport extension");
			if (p.Count == 0) {
				var md = new HigMessageDialog (App.Instance.Organizer.Window, DialogFlags.DestroyWithParent,
							  Gtk.MessageType.Error, ButtonsType.Ok,
							  Strings.NoSelectionAvailable,
							  Strings.ThisToolRequiresAnActiveSelection_PleaseSelectOneOrMore);

				md.Run ();
				md.Destroy ();
				return;
			}
			photos = p.Items.ToArray ();
			ShowDialog ();
		}

		public void ShowDialog ()
		{
			var builder = new GtkBeans.Builder (null, "zip_export.ui", null);
			builder.Autoconnect (this);
			zipdiag.Modal = false;
			zipdiag.TransientFor = null;

			uri_chooser = new Gtk.FileChooserButton (Strings.SelectExportFolder,
								 Gtk.FileChooserAction.SelectFolder);
			uri_chooser.LocalOnly = true;
			uri_chooser.SetFilename (System.IO.Path.Combine (FSpotConfiguration.HomeDirectory, "Desktop"));
			dirchooser_hbox.PackStart (uri_chooser, false, false, 2);
			filename.Text = "f-spot_export.zip";

			zipdiag.Response += on_dialog_response;
			filename.Changed += on_filename_change;
			scale_check.Toggled += on_scalecheck_change;
			on_scalecheck_change (null, null);

			zipdiag.ShowAll ();
		}

		void on_dialog_response (object sender, ResponseArgs args)
		{
			if (args.ResponseId != Gtk.ResponseType.Ok) {
				// FIXME this is to work around a bug in gtk+ where
				// the filesystem events are still listened to when
				// a FileChooserButton is destroyed but not finalized
				// and an event comes in that wants to update the child widgets.
				uri_chooser.Dispose ();
				uri_chooser = null;
			} else if (args.ResponseId == Gtk.ResponseType.Ok) {
				zip ();
			}
			zipdiag.Destroy ();
		}

		void zip ()
		{
			var dest = new System.Uri (uri_chooser.Uri);
			var crc = new Crc32 ();
			string filedest = dest.LocalPath + "/" + filename.Text;
			Logger.Log.Debug ($"Creating zip file {filedest}");
			var s = new ZipOutputStream (File.Create (filedest));
			if (scale_check.Active)
				Logger.Log.Debug ($"Scaling to {scale_size.ValueAsInt}");

			var progress_dialog = new ProgressDialog (Strings.ExportingFiles,
								  ProgressDialog.CancelButtonType.Stop,
								  photos.Length, zipdiag);

			//Pack up
			for (int i = 0; i < photos.Length; i++) {
				if (progress_dialog.Update (string.Format (Strings.PreparingPhotoX, photos[i].Name))) {
					progress_dialog.Destroy ();
					return;
				}
				string f = null;
				// FIXME: embed in a try/catch
				if (scale_check.Active) {
					var filters = new FilterSet ();
					filters.Add (new JpegFilter ());
					filters.Add (new ResizeFilter ((uint)scale_size.ValueAsInt));
					var freq = new FilterRequest (photos[i].DefaultVersion.Uri);
					filters.Convert (freq);
					f = freq.Current.LocalPath;
				} else {
					f = photos[i].DefaultVersion.Uri.LocalPath;
				}
				FileStream fs = File.OpenRead (f);

				byte[] buffer = new byte[fs.Length];
				fs.Read (buffer, 0, buffer.Length);
				var entry = new ZipEntry (System.IO.Path.GetFileName (photos[i].DefaultVersion.Uri.LocalPath));

				entry.DateTime = DateTime.Now;

				entry.Size = fs.Length;
				fs.Close ();

				crc.Reset ();
				crc.Update (buffer);

				entry.Crc = crc.Value;

				s.PutNextEntry (entry);

				s.Write (buffer, 0, buffer.Length);
			}
			s.Finish ();
			s.Close ();
			if (progress_dialog != null)
				progress_dialog.Destroy ();

		}

		void on_filename_change (object sender, System.EventArgs args)
		{
			create_button.Sensitive = System.Text.RegularExpressions.Regex.IsMatch (filename.Text, "[.]zip$");
		}

		void on_scalecheck_change (object sender, System.EventArgs args)
		{
			scale_size.Sensitive = scale_check.Active;
		}
	}
}
