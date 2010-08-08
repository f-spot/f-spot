//
// QueryView.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using FSpot.Core;

namespace FSpot.Widgets
{
	public class QueryView : IconView {
		public QueryView (System.IntPtr raw) : base (raw) {}

		public QueryView (IBrowsableCollection query) : base (query) {}

		protected override bool OnPopupMenu ()
		{
			PhotoPopup popup = new PhotoPopup ();
			popup.Activate ();
			return true;
		}

		protected override void ContextMenu (Gtk.ButtonPressEventArgs args, int cell_num)
		{
			PhotoPopup popup = new PhotoPopup ();
			popup.Activate (this.Toplevel, args.Event);
		}
	}
}
