public class QueryView : IconView {
	public QueryView (PhotoQuery query) {
		this.collection = query;
		this.query = query;
		
		query.Reload += OnReload;
		query.ItemChanged += ItemChanged;
	}
	
	private void OnReload (PhotoQuery query)
	{
		// FIXME we should probably try to merge the selection forward
		// but it needs some thought to be efficient.
			UnselectAllCells ();
			QueueResize ();
	}
	
	private void ItemChanged (PhotoQuery query, int item)
	{
		InvalidateCell (item);
	}
	
	PhotoQuery query;
}

