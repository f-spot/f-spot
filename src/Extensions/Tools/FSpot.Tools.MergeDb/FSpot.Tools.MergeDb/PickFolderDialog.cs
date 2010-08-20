/*
 * FSpot.PickFolderDialog.cs
 *
 * Author(s):
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details
 */

using System;
using FSpot;
using FSpot.Query;
using Mono.Unix;
using Hyena;

namespace FSpot.Tools.MergeDb
{
	internal class PickFolderDialog
	{
		[GtkBeans.Builder.Object] Gtk.Dialog pickfolder_dialog;
		[GtkBeans.Builder.Object] Gtk.FileChooserWidget pickfolder_chooser;
		[GtkBeans.Builder.Object] Gtk.Label pickfolder_label;

		public PickFolderDialog (Gtk.Dialog parent, string folder)
		{
			var builder = new GtkBeans.Builder (null, "pickfolder_dialog.ui", null);
			builder.Autoconnect (this);

			Log.Debug ("new pickfolder");
			pickfolder_dialog.Modal = false;
			pickfolder_dialog.TransientFor = parent;

			pickfolder_chooser.LocalOnly = false;

			pickfolder_label.Text = String.Format (Catalog.GetString ("<big>The database refers to files contained in the <b>{0}</b> folder.\n Please select that folder so I can do the mapping.</big>"), folder);
			pickfolder_label.UseMarkup = true;
		}

		public string Run ()
		{
			pickfolder_dialog.ShowAll ();
			if (pickfolder_dialog.Run () == -6)
				return pickfolder_chooser.Filename;
			else
				return null;
		}

		public Gtk.Dialog Dialog {
			get { return pickfolder_dialog; }
		}

	}
}
