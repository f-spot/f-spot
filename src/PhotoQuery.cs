using Gnome;
using System;
using System.Collections;

public class PhotoQuery : FSpot.IPhotoCollection {
	// ctor
	public PhotoQuery (PhotoStore store)
	{
		this.store = store;
		photos = store.Query (null, range);
	}

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

	public void RequestReload ()
	{
		if (Reload != null)
			Reload (this);
	}
	
	public int IndexOf (Photo photo)
	{
		return IndexOf (photo.Id);
	}

	public int IndexOf (uint photo_id)
	{
		// FIXME OPTIMIZEME horrible linear search
		for (int i = 0; i < photos.Length; i++) {
			if (photo_id == photos [i].Id)
				return i;
		}

		// FIXME use a real exception
		throw new Exception ("Photo index not found");
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
