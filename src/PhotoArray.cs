namespace FSpot {
	public class PhotoArray : IPhotoCollection {
		public PhotoArray (Photo [] photos) {
			this.photos = photos;
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

		Photo [] photos;
	}
}
