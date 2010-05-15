/*
 * FSpot.PhotoList.cs
 *
 * Author(s):
 *	Larry Ewing
 *
 * This is free software, See COPYING for details
 */

using System.Collections.Generic;

namespace FSpot {
	public class PhotoList : IBrowsableCollection {
		protected List<IBrowsableItem> list;
		IBrowsableItem [] cache;

		public PhotoList (IBrowsableItem [] photos)
		{
			list = new List<IBrowsableItem> (photos);
		}

		public PhotoList ()
		{
			list = new List<IBrowsableItem> ();
		}

		public int Count {
			get { return list.Count; }
		}

		public void Clear ()
		{
			list.Clear ();
			Reload ();
		}

		public int Capacity {
			set { list.Capacity = value; }
		}
        
		public void AddAll (List<IBrowsableItem> photos)
		{
			list = photos;
			Reload ();
		}

		public void Add (IBrowsableItem photo)
		{
			list.Add (photo);
			Reload ();
		}

		public void Add (IBrowsableItem [] items)
		{
			list.AddRange (items);
			Reload ();
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
			get { return list [index]; }
			set {
				list [index] = value;
				MarkChanged (index, FullInvalidate.Instance);
			}
		}

		public void Sort (IComparer<IBrowsableItem> compare)
		{
			list.Sort (compare);
			Reload ();
		}

		public void Reload ()
		{
			cache = null;
			if (Changed != null)
				Changed (this);
		}

		public void MarkChanged (int num, IBrowsableItemChanges changes)
		{
			MarkChanged (new BrowsableEventArgs (num, changes));
		}

		public void MarkChanged (BrowsableEventArgs args)
		{
			if (ItemsChanged != null)
				ItemsChanged (this, args);
		}

		public IBrowsableItem [] Items {
			get {
				if (cache == null)
					cache = list.ToArray ();

				return cache;
			}
			set {
				list.Clear ();
				Add (value);
			}
		}

		public event IBrowsableCollectionChangedHandler Changed;
		public event IBrowsableCollectionItemsChangedHandler ItemsChanged;
	}
}
