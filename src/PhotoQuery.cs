using Gnome;
using System;
using System.Collections;

public class PhotoQuery {

	// Public events.

	public delegate void IconUpdatedHandler (PhotoQuery model, int item_num);
	public event IconUpdatedHandler IconUpdated;

	public delegate void ReloadHandler (PhotoQuery model);
	public event ReloadHandler Reload;

	private Photo [] photos;
	public Photo [] Photos {
		get {
			return photos;
		}
	}

	private PhotoStore store;

	public PhotoQuery (PhotoStore store)
	{
		this.store = store;
		photos = store.Query (null);
		Array.Sort (photos);
	}

	public void RequestReload ()
	{
		if (Reload != null)
			Reload (this);
	}

	private Tag [] tags;
	public Tag [] Tags {
		get {
			return tags;
		}

		set {
			tags = value;
			photos = store.Query (tags);
			Array.Sort (photos);
			RequestReload ();
		}
	}
}
