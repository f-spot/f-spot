//
// PickFolderDialog.cs
//
// Author:
//   Paul Lange <palango@gmx.de>
//   Stephane Delcroix <sdelcroix*novell.com>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2010 Paul Lange
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Resources.Lang;

namespace FSpot.Tools.MergeDb
{
	class PickFolderDialog
	{
#pragma warning disable 649
		[GtkBeans.Builder.Object] Gtk.Dialog pickfolder_dialog;
		[GtkBeans.Builder.Object] Gtk.FileChooserWidget pickfolder_chooser;
		[GtkBeans.Builder.Object] Gtk.Label pickfolder_label;
#pragma warning restore 649

		public PickFolderDialog (Gtk.Dialog parent, string folder)
		{
			var builder = new GtkBeans.Builder (null, "pickfolder_dialog.ui", null);
			builder.Autoconnect (this);

			Logger.Log.Debug ("new pickfolder");
			pickfolder_dialog.Modal = false;
			pickfolder_dialog.TransientFor = parent;

			pickfolder_chooser.LocalOnly = false;

			pickfolder_label.Text = string.Format (Strings.DatabaseRefersToFilesContainedInXFolderPleaseSelectThatFolderSoICanDoTheMapping, folder);
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
