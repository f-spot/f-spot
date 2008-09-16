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

namespace MergeDbExtension
{
	internal class PickFolderDialog
	{
		[Glade.Widget] Gtk.Dialog pickfolder_dialog;
		[Glade.Widget] Gtk.FileChooserWidget pickfolder_chooser;
		[Glade.Widget] Gtk.Label pickfolder_label;

		public PickFolderDialog (Gtk.Dialog parent, string folder)
		{
			Glade.XML xml = new Glade.XML (null, "MergeDb.glade", "pickfolder_dialog", "f-spot");
			xml.Autoconnect (this);
			Console.WriteLine ("new pickfolder");
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
