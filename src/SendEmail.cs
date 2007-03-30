//
// SendEmail.cs: Lets user resize photos prior to sending them by mail.	
//
// 	This function uses the following preferenses in gconf.
//		/apps/f-spot/export/email/size
//		/apps/f-spot/export/email/auto_rotate
//		/apps/f-spot/export/email/delete_timeout_seconds
//	
// Author:
//   Bengt Thuree (bengt@thuree.com)
//
// (C) 2006 Bengt Thuree
// 


using Gtk;
using Gnome;
using System;

using FSpot.Filters;
using Mono.Unix;

namespace FSpot {
	public class SendEmail : FSpot.GladeDialog {
		PhotoQuery query;
		Gtk.Window parent_window;

		[Glade.Widget] private ScrolledWindow   tray_scrolled;
		[Glade.Widget] private Button 		ok_button;
		[Glade.Widget] private Label 		NumberOfPictures, TotalOriginalSize, ApproxNewSize;	
		[Glade.Widget] private RadioButton 	tiny_size, small_size, medium_size, 
							large_size, x_large_size, original_size;
		[Glade.Widget] private CheckButton 	rotate_check;

		int photo_index;
		bool clean;
		long Orig_Photo_Size 	= 0;
		double scale_percentage = 0.3;
		
		// The different sizes we can shrink to foto to. See RadioButton above for labels.
		static int[] sizes 		= { 0, 320, 	480, 	640, 	800, 	1024 };
		
		// Estimated size relative to original after shrinking down the photo. 
		double[] avg_scale_ref 	= { 0, 0.0186,	0.0348,	0.0532,	0.0826,	0.1234 };
		
		static int NoOfSizes	= sizes.Length;
		double[] avg_scale	= new double [NoOfSizes];
		System.Collections.ArrayList tmp_paths; // temporary resized image file name
		string tmp_mail_dir;	// To temporary keep the resized images

		ThreadProgressDialog progress_dialog;
		System.Threading.Thread command_thread;
		IBrowsableCollection selection;

		public SendEmail (IBrowsableCollection selection) : base ("mail_dialog")
		{
			this.selection = selection;
			
			// Set default values in dialog. Fetch from Preferences.
			switch ((int) Preferences.Get (Preferences.EXPORT_EMAIL_SIZE)) {
				case 0 :  original_size.Active = true; break;
				case 1 :  tiny_size.Active = true; break;
				case 2 :  small_size.Active = true; break;
				case 3 :  medium_size.Active = true; break;
				case 4 :  large_size.Active = true; break;
				case 5 :  x_large_size.Active = true; break;
				default: break;
			}
			rotate_check.Active = (bool) Preferences.Get (Preferences.EXPORT_EMAIL_ROTATE);
			rotate_check.Sensitive = original_size.Active;
			
			tray_scrolled.Add (new TrayView (selection));

			Dialog.Modal = false;

			string path;
			System.IO.FileInfo file_info;

			// Calculate total original filesize 
			foreach (Photo photo in selection.Items) {
				if (photo != null) {
					path = photo.GetVersionPath(photo.DefaultVersionId);
					if (System.IO.File.Exists (path)) {
						file_info = new System.IO.FileInfo (path);
						Orig_Photo_Size += file_info.Length;
					} // if file exists
				} // if photo != null
			} // foreach

			for (int k = 0; k < avg_scale_ref.Length; k++)
				avg_scale[k] = avg_scale_ref[k];


			// Calculate approximate size shrinking, use first photo, and shrink to medium size as base.
			Photo scalephoto = selection.Items [0] as Photo;			
			if (scalephoto != null) {
				
				// Get first photos file size
				file_info = new System.IO.FileInfo (scalephoto.GetVersionPath(scalephoto.DefaultVersionId));
				long orig_size = file_info.Length;
				
				// Get filesize of first photo after resizing it to medium size
				path = PixbufUtils.Resize (scalephoto.GetVersionPath(scalephoto.DefaultVersionId), sizes[3], true);
				file_info = new System.IO.FileInfo (path);
				long new_size = file_info.Length;
				
				// Delete the just created temporary resized photo
				System.IO.File.Delete (path);
				
				if (orig_size > 0) {
				
					// Get the factor (scale) between original and resized medium size.
					scale_percentage = 1 - ( (float) (orig_size - new_size) / orig_size);
					
					// What is the relation between the estimated medium scale factor, and reality?
					double scale_scale = scale_percentage / avg_scale_ref[3];
					
					//System.Console.WriteLine ("scale_percentage {0}, ref {1}, relative {2}", 
					//	scale_percentage, avg_scale_ref[3], scale_scale  );

					// Re-Calculate the proper relation per size
					for (int k = 0; k < avg_scale_ref.Length; k++) {
						avg_scale[k] = avg_scale_ref[k] * scale_scale;
					//	System.Console.WriteLine ("avg_scale[{0}]={1} (was {2})", 
					//		k, avg_scale[k], avg_scale_ref[k]  );
					}
				}

			}

			NumberOfPictures.Text 	= selection.Count.ToString();
			TotalOriginalSize.Text 	= SizeUtil.ToHumanReadable (Orig_Photo_Size);
			
			UpdateEstimatedSize();

			Dialog.ShowAll ();

			//LoadHistory ();

			Dialog.Response += HandleResponse;
		}

		private int GetScaleSize()
		{
			// not only convert dialog size to pixel size, but also set preferences se we use same size next time
			int size_number = 0; // default to original size
			if (tiny_size.Active) 
				size_number = 1;
			if (small_size.Active) 
				size_number = 2;
			if (medium_size.Active) 
				size_number = 3;
			if (large_size.Active) 
				size_number = 4;
			if (x_large_size.Active) 
				size_number = 5;
			
			Preferences.Set (Preferences.EXPORT_EMAIL_SIZE, size_number);			
			return sizes [ size_number ];		
		}
		
		private int GetScaleIndex ()
		{
			int scale = GetScaleSize();
			for (int k = 0; k < sizes.Length; k++)
				if (sizes[k] == scale)
					return k;
			return 0;
		}

		private void UpdateEstimatedSize()
		{
				int new_size_index;
				long new_approx_total_size;
				string approxresult;
				
				new_size_index = GetScaleIndex();
				if (new_size_index == 0)
					new_approx_total_size = Orig_Photo_Size;
				else
					new_approx_total_size = System.Convert.ToInt64(Orig_Photo_Size * avg_scale [new_size_index]);

				approxresult = SizeUtil.ToHumanReadable (new_approx_total_size);
				ApproxNewSize.Text 	= approxresult;	

		}

		public void on_size_toggled (object o, EventArgs args) 
		{
			UpdateEstimatedSize();
			
			// Only enable the rotate option if Original size is selected.
			rotate_check.Sensitive = original_size.Active;
		}


		private bool DeleteTempFile ()
		{
//			System.Console.WriteLine ("Lets delete all temp files");

			// Lets delete all the temporary files now
			for (int k = 0; k < tmp_paths.Count; k++) {
				if (System.IO.File.Exists((string) tmp_paths[k])) {
					System.IO.File.Delete ((string) tmp_paths[k]);
//					System.Console.WriteLine ("Lets delete temp file {0}", tmp_paths[k]);
				}
			}
			
			if (System.IO.Directory.Exists(tmp_mail_dir)) {
				System.IO.Directory.Delete(tmp_mail_dir);
//				System.Console.WriteLine ("Lets delete temp dir {0}", tmp_mail_dir);
			}
			
			return false;
		}

		private void HandleResponse (object sender, Gtk.ResponseArgs args)
		{
			long new_size = 0;
//			long orig_size = 0;
			long actual_total_size = 0;
			int size = 0;
			System.IO.FileInfo file_info;
			bool UserCancelled = false;
			bool rotate = true;

			// Lets remove the mail "create mail" dialog
			Dialog.Destroy();			

			if (args.ResponseId != Gtk.ResponseType.Ok) {
				return;
			}
			ProgressDialog progress_dialog = null;
			actual_total_size = 0;
		
			progress_dialog = new ProgressDialog (Catalog.GetString ("Preparing email"),
							      ProgressDialog.CancelButtonType.Stop,
							      selection.Items.Length, 
							      parent_window);
			
			size = GetScaleSize(); // Which size should we scale to. 0 --> Original
			
			// evaluate mailto command and define attachment args for cli
			System.Text.StringBuilder attach_arg = new System.Text.StringBuilder ();
			switch (Preferences.Get (Preferences.GNOME_MAILTO_COMMAND) as string) {
				case "thunderbird %s":
				case "mozilla-thunderbird %s":
				case "seamonkey -mail -compose %s":
				case "icedove %s":
					attach_arg.Append(",");
				break;
				case "kmail %s":
					attach_arg.Append(" --attach ");
				break;
				default:  //evolution falls into default, since it supports mailto uri correctly
					attach_arg.Append("&attach=");
				break;
			}

			rotate = rotate_check.Active;  // Should we automatically rotate original photos.
			Preferences.Set (Preferences.EXPORT_EMAIL_ROTATE, rotate);

			// Initiate storage for temporary files to be deleted later
			tmp_paths = new System.Collections.ArrayList();
			
			// Create a tmp directory.
			tmp_mail_dir = System.IO.Path.GetTempFileName ();	// Create a tmp file	
			System.IO.File.Delete (tmp_mail_dir);			// Delete above tmp file
			System.IO.Directory.CreateDirectory (tmp_mail_dir);	// Create a directory with above tmp name
			
			System.Text.StringBuilder mail_attach = new System.Text.StringBuilder ();

			FilterSet filters = new FilterSet ();

			if (size != 0)
				filters.Add (new ResizeFilter ((uint) size));
			else if (rotate)
				filters.Add (new OrientationFilter ());
			filters.Add (new UniqueNameFilter (tmp_mail_dir));


			foreach (Photo photo in selection.Items) {
			
				if ( (photo != null) && (!UserCancelled) ) {

					if (progress_dialog != null)
						UserCancelled = progress_dialog.Update (String.Format 
							(Catalog.GetString ("Exporting picture \"{0}\""), photo.Name));
							
					if (UserCancelled)
					 	break;
					 	
					file_info = new System.IO.FileInfo (photo.GetVersionPath(photo.DefaultVersionId));
//					orig_size = file_info.Length;

					// Prepare a tmp_mail file name
					FilterRequest request = new FilterRequest (photo.DefaultVersionUri);

					filters.Convert (request);
					request.Preserve(request.Current);

 					mail_attach.Append(attach_arg.ToString() + System.Web.HttpUtility.UrlEncode (request.Current.ToString()));
					
					// Mark the path for deletion
					tmp_paths.Add (request.Current.LocalPath);

					// Update the running total of the actual file sizes.
					file_info = new System.IO.FileInfo (request.Current.LocalPath);
					new_size = file_info.Length;
					actual_total_size += new_size;

					// Update dialog to indicate Actual size!
					// This is currently disabled, since the dialog box is not visible at this stage.
					// string approxresult = SizeUtil.ToHumanReadable (actual_total_size);
					// ActualMailSize.Text = approxresult;	


					//System.Console.WriteLine ("Orig file size {0}, New file size {1}, % {4}, Scaled to size {2}, new name {3}", 
					//orig_size, new_size, size, tmp_path, 1 - ((orig_size-new_size)/orig_size));
				}
			} // foreach
			
			if (progress_dialog != null) 
				progress_dialog.Destroy (); // No need to keep this window

			if (UserCancelled)
				DeleteTempFile();
			else {		
				// Send the mail :)
				switch (Preferences.Get (Preferences.GNOME_MAILTO_COMMAND) as string) {
					// openSuSE
					case "thunderbird %s":
						System.Diagnostics.Process.Start("thunderbird", " -compose \"subject=my photos,attachment='" + mail_attach + "'\"");
					break;
					case "icedove %s":
						System.Diagnostics.Process.Start("thunderbird", " -compose \"subject=my photos,attachment='" + mail_attach + "'\"");
					break;
					case "mozilla-thunderbird %s":
						System.Diagnostics.Process.Start("mozilla-thunderbird", " -compose \"subject=my photos,attachment='" + mail_attach + "'\"");
					break;
					case "seamonkey -mail -compose %s":
						System.Diagnostics.Process.Start("seamonkey", " -mail -compose \"subject=my photos,attachment='" + mail_attach + "'\"");
					break;
					case "kmail %s":
						System.Diagnostics.Process.Start("kmail", "  --composer --subject \"my photos\"" + mail_attach);
					break;
					default: 
						GnomeUtil.UrlShow (parent_window,"mailto:?subject=my%20photos" + mail_attach);
					break;
				}
				                
				// Check if we have any temporary files to be deleted
				if (tmp_paths.Count > 0) {
					// Fetch timeout value from preferences. In seconds. Needs to be multiplied with 1000 to get msec
					uint delete_timeout;
					delete_timeout = (uint) ( (int) Preferences.Get (Preferences.EXPORT_EMAIL_DELETE_TIMEOUT_SEC) );
					delete_timeout = delete_timeout * 1000; // to get milliseconds.

					// Start a timer and when it occurs, delete the temp files.
					GLib.Timeout.Add (delete_timeout, new GLib.TimeoutHandler (DeleteTempFile));
				}
			}
		}
	}
}
