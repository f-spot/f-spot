public abstract class PhotoListModel {

	// Public events.

	public delegate void IconUpdatedHandler (PhotoListModel model, int item_num);
	public event IconUpdatedHandler IconUpdated;

	public delegate void ReloadHandler (PhotoListModel model);
	public event ReloadHandler Reload;

	// Public API.

	public class Item {
		public string Path;
		public string Caption;

		public Item (string path, string caption)
		{
			Path = path;
			Caption = caption;
		}
	}

	public abstract Item GetItem (int item_num);
	public abstract int Count { get; }

	public void RequestReload ()
	{
		if (Reload != null)
			Reload (this);
	}
}
