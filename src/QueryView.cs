//
// QueryView.cs
//
// Copyright (C) 2004 Novell, Inc.
//

public class QueryView : IconView {
	FSpot.PhotoQuery query;

	public QueryView (FSpot.PhotoQuery query) {
		this.collection = query;
		this.query = query;
		
		query.Changed += HandleChanged;
		query.ItemChanged += HandleItemChanged;
	}

	protected QueryView (System.IntPtr raw) : base (raw) {}
	
	private void HandleChanged (FSpot.IPhotoCollection query)
	{
		// FIXME we should probably try to merge the selection forward
		// but it needs some thought to be efficient.
		UnselectAllCells ();
		QueueResize ();
	}
	
	private void HandleItemChanged (FSpot.IPhotoCollection query, int item)
	{
		UpdateThumbnail (item);
		InvalidateCell (item);
	}
}

