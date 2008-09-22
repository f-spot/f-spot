/*
 * FSpot.BrowsablePointer.cs
 * 
 * Author(s):
 *	Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

namespace FSpot
{
	public delegate void ItemChangedHandler (BrowsablePointer pointer, BrowsablePointerChangedArgs old);
	public class BrowsablePointer {
		IBrowsableCollection collection;
		IBrowsableItem item;
		int index;
		public event ItemChangedHandler Changed;

		public BrowsablePointer (IBrowsableCollection collection, int index)
		{
			this.collection = collection;
			this.Index = index;
			item = Current;

			collection.Changed += HandleCollectionChanged;
			collection.ItemsChanged += HandleCollectionItemsChanged;
		}

		public IBrowsableCollection Collection {
			get { return collection; }
		}

		public IBrowsableItem Current {
			get {
				if (!this.IsValid)
					return null;
				else 
					return collection [index];
			}
		}

		private bool Valid (int val)
		{
			return val >= 0 && val < collection.Count;
		}

		public bool IsValid {
			get { return Valid (this.Index); }
		}

		public void MoveFirst ()
		{
			Index = 0;
		}

		public void MoveLast ()
		{
			Index = collection.Count - 1;
		}
		
		public void MoveNext ()
		{
			MoveNext (false);
		}

		public void MoveNext (bool wrap)
		{
			int val = Index;

			val++;
			if (!Valid (val))
				val = wrap ? 0 : Index;
			
			Index = val;
		}
		
		public void MovePrevious ()
		{
			MovePrevious (false);
		}

		public void MovePrevious (bool wrap)
		{
			int val = Index;

			val--;
			if (!Valid (val))
				val = wrap ? collection.Count - 1 : Index;

			Index = val;
		}

		public int Index {
			get { return index; }
			set {
				if (index != value) {
					SetIndex (value);
				}				
			}
		}

		private void SetIndex (int value)
		{
			SetIndex (value, null);
		}

		private void SetIndex (int value, IBrowsableItemChanges changes)
		{
			BrowsablePointerChangedArgs args;
			
			args = new BrowsablePointerChangedArgs (Current, index, changes);
			
			index = value;
			item = Current;
			
			if (Changed != null)
				Changed (this, args);
		}

		protected void HandleCollectionItemsChanged (IBrowsableCollection collection,
							     BrowsableEventArgs event_args)
		{
			foreach (int item in event_args.Items)
				if (item == Index) 
					SetIndex (Index, event_args.Changes);
		}
		
		protected void HandleCollectionChanged (IBrowsableCollection collection)
		{
			int old_location = Index;
			int next_location = collection.IndexOf (item);
			
			if (old_location == next_location) {
				if (! Valid (next_location))
					SetIndex (0, null);

				return;
			}
			
			if (Valid (next_location))
				SetIndex (next_location);
			else if (Valid (old_location))
				SetIndex (old_location);
			else
				SetIndex (0);
		}
	}
}
