/*
 * FSpot.UI.Dialog.RepairDialog
 *
 * Author(s):
 *	Larry Ewing  <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 *
 */

using Gtk;
using System;
using System.IO;
using FSpot.Widgets;

namespace FSpot. UI.Dialog
{
	public class RepairDialog : BuilderDialog
	{
		[GtkBeans.Builder.Object] ScrolledWindow view_scrolled;
		
		IBrowsableCollection source;
		PhotoList missing;

		public RepairDialog (IBrowsableCollection collection) : base ("RepairDialog.ui", "repair_dialog")
		{
			source = collection;
			missing = new PhotoList ();
			
			FindMissing ();
			TrayView view = new TrayView (missing);
			view_scrolled.Add (view);
				
			this.ShowAll ();
		}

		public void FindMissing ()
		{
			int i;
			missing.Clear ();

			for (i = 0; i < source.Count; i++) {
				IBrowsableItem item = source [i];
				string path = item.DefaultVersion.Uri.LocalPath;
				if (! File.Exists (path) || (new FileInfo (path).Length == 0))
					missing.Add (item);
			}
		}
	}
}
