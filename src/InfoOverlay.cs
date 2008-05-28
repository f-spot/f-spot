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
using FSpot.Widgets;

namespace FSpot {
	public class InfoItem : InfoVBox {
		BrowsablePointer item;
		Delay update_delay;
		
		public InfoItem (BrowsablePointer item)
		{
			update_delay = new Delay (Update);
			this.item = item;
			item.Changed += HandleItemChanged;
			HandleItemChanged (item, null);
			VersionIdChanged += HandleVersionIdChanged;
			//ShowTags = true;
		}
		
		public bool Update () {
			UpdateSingleSelection (item.Current);
			return false;
		}

		private void HandleItemChanged (BrowsablePointer sender, BrowsablePointerChangedArgs args)
		{
			update_delay.Start ();
			//this.UpdateSingleSelection (item.Current);
			//Photo = item.Current;
		}

		private void HandleVersionIdChanged (InfoVBox box, uint version_id)
		{
			Photo p = item.Current as Photo;
			PhotoQuery q = item.Collection as PhotoQuery;

			if (p !=  null && q != null) {
				p.DefaultVersionId  = version_id;
				q.Commit (item.Index);
			}
		}
	}

	public class InfoOverlay : ControlOverlay {
		InfoItem box;

		public InfoOverlay (Widget w, BrowsablePointer item) : base (w)
		{
			XAlign = 1.0;
			YAlign = 0.1;
			box = new InfoItem (item);
			box.BorderWidth = 15;
			Add (box);
			box.Show ();
			Visibility = VisibilityType.Partial;
			KeepAbove = true;
			//WindowPosition = WindowPosition.Mouse;
			AutoHide = false;
		}
	}
}
