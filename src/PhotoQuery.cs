using Gnome;
using System;
using System.Collections;

public class PhotoQuery {

	// Public events.
	public delegate void ReloadHandler (PhotoQuery model);
	public event ReloadHandler Reload;
	
	public delegate void ItemChangedHandler (PhotoQuery model, int item);
	public event ItemChangedHandler ItemChanged;

	private Photo [] photos;
	public Photo [] Photos {
		get {
			return photos;
		}
	}

	private PhotoStore store;
	public PhotoStore Store {
		get {
			return store;
		}
	}

	public PhotoQuery (PhotoStore store)
	{
		this.store = store;
		photos = store.Query (null, range);
	}

	public void RequestReload ()
	{
		if (Reload != null)
			Reload (this);
	}
	
	public void Commit (int index) 
	{
		store.Commit (photos[index]);
		ItemChanged (this, index);
	}

	private Tag [] tags;
	public Tag [] Tags {
		get {
			return tags;
		}

		set {
			tags = value;
			photos = store.Query (tags, range);
			RequestReload ();
		}
	}

	private PhotoStore.DateRange range = null;
	public PhotoStore.DateRange Range {
		get {
			return range;
		}
		set {
			range = value;
			photos = store.Query (tags, range);
			RequestReload ();
		}
	}
}
