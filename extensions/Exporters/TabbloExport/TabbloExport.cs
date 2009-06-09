//
// FSpotTabbloExport.TabbloExport
//
// Authors:
//	Wojciech Dzierzanowski (wojciech.dzierzanowski@gmail.com)
//
// (C) Copyright 2009 Wojciech Dzierzanowski
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using Mono.Tabblo;
using Mono.Unix;

using System;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Threading;

using FSpot.Utils;
using FSpot.UI.Dialog;

namespace FSpotTabbloExport {

	public class TabbloExport : FSpot.Extensions.IExporter {

		private readonly TabbloExportModel model;

		private TabbloExportView main_dialog;
		private ThreadProgressDialog progress_dialog;

		private readonly Uploader uploader;
		private TraceListener debug_listener;
		private TotalUploadProgress upload_progress;


		//
		// Initialization
		//

		public TabbloExport ()
		{
			model = new TabbloExportModel ();
			uploader = new Uploader (model);
		}


		public void Run (FSpot.IBrowsableCollection photos)
		{
			if (null == photos || null == photos.Items) {
				throw new ArgumentNullException ("photos");
			}

			main_dialog = new TabbloExportView (photos);

			InitBindings ();

			model.Deserialize ();
			model.PhotoCollection = photos;

			// Model deserialization triggers the various event
			// handlers, which can cause the default widget to lose
			// focus (it can be invalid, and hence disabled, for a
			// moment).
			main_dialog.ResetFocus ();
		}


		private void InitBindings ()
		{
			Debug.Assert (null != model);
			Debug.Assert (null != main_dialog);

			main_dialog.tabblo_export_dialog.Response +=
					HandleResponse;

			// Account data
			model.UsernameChanged += HandleUsernameChanged;
			main_dialog.username_entry.Changed +=
					HandleUsernameChanged;

			model.PasswordChanged += HandlePasswordChanged;
			main_dialog.password_entry.Changed +=
					HandlePasswordChanged;
		}


		//
		// Event handlers
		//

		private void HandleUsernameChanged (object sender,
		                                    EventArgs args)
		{
			if (model == sender) {
				main_dialog.username_entry.Text = model.Username;
				OnAccountDataChanged ();
			} else {
				model.Username = main_dialog.username_entry.Text;
			}
		}

		private void HandlePasswordChanged (object sender,
		                                    EventArgs args)
		{
			if (model == sender) {
				main_dialog.password_entry.Text = model.Password;
				OnAccountDataChanged ();
			} else {
				model.Password = main_dialog.password_entry.Text;
			}
		}

		private void OnAccountDataChanged ()
		{
			main_dialog.Validated =
					model.Username.Length > 0
					&& model.Password.Length > 0;
		}


		private void HandleResponse (object sender,
		                             Gtk.ResponseArgs args)
		{
			main_dialog.Destroy ();

			if (Gtk.ResponseType.Ok != args.ResponseId) {
				Log.Information ("Tabblo export was canceled.");
				return;
			}
			
			model.Serialize ();
			
			Log.Information ("Starting Tabblo export");
			
			Thread upload_thread =
					new Thread (new ThreadStart (Upload));
			progress_dialog = new ThreadProgressDialog (
					upload_thread, model.Photos.Length);
			progress_dialog.Start ();
		}
		
		
		//
		// Upload logic
		// 

		private void Upload ()
		{
			Debug.Assert (null != uploader);
			
			Picture [] pictures = GetPicturesForUpload (); 
			Debug.Assert (pictures.Length == model.Photos.Length);

			OnUploadStarted (pictures);
				
			try {
				for (int i = 0; i < pictures.Length; ++i) {
					uploader.Upload (pictures [i]);
					OnPhotoUploaded (model.Photos [i]);
				}
				
				progress_dialog.Message = Catalog.GetString (
						"Done sending photos");
				progress_dialog.ProgressText = Catalog
						.GetString ("Upload complete");
				progress_dialog.Fraction = 1;
				progress_dialog.ButtonLabel = Gtk.Stock.Ok;
				
			} catch (TabbloException e) {
				progress_dialog.Message = Catalog.GetString (
						"Error uploading to Tabblo: ")
						+ e.Message;
				progress_dialog.ProgressText =
						Catalog.GetString ("Error");
				// FIXME:  Retry logic? 
//				  progressDialog.PerformRetrySkip ();
				Console.WriteLine (e);
			} finally {
				OnUploadFinished ();
			}
		}
		

		private void OnUploadStarted (Picture [] pictures)
		{
			Debug.Assert (null != pictures);
			Debug.Assert (null != uploader);
			Debug.Assert (null != progress_dialog);

			// Initialize the debug output listener.
			// `Mono.Tabblo' uses the standard
			// `System.Diagnostics.Debug' facilities for debug
			// output only.
			debug_listener = new FSpotTraceListener ();
			Debug.Listeners.Add (debug_listener);

			// Initialize the progress handler.
			upload_progress = new FSpotUploadProgress (
					pictures, progress_dialog);
			uploader.ProgressChanged +=
					upload_progress.HandleProgress;
			
			// Set up the certificate policy.
			ServicePointManager.CertificatePolicy =
					new UserDecisionCertificatePolicy ();
		}

		private void OnUploadFinished ()
		{
			Debug.Assert (null != uploader);
			Debug.Assert (null != debug_listener);
			Debug.Assert (null != upload_progress);

			uploader.ProgressChanged -=
					upload_progress.HandleProgress;

			Debug.Listeners.Remove (debug_listener);
		}

		
		private void OnPhotoUploaded (FSpot.IBrowsableItem item)
		{
			Debug.Assert (null != item);
		}

		
		private Picture [] GetPicturesForUpload ()
		{
			Picture [] pictures = new Picture [model.Photos.Length];
			FSpot.IBrowsableItem [] items = model.Photos;

			for (int i = 0; i < pictures.Length; ++i) {
				// FIXME: GnomeVFS is deprecated, we should use
				// GIO instead.  However, I don't know how to
				// call `GLib.Content.TypeGuess ()'.
				string path = items [i].DefaultVersionUri
						.LocalPath;
				string mime_type = Gnome.Vfs.MimeType
						.GetMimeTypeForUri (path);

				pictures [i] = new Picture (items [i].Name,
						items [i].DefaultVersionUri,
						mime_type,
						model.Privacy);
			}

			return pictures;
		}
	}
}
