namespace FSpot {
	public class PhotoList : IPhotoCollection {
		System.Collections.ArrayList list;
		IBrowsableItem [] cache;

		public PhotoList (IBrowsableItem [] photos)
		{
			list = new System.Collections.ArrayList (photos);
		}

		public int Count {
			get {
				return list.Count;
			}
		}

		public void Add (Photo photo)
		{
			list.Add (photo);
			cache = null;
			if (Changed != null)
				Changed (this);
		}

		public int IndexOf (IBrowsableItem item)
		{
			return list.IndexOf (item);
		}
		
		public bool Contains (IBrowsableItem item)
		{
			return list.Contains (item);
		}

		public IBrowsableItem [] Items {
			get {
				if (cache == null)
					cache = (IBrowsableItem []) list.ToArray (typeof (Photo));

				return cache;
			}
			set {
				cache = null;
				list.Clear ();
				list.Add (value);

				if (Changed != null)
					Changed (this);
			}
		}
		
		public Photo [] Photos {
			get {
				return (Photo []) this.Items;
			}
		}

		public event IBrowsableCollectionChangedHandler Changed;
		public event IBrowsableCollectionItemChangedHandler ItemChanged;
	}

	public class PhotoArray : IPhotoCollection {
		Photo [] photos;

		public PhotoArray (Photo [] photos) 
		{
			this.photos = photos;
		}

		public int Count {
			get {
				return photos.Length;
			}
		}

		public void Add (Photo photo)
		{
		        Photo [] larger = new Photo [photos.Length + 1];
			System.Array.Copy (photos, larger, photos.Length);
			larger [photos.Length] = photo;
			photos = larger;

			if (Changed != null)
				Changed (this);
		}
		
		// IBrowsableCollection
		public IBrowsableItem [] Items {
			get {
				return photos;
			}
		}
		
		public int IndexOf (IBrowsableItem item)
		{
			return System.Array.IndexOf (photos, item);
		}

		public bool Contains (IBrowsableItem item)
		{
			return IndexOf (item) >= 0;
		}

		public event IBrowsableCollectionChangedHandler Changed;
		public event IBrowsableCollectionItemChangedHandler ItemChanged;

		// IPhotoCollection
		public Photo [] Photos {
			get {
				return photos;
			}
		}
	}
}
