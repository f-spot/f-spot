//
// ChangePhotoPath.IChangePhotoPathGui.cs: The Gui to change the photo path in photos.db
//
// Author:
//   Bengt Thuree (bengt@thuree.com)
//
// Copyright (C) 2007
//

using FSpot.Extensions;
using FSpot.UI.Dialog;
using System;
//using Gnome.Vfs;
using Gtk;
using Hyena;

namespace ChangePhotoPath
{

	public class Dump : Gtk.Dialog, ICommand, IChangePhotoPathGui
	{
		private string dialog_name = "ChangePhotoPath";
		private Glade.XML xml;
		private Gtk.Dialog dialog;
		private ChangePathController contr;

		private ProgressDialog progress_dialog;
		private int progress_dialog_total = 0;

		[Glade.Widget] Gtk.Entry old_common_uri;
		[Glade.Widget] Gtk.Label new_common_uri;
//		[Glade.Widget] Gtk.ProgressBar progress_bar;

		private bool LaunchController()
		{
			try {
				contr = new ChangePathController ( this );
			} catch (Exception e) {
				Log.Exception(e);
				return false;
			}
			return true;
		}

		public void create_progress_dialog(string txt, int total)
		{
			progress_dialog = new ProgressDialog (txt,
							      ProgressDialog.CancelButtonType.Stop,
							      total,
							      dialog);
		}


		public void LaunchDialog()
		{
			CreateDialog();
			Dialog.Modal = false;
			Dialog.TransientFor = null;
			if (LaunchController() && contr.CanWeRun())
			{
				DisplayDoNotStopFSpotMsg();
				Dialog.ShowAll();
				Dialog.Response += HandleResponse;
			} else {
				DisplayOrigBasePathNotFoundMsg();
				Dialog.Destroy();
			}
		}

		private void CreateDialog()
		{
			xml = new Glade.XML (null, "ChangePhotoPath.glade", dialog_name, "f-spot");
			xml.Autoconnect (this);
		}

		private Gtk.Dialog Dialog {
			get {
				if (dialog == null)
					dialog = (Gtk.Dialog) xml.GetWidget (dialog_name);
				return dialog;
			}
		}

		private void DisplayMsg(Gtk.MessageType MessageType, string msg)
		{

			HigMessageDialog.RunHigMessageDialog (	null,
								Gtk.DialogFlags.Modal | Gtk.DialogFlags.DestroyWithParent,
								MessageType,
								Gtk.ButtonsType.Ok,
								msg,
								null);
		}

		private void DisplayDoNotStopFSpotMsg()
		{
			DisplayMsg (Gtk.MessageType.Info, "It will take a long time for SqLite to update the database if you have many photos." +
							  "\nWe recommend you to let F-Spot be running during the night to ensure everything is written to disk."+
							  "\nChanging path on 23000 photos took 2 hours until sqlite had updated all photos in the database.");
		}

		private void DisplayOrigBasePathNotFoundMsg()
		{
			DisplayMsg (Gtk.MessageType.Error, "Could not find an old base path. /YYYY/MM/DD need to start with /20, /19 or /18.");
		}

		private void DisplayCancelledMsg()
		{
			DisplayMsg (Gtk.MessageType.Warning, "Operation aborted. Database has not been modified.");
		}

		private void DisplaySamePathMsg()
		{
			DisplayMsg (Gtk.MessageType.Warning, "New and Old base path are the same.");
		}

		private void DisplayNoPhotosFoundMsg()
		{
			DisplayMsg (Gtk.MessageType.Warning, "Did not find any photos with the old base path.");
		}

		private void DisplayExecutionOkMsg()
		{
			DisplayMsg (Gtk.MessageType.Info, "Completed successfully. Please ensure you wait 1-2 hour before you exit f-spot. This to ensure the database cache is written to disk.");
		}

		private void DisplayExecutionNotOkMsg()
		{
			DisplayMsg (Gtk.MessageType.Error, "An error occured. Reverted all changes to the database.");
		}


		private void HandleResponse (object sender, Gtk.ResponseArgs args)
		{
			bool destroy_dialog = false;
			ChangePhotoPath.ProcessResult tmp_res;
			if (args.ResponseId == Gtk.ResponseType.Ok) {

				tmp_res = contr.ChangePathOnPhotos (old_common_uri.Text, new_common_uri.Text);
				switch (tmp_res) {
				case ProcessResult.Ok 			: 	DisplayExecutionOkMsg();
										destroy_dialog=true;
										break;
				case ProcessResult.Cancelled 		: 	DisplayCancelledMsg();
										break;
				case ProcessResult.Error 		: 	DisplayExecutionNotOkMsg();
										break;
				case ProcessResult.SamePath 		: 	DisplaySamePathMsg();
										break;
				case ProcessResult.NoPhotosFound 	: 	DisplayNoPhotosFoundMsg();
										break;
				case ProcessResult.Processing 		: 	Log.Debug ("processing");
										break;
				}
			} else
				destroy_dialog = true;

			remove_progress_dialog();
			if (destroy_dialog)
				Dialog.Destroy();

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
				progress_dialog.Destroy();
				progress_dialog = null;
			}
		}

		public void check_if_remove_progress_dialog (int total)
		{
			if (total != progress_dialog_total)
				remove_progress_dialog();
		}


		public bool UpdateProgressBar (string hdr_txt, string txt, int total)
		{
			if (progress_dialog != null)
				check_if_remove_progress_dialog(total);
			if (progress_dialog == null)
				create_progress_dialog(hdr_txt, total);
			progress_dialog_total = total;
			return progress_dialog.Update (String.Format ("{0} ", txt));
		}

		public void Run (object sender, EventArgs args)
		{
			try {
				LaunchDialog( );
			} catch (Exception e) {
				Log.Exception(e);
			}
		}
	}
}
