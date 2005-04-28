//
// QueryView.cs
//
// Copyright (C) 2004 Novell, Inc.
//

public class QueryView : IconView {
	public QueryView (System.IntPtr raw) : base (raw) {}

	public QueryView (FSpot.PhotoQuery query) {
		this.collection = query;
		
		query.Changed += HandleChanged;
		query.ItemChanged += HandleItemChanged;
	}

	private void HandleChanged (FSpot.IBrowsableCollection sender)
	{
		// FIXME we should probably try to merge the selection forward
		// but it needs some thought to be efficient.
		UnselectAllCells ();
		QueueResize ();
	}
	
	private void HandleItemChanged (FSpot.IBrowsableCollection sender, int item)
	{
		UpdateThumbnail (item);
		InvalidateCell (item);
	}
}

