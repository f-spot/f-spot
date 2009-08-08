/*
 * ZipExport.cs
 * Simple zip exporter. Creates a zip file with unmodified pictures.
 *
 * Author(s)
 * 	Lorenzo Milesi <maxxer@yetopen.it>
 *
 * Many thanks to Stephane for his help and patience. :)
 *
 * This is free software. See COPYING for details
 * (c) YetOpen S.r.l.
 */


using FSpot;
using FSpot.UI.Dialog;
using FSpot.Extensions;
using FSpot.Filters;
using FSpot.Utils;
using System;
using System.IO;
using System.Collections;
using Mono.Unix;
using Gtk;
using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.GZip;

namespace ZipExport {
	public class Zip : IExporter {

		[Glade.Widget] Gtk.Dialog zipdiag;
		[Glade.Widget] Gtk.HBox dirchooser_hbox;
		[Glade.Widget] Gtk.CheckButton scale_check;
		[Glade.Widget] Gtk.Entry filename;
		[Glade.Widget] Gtk.SpinButton scale_size;
		[Glade.Widget] Gtk.Button create_button;

		IBrowsableItem [] photos;
		Gtk.FileChooserButton uri_chooser;

		public void Run (IBrowsableCollection p) {
			Log.Information ("Executing ZipExport extension");
			if (p.Count == 0) {
				HigMessageDialog md = new HigMessageDialog (MainWindow.Toplevel.Window, DialogFlags.DestroyWithParent,
							  Gtk.MessageType.Error, ButtonsType.Ok,
							  Catalog.GetString ("No selection available"),
							  Catalog.GetString ("This tool requires an active selection. Please select one or more pictures and try again"));

				md.Run ();
				md.Destroy ();
				return;
			}
			photos = p.Items;
			ShowDialog ();
		}

		public void ShowDialog () {
			Glade.XML xml = new Glade.XML (null, "ZipExport.glade", "zipdiag", "f-spot");
			xml.Autoconnect (this);
			zipdiag.Modal = false;
			zipdiag.TransientFor = null;

			uri_chooser = new Gtk.FileChooserButton (Catalog.GetString ("Select export folder"),
								 Gtk.FileChooserAction.SelectFolder);
			uri_chooser.LocalOnly = true;
			uri_chooser.SetFilename (System.IO.Path.Combine (FSpot.Global.HomeDirectory, "Desktop"));
			dirchooser_hbox.PackStart (uri_chooser, false, false, 2);
			filename.Text = "f-spot_export.zip";

			zipdiag.Response += on_dialog_response;
			filename.Changed += on_filename_change;
			scale_check.Toggled += on_scalecheck_change;
			on_scalecheck_change (null, null);

			zipdiag.ShowAll ();
		}

		private void on_dialog_response (object sender, ResponseArgs args) {
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

		void zip () {
			System.Uri dest = new System.Uri (uri_chooser.Uri);
			Crc32 crc = new Crc32 ();
			string filedest = dest.LocalPath + "/" + filename.Text;
			Log.Debug ("Creating zip file {0}", filedest);
			ZipOutputStream s = new ZipOutputStream (File.Create(filedest));
			if (scale_check.Active)
				Log.Debug ("Scaling to {0}", scale_size.ValueAsInt);

			ProgressDialog progress_dialog = new ProgressDialog (Catalog.GetString ("Exporting files"),
							      ProgressDialog.CancelButtonType.Stop,
							      photos.Length, zipdiag);

			//Pack up
			for (int i = 0; i < photos.Length; i ++) {
				if (progress_dialog.Update (String.Format (Catalog.GetString ("Preparing photo \"{0}\""), photos[i].Name))) {
					progress_dialog.Destroy ();
					return;
				}
				string f = null;
				// FIXME: embed in a try/catch
				if (scale_check.Active) {
					FilterSet filters = new FilterSet ();
					filters.Add (new JpegFilter ());
					filters.Add (new ResizeFilter ((uint) scale_size.ValueAsInt));
					FilterRequest freq = new FilterRequest (photos [i].DefaultVersionUri);
					filters.Convert (freq);
					f = freq.Current.LocalPath;
				} else {
					f = photos [i].DefaultVersionUri.LocalPath;
				}
				FileStream fs = File.OpenRead (f);

				byte [] buffer = new byte [fs.Length];
				fs.Read (buffer, 0, buffer.Length);
				ZipEntry entry = new ZipEntry (System.IO.Path.GetFileName (photos [i].DefaultVersionUri.LocalPath));

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

		private void on_filename_change (object sender, System.EventArgs args) {
			create_button.Sensitive = System.Text.RegularExpressions.Regex.IsMatch (filename.Text, "[.]zip$");
		}

		private void on_scalecheck_change (object sender, System.EventArgs args) {
			scale_size.Sensitive = scale_check.Active;
		}
	}
}
