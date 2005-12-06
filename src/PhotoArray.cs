namespace FSpot {
	public class PhotoList : IBrowsableCollection {
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

		public void Clear ()
		{
			list.Clear ();

			if (Changed != null)
				Changed (this);
		}

		public int Capacity {
			set {
				list.Capacity = value;
			}
		}

		public void Add (IBrowsableItem photo)
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

		public IBrowsableItem this [int index] {
			get {
				return (IBrowsableItem) list [index];
			}
		}

		public IBrowsableItem [] Items {
			get {
				if (cache == null)
					cache = (IBrowsableItem []) list.ToArray (typeof (IBrowsableItem));

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
		
		public event IBrowsableCollectionChangedHandler Changed;
		public event IBrowsableCollectionItemsChangedHandler ItemsChanged;
	}

	public class PhotoArray : IBrowsableCollection {
		IBrowsableItem [] photos;

		public PhotoArray (IBrowsableItem [] photos) 
		{
			this.photos = photos;
		}

		public int Count {
			get {
				return photos.Length;
			}
		}

		/*
		public void Add (Photo photo)
		{
		        Photo [] larger = new Photo [photos.Length + 1];
			System.Array.Copy (photos, larger, photos.Length);
			larger [photos.Length] = photo;
			photos = larger;

			if (Changed != null)
				Changed (this);
		}
		*/

		// IBrowsableCollection
		public IBrowsableItem [] Items {
			get {
				return photos;
			}
		}

		public IBrowsableItem this [int index] {
			get {
				return photos [index];
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
		public event IBrowsableCollectionItemsChangedHandler ItemsChanged;
	}
}
