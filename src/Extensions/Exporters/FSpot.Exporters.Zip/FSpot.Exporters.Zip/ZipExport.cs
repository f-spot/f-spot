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

using FSpot;
using FSpot.UI.Dialog;
using FSpot.Core;
using FSpot.Extensions;
using FSpot.Filters;

using Hyena;
using Hyena.Widgets;

using Mono.Unix;

using Gtk;
using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip;
using System.Linq;

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

		IPhoto [] photos;
		Gtk.FileChooserButton uri_chooser;

		public void Run (IBrowsableCollection p) {
			Log.Information ("Executing ZipExport extension");
			if (p.Count == 0) {
				HigMessageDialog md = new HigMessageDialog (App.Instance.Organizer.Window, DialogFlags.DestroyWithParent,
							  Gtk.MessageType.Error, ButtonsType.Ok,
							  Catalog.GetString ("No selection available"),
							  Catalog.GetString ("This tool requires an active selection. Please select one or more pictures and try again"));

				md.Run ();
				md.Destroy ();
				return;
			}
			photos = p.Items.ToArray ();
			ShowDialog ();
		}

		public void ShowDialog () {
			var builder = new GtkBeans.Builder (null, "zip_export.ui", null);
			builder.Autoconnect (this);
			zipdiag.Modal = false;
			zipdiag.TransientFor = null;

			uri_chooser = new Gtk.FileChooserButton (Catalog.GetString ("Select export folder"),
								 Gtk.FileChooserAction.SelectFolder);
			uri_chooser.LocalOnly = true;
			uri_chooser.SetFilename (System.IO.Path.Combine (FSpot.Settings.Global.HomeDirectory, "Desktop"));
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
			Log.DebugFormat ("Creating zip file {0}", filedest);
			ZipOutputStream s = new ZipOutputStream (File.Create(filedest));
			if (scale_check.Active)
				Log.DebugFormat ("Scaling to {0}", scale_size.ValueAsInt);

			ProgressDialog progress_dialog = new ProgressDialog (Catalog.GetString ("Exporting files"),
							      ProgressDialog.CancelButtonType.Stop,
							      photos.Length, zipdiag);

			//Pack up
			for (int i = 0; i < photos.Length; i ++) {
				if (progress_dialog.Update (string.Format (Catalog.GetString ("Preparing photo \"{0}\""), photos[i].Name))) {
					progress_dialog.Destroy ();
					return;
				}
				string f = null;
				// FIXME: embed in a try/catch
				if (scale_check.Active) {
					FilterSet filters = new FilterSet ();
					filters.Add (new JpegFilter ());
					filters.Add (new ResizeFilter ((uint) scale_size.ValueAsInt));
					FilterRequest freq = new FilterRequest (photos [i].DefaultVersion.Uri);
					filters.Convert (freq);
					f = freq.Current.LocalPath;
				} else {
					f = photos [i].DefaultVersion.Uri.LocalPath;
				}
				FileStream fs = File.OpenRead (f);

				byte [] buffer = new byte [fs.Length];
				fs.Read (buffer, 0, buffer.Length);
				ZipEntry entry = new ZipEntry (System.IO.Path.GetFileName (photos [i].DefaultVersion.Uri.LocalPath));

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
