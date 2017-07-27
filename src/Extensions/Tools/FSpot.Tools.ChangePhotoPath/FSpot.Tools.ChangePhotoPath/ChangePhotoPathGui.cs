//
// ChangePhotoPathGui.cs
//
// Author:
//   Stephane Delcroix <sdelcroix*novell.com>
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
//using Gnome.Vfs;

using Hyena;
using Hyena.Widgets;

namespace FSpot.Tools.ChangePhotoPath
{
	public class Dump : Gtk.Dialog, ICommand, IChangePhotoPathGui
	{
		private string dialog_name = "ChangePhotoPath";
		private GtkBeans.Builder builder;
		private Gtk.Dialog dialog;
		private ChangePathController contr;

		private ProgressDialog progress_dialog;
		private int progress_dialog_total = 0;

#pragma warning disable 649
		[GtkBeans.Builder.Object] Gtk.Entry old_common_uri;
		[GtkBeans.Builder.Object] Gtk.Label new_common_uri;
#pragma warning restore 649

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
			builder = new GtkBeans.Builder (null, "ChangePhotoPath.ui", null);
			builder.Autoconnect (this);
		}

		private Gtk.Dialog Dialog {
			get {
				if (dialog == null)
					dialog = new Gtk.Dialog (builder.GetRawObject (dialog_name));
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
			DisplayMsg (Gtk.MessageType.Error, "An error occurred. Reverted all changes to the database.");
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
			return progress_dialog.Update (string.Format ("{0} ", txt));
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
