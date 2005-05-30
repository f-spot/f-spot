namespace FSpot {
	public delegate void IBrowsableCollectionChangedHandler (IBrowsableCollection collection);
	public delegate void IBrowsableCollectionItemChangedHandler (IBrowsableCollection collection, int item);

	public interface IBrowsableCollection {
		IBrowsableItem [] Items {
			get;
		}
		
		int IndexOf (IBrowsableItem item);

		int Count {
			get;
		}

		event IBrowsableCollectionChangedHandler Changed;
		event IBrowsableCollectionItemChangedHandler ItemChanged;
	}

	public interface IBrowsableItem {
		System.DateTime Time {
			get;
		}
		
		Tag [] Tags {
			get;
		}

		System.Uri DefaultVersionUri {
			get;
		}

		string Description {
			get;
		}

		string Name {
			get; 
		}
	}

	public delegate void ItemIndexChangedHandler (BrowsablePointer pointer, IBrowsableItem old);

	public class BrowsablePointer {
		IBrowsableCollection collection;
		IBrowsableItem item;
		int index;
		public event ItemIndexChangedHandler IndexChanged;

		public BrowsablePointer (IBrowsableCollection collection, int index)
		{
			this.collection = collection;
			this.Index = index;
			item = Current;

			collection.Changed += HandleCollectionChanged;
		}

		public IBrowsableCollection Collection {
			get {
				return collection;
			}
		}

		public IBrowsableItem Current {
			get {
				if (!this.IsValid)
					return null;
				else 
					return collection.Items [index];
			}
		}

		private bool Valid (int val)
		{
			return val >= 0 && val < collection.Count;
		}

		public bool IsValid {
			get {
				return Valid (this.Index);
			}
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
			get {
				return index;
			}
			set {
				if (index != value) {
					IBrowsableItem old = item;
					index = value;
					item = Current;
					if (IndexChanged != null)
						IndexChanged (this, old);
				}				
			}
		}

		protected virtual void HandleCollectionChanged (IBrowsableCollection collection)
		{
			int next_location = collection.IndexOf (item);
		        Index = Valid (next_location) ? next_location : 0;
		}
	}
}	
