namespace FSpot {
	public class PhotoArray : IPhotoCollection {
		public PhotoArray (Photo [] photos) {
			this.photos = photos;
		}
		
		public Photo [] Photos {
			get {
				return photos;
			}
		}

		Photo [] photos;
	}
}
