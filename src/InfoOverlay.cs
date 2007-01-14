/* 
 * Copyright 2007 Novell Inc.
 *
 * Author
 *   Larry Ewing <lewing@novell.com>
 *
 * See COPYING for license information.
 *
 */
using Gtk;

namespace FSpot {
	public class InfoItem : InfoBox {
		BrowsablePointer item;

		public InfoItem (BrowsablePointer item)
		{
			this.item = item;
			item.Changed += HandleItemChanged;
			HandleItemChanged (item, null);
		}

		private void HandleItemChanged (BrowsablePointer sender, BrowsablePointerChangedArgs args)
		{
			Photo = item.Current;
		}
	}

	public class InfoOverlay : ControlOverlay {
		InfoItem box;

		public InfoOverlay (Widget w, BrowsablePointer item) : base (w)
		{
			//AutoHide = false;
			XAlign = 0.9;
			YAlign = 0.1;
			box = new InfoItem (item);
			box.BorderWidth = 15;
			Add (box);
			box.Show ();
			Visibility = VisibilityType.Partial;
			KeepAbove = true;
			WindowPosition = WindowPosition.Mouse;
			AutoHide = false;
		}
	}
}
