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
using FSpot.Core;
using FSpot.Widgets;

namespace FSpot {
	public class InfoItem : InfoBox {
		BrowsablePointer item;

		public InfoItem (BrowsablePointer item)
		{
			this.item = item;
			item.Changed += HandleItemChanged;
			HandleItemChanged (item, null);
			VersionChanged += HandleVersionChanged;
			ShowTags = true;
			ShowRating = true;
			Context = ViewContext.FullScreen;
		}

		private void HandleItemChanged (object sender, BrowsablePointerChangedEventArgs args)
		{
			Photo = item.Current;
		}

		private void HandleVersionChanged (InfoBox box, IPhotoVersion version)
		{
			IPhotoVersionable versionable = item.Current as IPhotoVersionable;
			PhotoQuery q = item.Collection as PhotoQuery;

			if (versionable != null && q != null) {
				versionable.SetDefaultVersion (version);
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
