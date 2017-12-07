//
// SendEmail.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Paul Lange <palango@gmx.de>
//   Bengt Thuree <bengt@thuree.com>
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2006-2010 Novell, Inc.
// Copyright (C) 2009-2010 Ruben Vermeersch
// Copyright (C) 2010 Paul Lange
// Copyright (C) 2006 Bengt Thuree
// Copyright (C) 2007-2009 Stephane Delcroix
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

using Gtk;
using GLib;
using System;

using FSpot.Core;
using FSpot.Widgets;
using FSpot.Filters;
using FSpot.UI.Dialog;
using FSpot.Settings;

using Hyena;
using Hyena.Widgets;
using Mono.Unix;

namespace FSpot
{
	public class SendEmail : BuilderDialog
	{
		readonly Window parent_window;

#pragma warning disable 0649
		[GtkBeans.Builder.Object] Gtk.ScrolledWindow tray_scrolled;
		[GtkBeans.Builder.Object] Label NumberOfPictures, TotalOriginalSize, ApproxNewSize;
		[GtkBeans.Builder.Object] RadioButton tiny_size, small_size, medium_size, large_size, x_large_size, original_size;
#pragma warning restore 0649

		long Orig_Photo_Size = 0;
		double scale_percentage = 0.3;

		// The different sizes we can shrink to foto to. See RadioButton above for labels.
		static int[] sizes = { 0, 320, 480, 640, 800, 1024 };

		// Estimated size relative to original after shrinking down the photo.
		double[] avg_scale_ref = { 0, 0.0186,	0.0348,	0.0532,	0.0826,	0.1234 };

		static int NoOfSizes = sizes.Length;
		double[] avg_scale = new double [NoOfSizes];
		string tmp_mail_dir;	// To temporary keep the resized images
		bool force_original;
		readonly IBrowsableCollection selection;

		public SendEmail (IBrowsableCollection selection, Window parent_window) : base ("mail_dialog.ui", "mail_dialog")
		{
			this.selection = selection;
			this.parent_window = parent_window;

			foreach (var p in selection.Items) {
				if (FileFactory.NewForUri (p.DefaultVersion.Uri).QueryInfo ("standard::content-type", FileQueryInfoFlags.None, null).ContentType != "image/jpeg")
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


			tray_scrolled.Add (new TrayView (selection));

			Modal = false;

			// Calculate total original filesize
			foreach (var photo in selection.Items) {
				try {
					Orig_Photo_Size += FileFactory.NewForUri (photo.DefaultVersion.Uri).QueryInfo ("standard::size", FileQueryInfoFlags.None, null).Size;
				} catch {
				}
			}

			for (int k = 0; k < avg_scale_ref.Length; k++)
				avg_scale[k] = avg_scale_ref[k];


			// Calculate approximate size shrinking, use first photo, and shrink to medium size as base.
			var scalephoto = selection [0];
			if (scalephoto != null && !force_original) {

				// Get first photos file size
				long orig_size = FileFactory.NewForUri (scalephoto.DefaultVersion.Uri).QueryInfo ("standard::size", FileQueryInfoFlags.None, null).Size;

				var filters = new FilterSet ();
				filters.Add (new ResizeFilter ((uint)(sizes [3])));
				long new_size;
				using (FilterRequest request = new FilterRequest (scalephoto.DefaultVersion.Uri)) {
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

			ShowAll ();

			//LoadHistory ();

			Response += HandleResponse;
		}

		int GetScaleSize()
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

		int GetScaleIndex ()
		{
			int scale = GetScaleSize();
			for (int k = 0; k < sizes.Length; k++)
				if (sizes[k] == scale)
					return k;
			return 0;
		}

		void UpdateEstimatedSize()
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
		}

		void HandleResponse (object sender, Gtk.ResponseArgs args)
		{
			int size = 0;
			bool UserCancelled = false;

			// Lets remove the mail "create mail" dialog
			Destroy();

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
			default://evolution falls into default, since it supports mailto uri correctly
				attach_arg.Append("&attach=");
				break;
			}

			// Create a tmp directory.
			tmp_mail_dir = System.IO.Path.GetTempFileName ();	// Create a tmp file
			System.IO.File.Delete (tmp_mail_dir);			// Delete above tmp file
			System.IO.Directory.CreateDirectory (tmp_mail_dir);	// Create a directory with above tmp name

			System.Text.StringBuilder mail_attach = new System.Text.StringBuilder ();

			FilterSet filters = new FilterSet ();

			if (size != 0)
				filters.Add (new ResizeFilter ((uint) size));
			filters.Add (new UniqueNameFilter (new SafeUri (tmp_mail_dir)));


			for (int i = 0; i < selection.Count; i++) {
				var photo = selection [i];
				if ( (photo != null) && (!UserCancelled) ) {

					if (progress_dialog != null)
						UserCancelled = progress_dialog.Update (string.Format
							(Catalog.GetString ("Exporting picture \"{0}\""), photo.Name));

					if (UserCancelled)
						break;

					try {
						// Prepare a tmp_mail file name
						FilterRequest request = new FilterRequest (photo.DefaultVersion.Uri);

						filters.Convert (request);
						request.Preserve(request.Current);

						mail_attach.Append(((i == 0 && attach_arg.ToString () == ",") ? "" : attach_arg.ToString()) + request.Current.ToString ());
					} catch (Exception e) {
						Hyena.Log.ErrorFormat ("Error preparing {0}: {1}", selection[i].Name, e.Message);
						HigMessageDialog md = new HigMessageDialog (parent_window,
											    DialogFlags.DestroyWithParent,
											    MessageType.Error,
											    ButtonsType.Close,
											    Catalog.GetString("Error processing image"),
											    string.Format(Catalog.GetString("An error occurred while processing \"{0}\": {1}"), selection[i].Name, e.Message));
						md.Run();
						md.Destroy();
						UserCancelled = true;
					}
				}
			} // foreach

			if (progress_dialog != null)
				progress_dialog.Destroy (); // No need to keep this window

		    if (UserCancelled)
                return;

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
		            GtkBeans.Global.ShowUri (Screen, "mailto:?subject=" + mail_subject + mail_attach);
		            break;
		        default:
		            GtkBeans.Global.ShowUri (Screen, "mailto:?subject=" + System.Web.HttpUtility.UrlEncode(mail_subject) + mail_attach);
		            break;
		    }
		}
	}
}
