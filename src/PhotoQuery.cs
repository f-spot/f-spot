using Gnome;
using System;
using System.Collections;

namespace FSpot {
	public class PhotoQuery : FSpot.IPhotoCollection {
		private Photo [] photos;
		private PhotoStore store;
		private Tag [] tags;
		private PhotoStore.DateRange range = null;
		
		// Constructor
		public PhotoQuery (PhotoStore store)
		{
			this.store = store;
			photos = store.Query (null, range);
		}
		
		// IPhotoCollection Interface
		public event FSpot.IBrowsableCollectionChangedHandler Changed;
		public event FSpot.IBrowsableCollectionItemChangedHandler ItemChanged;
		
		public Photo [] Photos {
			get {
				return photos;
			}
		}

		public IBrowsableItem [] Items {
			get {
				return (IBrowsableItem [])photos;
			}
		}
		
		public PhotoStore Store {
			get {
				return store;
			}
		}
		
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

		public void RequestReload ()
		{
			if (Changed != null)
				Changed (this);
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
			MarkChanged (index);
		}
		
		public void MarkChanged (int index)
		{
			ItemChanged (this, index);
		}
	}
}
