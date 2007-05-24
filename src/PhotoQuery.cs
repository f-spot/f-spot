using Gnome;
using System;
using System.Collections;
using FSpot.Query;

namespace FSpot {
	public class PhotoQuery : FSpot.IBrowsableCollection {
		private Photo [] photos;
		private PhotoStore store;
		private Term terms;
		private Tag [] tags;
		private string extra_condition;
		private PhotoStore.DateRange range = null;
		private RollSet roll_set = null;
		
		// Constructor
		public PhotoQuery (PhotoStore store)
		{
			this.store = store;
			photos = store.Query ((Tag [])null, null, range, roll_set);
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
		public event FSpot.IBrowsableCollectionItemsChangedHandler ItemsChanged;
		
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
		
		public Term Terms {
			get {
				return terms;
			}
			set {
				terms = value;
				untagged = false;
				RequestReload ();
			}
		}

		public string ExtraCondition {
			get {
				return extra_condition;
			}
			
			set {
				extra_condition = value;

				if (value != null)
					untagged = false;

 				RequestReload ();
 			}
 		}
		
		public PhotoStore.DateRange Range {
			get {
				return range;
			}
			set {
				if (value == range)
					return;

				range = value;
				
				RequestReload ();
			}
		}
		
		private bool untagged = false;
		public bool Untagged {
			get {
				return untagged;
			}
			set {
				if (untagged != value) {
					untagged = value;

					if (untagged) {
						tags = null;
						extra_condition = null;
					}
					
					RequestReload ();
				}
			}
		}

		public RollSet RollSet {
			get { return roll_set; }
			set {
				if (value == roll_set)
					return;

				roll_set = value;	
 				RequestReload ();
 			}
		}

		public void RequestReload ()
		{
			if (untagged)
				photos = store.QueryUntagged (range, roll_set);
			else
				photos = store.Query (terms, extra_condition, range, roll_set);

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
			ItemsChanged (this, new BrowsableArgs (index));
		}
	}
}
