using Gnome;
using System;
using System.Collections;

public class PhotoQuery : PhotoListModel {
	private PhotoStore store;
	private ArrayList photos;

	private ArrayList tags;
	public ArrayList Tags {
		get {
			return tags;
		}

		set {
			tags = value;
			photos = store.Query (tags);
			RequestReload ();
		}
	}

	public override Item GetItem (int item_num)
	{
		Photo photo = photos [item_num] as Photo;
		return new Item (photo.Path, photo.Name);
	}

	public override int Count {
		get {
			return photos.Count;
		}
	}

	public PhotoQuery (PhotoStore store)
	{
		this.store = store;
		photos = store.Query (null);
	}
}
