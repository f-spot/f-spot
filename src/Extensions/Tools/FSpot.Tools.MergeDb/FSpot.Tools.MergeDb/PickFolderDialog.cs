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

using System;

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

			pickfolder_label.Text = string.Format (Catalog.GetString ("<big>The database refers to files contained in the <b>{0}</b> folder.\n Please select that folder so I can do the mapping.</big>"), folder);
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
