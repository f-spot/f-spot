namespace FSpot {
	public class PhotoList : IBrowsableCollection {
		System.Collections.ArrayList list;
		IBrowsableItem [] cache;

		public PhotoList (IBrowsableItem [] photos)
		{
			list = new System.Collections.ArrayList (photos);
		}

		public void Add (Photo photo)
		{
			list.Add (photo);
			cache = null;
			if (Changed != null)
				Changed (this);
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
		public event IBrowsableCollectionItemChangedHandler ItemChanged;
	}

	public class PhotoArray : IPhotoCollection {
		Photo [] photos;

		public PhotoArray (Photo [] photos) 
		{
			this.photos = photos;
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
