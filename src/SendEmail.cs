/*
 * FSpot.SendEmail
 *
 * Author(s)
 * 	Bengt Thuree  <bengt@thuree.com>
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using Gtk;
using GLib;
using System;

using FSpot.Widgets;
using FSpot.Filters;
using FSpot.Utils;
using FSpot.UI.Dialog;

using Mono.Unix;

namespace FSpot {
	public class SendEmail : GladeDialog {
		Window parent_window;
		PhotoQuery query;

		[Glade.Widget] private ScrolledWindow   tray_scrolled;
		[Glade.Widget] private Button 		ok_button;
		[Glade.Widget] private Label 		NumberOfPictures, TotalOriginalSize, ApproxNewSize;	
		[Glade.Widget] private RadioButton 	tiny_size, small_size, medium_size, 
							large_size, x_large_size, original_size;
		[Glade.Widget] private CheckButton 	rotate_check;

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
		bool force_original = false;

		ThreadProgressDialog progress_dialog;
		System.Threading.Thread command_thread;
		IBrowsableCollection selection;

		public SendEmail (IBrowsableCollection selection, Window parent_window) : base ("mail_dialog")
		{
			this.selection = selection;
			this.parent_window = parent_window;

			for (int i = 0; i < selection.Count; i++) {
				Photo p = selection[i] as Photo;
				if (FileFactory.NewForUri (p.DefaultVersionUri).QueryInfo ("standard::content-type", FileQueryInfoFlags.None, null).ContentType != "image/jpeg")
					force_original = true;
			}

			if (force_original) {
				original_size.Active = true;
				tiny_size.Sensitive = false;
				small_size.Sensitive = false;
				medium_size.Sensitive = false;
				large_size.Sensitive = false;
				x_large_size.Sensitive = false;
			} else  
				switch (Preferences.Get<int> (Preferences.EXPORT_EMAIL_SIZE)) {
					case 0 :  original_size.Active = true; break;
					case 1 :  tiny_size.Active = true; break;
					case 2 :  small_size.Active = true; break;
					case 3 :  medium_size.Active = true; break;
					case 4 :  large_size.Active = true; break;
					case 5 :  x_large_size.Active = true; break;
					default: break;
				}

			rotate_check.Active = Preferences.Get<bool> (Preferences.EXPORT_EMAIL_ROTATE);
			rotate_check.Sensitive = original_size.Active && tiny_size.Sensitive;
			
			tray_scrolled.Add (new TrayView (selection));

			Dialog.Modal = false;

			// Calculate total original filesize 
			for (int i = 0; i < selection.Count; i++) {
				Photo photo = selection[i] as Photo;
				try {
					Orig_Photo_Size += FileFactory.NewForUri (photo.DefaultVersionUri).QueryInfo ("standard::size", FileQueryInfoFlags.None, null).Size;
				} catch {
				}
			}

			for (int k = 0; k < avg_scale_ref.Length; k++)
				avg_scale[k] = avg_scale_ref[k];


			// Calculate approximate size shrinking, use first photo, and shrink to medium size as base.
			Photo scalephoto = selection [0] as Photo;
			if (scalephoto != null && !force_original) {
				
				// Get first photos file size
				long orig_size = FileFactory.NewForUri (scalephoto.DefaultVersionUri).QueryInfo ("standard::size", FileQueryInfoFlags.None, null).Size;
				
				FilterSet filters = new FilterSet ();
				filters.Add (new ResizeFilter ((uint)(sizes [3])));
				long new_size;
				using (FilterRequest request = new FilterRequest (scalephoto.DefaultVersionUri)) {
					filters.Convert (request);
					new_size = FileFactory.NewForUri (request.Current).QueryInfo ("standard::size", FileQueryInfoFlags.None, null).Size;
				}
				
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
			TotalOriginalSize.Text 	= GLib.Format.SizeForDisplay (Orig_Photo_Size);
			
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
			
			if (!force_original) 
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

				approxresult = GLib.Format.SizeForDisplay (new_approx_total_size);
				ApproxNewSize.Text 	= approxresult;	

		}

		public void on_size_toggled (object o, EventArgs args) 
		{
			UpdateEstimatedSize();
			
			// Only enable the rotate option if Original size is selected.
			rotate_check.Sensitive = original_size.Active;
		}


		private void HandleResponse (object sender, Gtk.ResponseArgs args)
		{
			int size = 0;
			bool UserCancelled = false;
			bool rotate = true;

			// Lets remove the mail "create mail" dialog
			Dialog.Destroy();			

			if (args.ResponseId != Gtk.ResponseType.Ok) {
				return;
			}
			ProgressDialog progress_dialog = null;
		
			progress_dialog = new ProgressDialog (Catalog.GetString ("Preparing email"),
							      ProgressDialog.CancelButtonType.Stop,
							      selection.Count,
							      parent_window);
			
			size = GetScaleSize(); // Which size should we scale to. 0 --> Original
			
			// evaluate mailto command and define attachment args for cli
			System.Text.StringBuilder attach_arg = new System.Text.StringBuilder ();
			switch (Preferences.Get<string> (Preferences.GNOME_MAILTO_COMMAND)) {
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


			for (int i = 0; i < selection.Count; i++) {
				Photo photo = selection [i] as Photo;
				if ( (photo != null) && (!UserCancelled) ) {

					if (progress_dialog != null)
						UserCancelled = progress_dialog.Update (String.Format 
							(Catalog.GetString ("Exporting picture \"{0}\""), photo.Name));
							
					if (UserCancelled)
					 	break;
					 	
					try {
						// Prepare a tmp_mail file name
						FilterRequest request = new FilterRequest (photo.DefaultVersionUri);

						filters.Convert (request);
						request.Preserve(request.Current);

						mail_attach.Append(((i == 0 && attach_arg.ToString () == ",") ? "" : attach_arg.ToString()) + request.Current.ToString ());
						
						// Mark the path for deletion
						tmp_paths.Add (request.Current.LocalPath);
					} catch (Exception e) {
						Console.WriteLine("Error preparing {0}: {1}", selection[i].Name, e.Message);
						HigMessageDialog md = new HigMessageDialog (parent_window, 
											    DialogFlags.DestroyWithParent,
											    MessageType.Error,
											    ButtonsType.Close,
											    Catalog.GetString("Error processing image"), 
											    String.Format(Catalog.GetString("An error occured while processing \"{0}\": {1}"), selection[i].Name, e.Message));
						md.Run();
						md.Destroy();
						UserCancelled = true;
					}
				}
			} // foreach
			
			if (progress_dialog != null) 
				progress_dialog.Destroy (); // No need to keep this window


			if (!UserCancelled) {
				// Send the mail :)
				string mail_subject = Catalog.GetString("My Photos");
				switch (Preferences.Get<string> (Preferences.GNOME_MAILTO_COMMAND)) {
				// openSuSE
				case "thunderbird %s":
					System.Diagnostics.Process.Start("thunderbird", " -compose \"subject=" + mail_subject + ",attachment='" + mail_attach + "'\"");
					break;
				case "icedove %s":
					System.Diagnostics.Process.Start("icedove", " -compose \"subject=" + mail_subject + ",attachment='" + mail_attach + "'\"");
					break;
				case "mozilla-thunderbird %s":
					System.Diagnostics.Process.Start("mozilla-thunderbird", " -compose \"subject=" + mail_subject + ",attachment='" + mail_attach + "'\"");
					break;
				case "seamonkey -mail -compose %s":
					System.Diagnostics.Process.Start("seamonkey", " -mail -compose \"subject=" + mail_subject + ",attachment='" + mail_attach + "'\"");
					break;
				case "kmail %s":
					System.Diagnostics.Process.Start("kmail", "  --composer --subject \"" + mail_subject + "\"" + mail_attach);
					break;
				case "evolution %s": //evo doesn't urldecode the subject
					GtkBeans.Global.ShowUri (Dialog.Screen, "mailto:?subject=" + mail_subject + mail_attach);
					break;
				default: 
					GtkBeans.Global.ShowUri (Dialog.Screen, "mailto:?subject=" + System.Web.HttpUtility.UrlEncode(mail_subject) + mail_attach);
					break;
				}
			}
		}
	}
}
