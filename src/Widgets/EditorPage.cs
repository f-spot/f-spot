/*
 * Widgets.EditorPage.cs
 *
 * Author(s)
 * 	Ruben Vermeersch <ruben@savanne.be>
 *
 * This is free software. See COPYING for details.
 */

using FSpot;
using Gtk;
using Mono.Unix;
using System;

namespace FSpot.Widgets {
	public class EditorPage : SidebarPage {
		public EditorPage () : base (new EditorPageWidget (),
									   Catalog.GetString ("Edit"),
									   "mode-image-edit") {
			// TODO: Somebody might need to change the icon to something more suitable.
			// FIXME: The icon isn't shown in the menu, are we missing a size?
			MainWindow.Toplevel.ViewModeChanged += HandleViewModeChanged;
		}

		private void HandleViewModeChanged (object sender, EventArgs args)
		{
			if (MainWindow.Toplevel.ViewMode == MainWindow.ModeType.PhotoView) {
				CanSelect = true;
			} else {
				CanSelect = false;
			}
		}
	}

	public class EditorPageWidget : ScrolledWindow {
		
	}
}
