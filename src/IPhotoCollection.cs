namespace FSpot {
	public delegate void IPhotoCollectionChangedHandler (IPhotoCollection collection);
	public delegate void IPhotoCollectionItemChangedHandler (IPhotoCollection collection, int item);

	public interface IPhotoCollection {
		Photo [] Photos {
			get;
		}

#if false
		event IPhotoCollectionChangedHandler Changed;
		event IPhotoCollectionItemChangedHandler ItemChanged;
#endif		
	}

	
}
