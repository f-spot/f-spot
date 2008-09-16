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
	public class InfoItem : InfoBox {
		BrowsablePointer item;
		
		public InfoItem (BrowsablePointer item)
		{
			this.item = item;
			item.Changed += HandleItemChanged;
			HandleItemChanged (item, null);
			VersionIdChanged += HandleVersionIdChanged;
			ShowTags = true;
			ShowRating = true;
			Context = ViewContext.FullScreen;
		}
		
		private void HandleItemChanged (BrowsablePointer sender, BrowsablePointerChangedArgs args)
		{
			Photo = item.Current as Photo;
		}

		private void HandleVersionIdChanged (InfoBox box, uint version_id)
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
			DefaultWidth = 250;
			box = new InfoItem (item);
			box.BorderWidth = 15;
			Add (box);
			box.ShowAll ();
			Visibility = VisibilityType.Partial;
			KeepAbove = true;
			//WindowPosition = WindowPosition.Mouse;
			AutoHide = false;
		}
	}
}
