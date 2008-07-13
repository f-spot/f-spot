/*
 * FSpot.PhotoArray.cs
 *
 * Author(s):
 *	Larry Ewing
 *
 * This is free software, See COPYING for details
 */

namespace FSpot
{
	public class PhotoArray : IBrowsableCollection {
		IBrowsableItem [] photos;

		public PhotoArray (IBrowsableItem [] photos) 
		{
			this.photos = photos;
		}

		public int Count {
			get { return photos.Length; }
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
			get { return photos; }
		}

		public IBrowsableItem this [int index] {
			get { return photos [index]; }
		}
		
		public int IndexOf (IBrowsableItem item)
		{
			return System.Array.IndexOf (photos, item);
		}

		public bool Contains (IBrowsableItem item)
		{
			return IndexOf (item) >= 0;
		}

		public void MarkChanged (int item, IBrowsableItemChanges changes)
		{
			if (ItemsChanged != null)
				ItemsChanged (this, new BrowsableEventArgs (item, changes));
		}

		public void Reload ()
		{
			if (Changed != null)
				Changed (this);
		}

		public event IBrowsableCollectionChangedHandler Changed;
		public event IBrowsableCollectionItemsChangedHandler ItemsChanged;
	}
}
