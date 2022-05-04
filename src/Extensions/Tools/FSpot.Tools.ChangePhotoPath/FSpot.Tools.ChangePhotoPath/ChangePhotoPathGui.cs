//
// ChangePhotoPathGui.cs
//
// Author:
//   Stephane Delcroix <sdelcroix*novell.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

//
// ChangePhotoPath.IChangePhotoPathGui.cs: The Gui to change the photo path in photos.db
//
// Author:
//   Bengt Thuree (bengt@thuree.com)
//
// Copyright (C) 2007
//

using System;

using FSpot.Extensions;
using FSpot.UI.Dialog;

using Hyena.Widgets;



namespace FSpot.Tools.ChangePhotoPath
{
	public class Dump : Gtk.Dialog, ICommand, IChangePhotoPathGui
	{
		string dialog_name = "ChangePhotoPath";
		GtkBeans.Builder builder;
		Gtk.Dialog dialog;
		ChangePathController contr;

		ProgressDialog progress_dialog;
		int progress_dialog_total = 0;

#pragma warning disable 649
		[GtkBeans.Builder.Object] Gtk.Entry old_common_uri;
		[GtkBeans.Builder.Object] Gtk.Label new_common_uri;
#pragma warning restore 649

		bool LaunchController ()
		{
			try {
				contr = new ChangePathController (this);
			} catch (Exception e) {
				Logger.Log.Error (e, "");
				return false;
			}
			return true;
		}

		public void create_progress_dialog (string txt, int total)
		{
			progress_dialog = new ProgressDialog (txt,
								  ProgressDialog.CancelButtonType.Stop,
								  total,
								  dialog);
		}


		public void LaunchDialog ()
		{
			CreateDialog ();
			Dialog.Modal = false;
			Dialog.TransientFor = null;
			if (LaunchController () && contr.CanWeRun ()) {
				DisplayDoNotStopFSpotMsg ();
				Dialog.ShowAll ();
				Dialog.Response += HandleResponse;
			} else {
				DisplayOrigBasePathNotFoundMsg ();
				Dialog.Destroy ();
			}
		}

		void CreateDialog ()
		{
			builder = new GtkBeans.Builder (null, "ChangePhotoPath.ui", null);
			builder.Autoconnect (this);
		}

		Gtk.Dialog Dialog {
			get {
				if (dialog == null)
					dialog = new Gtk.Dialog (builder.GetRawObject (dialog_name));
				return dialog;
			}
		}

		void DisplayMsg (Gtk.MessageType MessageType, string msg)
		{

			HigMessageDialog.RunHigMessageDialog (null,
								Gtk.DialogFlags.Modal | Gtk.DialogFlags.DestroyWithParent,
								MessageType,
								Gtk.ButtonsType.Ok,
								msg,
								null);
		}

		void DisplayDoNotStopFSpotMsg ()
		{
			DisplayMsg (Gtk.MessageType.Info, "It will take a long time for SqLite to update the database if you have many photos." +
							  "\nWe recommend you to let F-Spot be running during the night to ensure everything is written to disk." +
							  "\nChanging path on 23000 photos took 2 hours until sqlite had updated all photos in the database.");
		}

		void DisplayOrigBasePathNotFoundMsg ()
		{
			DisplayMsg (Gtk.MessageType.Error, "Could not find an old base path. /YYYY/MM/DD need to start with /20, /19 or /18.");
		}

		void DisplayCancelledMsg ()
		{
			DisplayMsg (Gtk.MessageType.Warning, "Operation aborted. Database has not been modified.");
		}

		void DisplaySamePathMsg ()
		{
			DisplayMsg (Gtk.MessageType.Warning, "New and Old base path are the same.");
		}

		void DisplayNoPhotosFoundMsg ()
		{
			DisplayMsg (Gtk.MessageType.Warning, "Did not find any photos with the old base path.");
		}

		void DisplayExecutionOkMsg ()
		{
			DisplayMsg (Gtk.MessageType.Info, "Completed successfully. Please ensure you wait 1-2 hour before you exit f-spot. This to ensure the database cache is written to disk.");
		}

		void DisplayExecutionNotOkMsg ()
		{
			DisplayMsg (Gtk.MessageType.Error, "An error occurred. Reverted all changes to the database.");
		}


		void HandleResponse (object sender, Gtk.ResponseArgs args)
		{
			bool destroy_dialog = false;
			ChangePhotoPath.ProcessResult tmp_res;
			if (args.ResponseId == Gtk.ResponseType.Ok) {

				tmp_res = contr.ChangePathOnPhotos (old_common_uri.Text, new_common_uri.Text);
				switch (tmp_res) {
				case ProcessResult.Ok:
					DisplayExecutionOkMsg ();
					destroy_dialog = true;
					break;
				case ProcessResult.Cancelled:
					DisplayCancelledMsg ();
					break;
				case ProcessResult.Error:
					DisplayExecutionNotOkMsg ();
					break;
				case ProcessResult.SamePath:
					DisplaySamePathMsg ();
					break;
				case ProcessResult.NoPhotosFound:
					DisplayNoPhotosFoundMsg ();
					break;
				case ProcessResult.Processing:
					Logger.Log.Debug ("processing");
					break;
				}
			} else
				destroy_dialog = true;

			remove_progress_dialog ();
			if (destroy_dialog)
				Dialog.Destroy ();

			return;
		}

		public void DisplayDefaultPaths (string oldpath, string newpath)
		{
			old_common_uri.Text = oldpath;
			new_common_uri.Text = newpath;
		}

		public void remove_progress_dialog ()
		{
			if (progress_dialog != null) {
				progress_dialog.Destroy ();
				progress_dialog = null;
			}
		}

		public void check_if_remove_progress_dialog (int total)
		{
			if (total != progress_dialog_total)
				remove_progress_dialog ();
		}


		public bool UpdateProgressBar (string hdr_txt, string txt, int total)
		{
			if (progress_dialog != null)
				check_if_remove_progress_dialog (total);
			if (progress_dialog == null)
				create_progress_dialog (hdr_txt, total);
			progress_dialog_total = total;
			return progress_dialog.Update (string.Format ("{0} ", txt));
		}

		public void Run (object sender, EventArgs args)
		{
			try {
				LaunchDialog ();
			} catch (Exception e) {
				Logger.Log.Error (e, "");
			}
		}
	}
}
