namespace FSpot {
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
