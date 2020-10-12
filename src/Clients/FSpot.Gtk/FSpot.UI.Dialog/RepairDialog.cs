//
// RepairDialog.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2006-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2006 Larry Ewing
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using Gtk;

using FSpot.Widgets;
using FSpot.Core;

namespace FSpot. UI.Dialog
{
	public class RepairDialog : BuilderDialog
	{
#pragma warning disable 649
		[GtkBeans.Builder.Object] ScrolledWindow view_scrolled;
#pragma warning restore 649

		readonly IBrowsableCollection source;
		readonly PhotoList missing;

		public RepairDialog (IBrowsableCollection collection) : base ("RepairDialog.ui", "repair_dialog")
		{
			source = collection;
			missing = new PhotoList ();

			FindMissing ();
			TrayView view = new TrayView (missing);
			view_scrolled.Add (view);

			ShowAll ();
		}

		public void FindMissing ()
		{
			int i;
			missing.Clear ();

			for (i = 0; i < source.Count; i++) {
				IPhoto item = source [i];
				string path = item.DefaultVersion.Uri.LocalPath;
				if (! File.Exists (path) || (new FileInfo (path).Length == 0))
					missing.Add (item);
			}
		}
	}
}
