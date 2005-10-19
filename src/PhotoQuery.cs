using Gnome;
using System;
using System.Collections;

namespace FSpot {
	public class PhotoQuery : FSpot.IBrowsableCollection {
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

		public int Count {
			get {
				return photos.Length;
			}
		}
		
		public bool Contains (IBrowsableItem item) {
			return IndexOf (item) >= 0;
		}

		// IPhotoCollection Interface
		public event FSpot.IBrowsableCollectionChangedHandler Changed;
		public event FSpot.IBrowsableCollectionItemChangedHandler ItemChanged;
		
		public IBrowsableItem this [int index] {
			get {
				return photos [index];
			}
		}

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
		
		public int IndexOf (IBrowsableItem photo)
		{
			return System.Array.IndexOf (photos, photo);
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
