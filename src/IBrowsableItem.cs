#if ENABLE_NUNIT
using NUnit.Framework;
#endif

namespace FSpot {
	public delegate void IBrowsableCollectionChangedHandler (IBrowsableCollection collection);
	public delegate void IBrowsableCollectionItemsChangedHandler (IBrowsableCollection collection, BrowsableArgs args);

	/*
	public interface IBrowsableSelection : IBrowsableCollection {
		int [] ParentPositions ();
		public void Clear ();
		public void SelectAll ();
	}.
	*/

	public class BrowsableArgs : System.EventArgs {
		int [] items;

		public int [] Items {
			get { return items; }
		}

		public BrowsableArgs (int num)
		{
			items = new int [] { num };
		}

		public BrowsableArgs (int [] items)
		{
			this.items = items;
		}
	}

	public interface IBrowsableCollection {
		// FIXME this should really be ToArray ()
		IBrowsableItem [] Items {
			get;
		}
		
		int IndexOf (IBrowsableItem item);

		IBrowsableItem this [int index] {
			get;
		}

		int Count {
			get;
		}

		bool Contains (IBrowsableItem item);

		// FIXME the Changed event needs to pass along information
		// about the items that actually changed if possible.  For things like
		// TrayView everything has to be redrawn when a single
		// item has been added or removed which adds too much
		// overhead.
		event IBrowsableCollectionChangedHandler Changed;
		event IBrowsableCollectionItemsChangedHandler ItemsChanged;

		void MarkChanged (int index);
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


	public class BrowsablePointerChangedArgs {
		IBrowsableItem previous_item;
		int previous_index;
		
		public IBrowsableItem PreviousItem {
			get { return previous_item; }
		}
		
		public int PreviousIndex {
			get { return previous_index; }
		}

		public BrowsablePointerChangedArgs (IBrowsableItem old_item, int old_index)
		{
			previous_item = old_item;
			previous_index = old_index;
		}
	}

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
			BrowsablePointerChangedArgs args;
			
			args = new BrowsablePointerChangedArgs (Current, index);
			
			index = value;
			item = Current;
			
			if (Changed != null)
				Changed (this, args);
		}

		protected void HandleCollectionItemsChanged (IBrowsableCollection collection,
							     BrowsableArgs event_args)
		{
			foreach (int item in event_args.Items)
				if (item == Index) 
					SetIndex (Index);
		}
		
		protected void HandleCollectionChanged (IBrowsableCollection collection)
		{
			int old_location = Index;
			int next_location = collection.IndexOf (item);
			
			if (old_location == next_location) {
				if (! Valid (next_location))
					SetIndex (0);

				return;
			}
			
			if (Valid (next_location))
				SetIndex (next_location);
			else if (Valid (old_location))
				SetIndex (old_location);
			else
				SetIndex (0);
		}

#if ENABLE_NUNIT
		[TestFixture]
		public class Tests
		{
			BrowsablePointer item;
			UriCollection collection;
			bool changed;

			public Tests ()
			{
				Gnome.Vfs.Vfs.Initialize ();

				collection = new FSpot.UriCollection ();
				item = new BrowsablePointer (collection, 0);
				item.Changed += delegate {
					changed = true;
				};
			}
			
			[Test]
		        public void ChangeNotification ()
			{
				collection.Clear ();
				item.Index = 0;

				collection.Add (new System.Uri ("file:///blah.jpg"));
				Assert.IsTrue (item.IsValid);
				Assert.IsTrue (changed);
				
				changed = false;
				collection.Add (new System.Uri ("file:///test.png"));
				Assert.IsFalse (changed);

				collection.MarkChanged (0);
				Assert.IsTrue (changed);
			       
				changed = false;
				item.MoveNext ();
				Assert.IsTrue (changed);
				Assert.AreEqual (item.Index, 1);

				changed = false;
				collection.Add (new System.Uri ("file:///bill.png"));
				Assert.IsFalse (changed);
			}

			[Test]
			public void Motion ()
			{
				collection.Clear ();
				item.Index = 0;

				collection.Add (new System.Uri ("file:///fake.png"));
				collection.Add (new System.Uri ("file:///mynameisedd.jpg"));
				Assert.AreEqual (item.Index, 0);

				changed = false;
				item.MoveNext ();
				Assert.IsTrue (changed);

				Assert.AreEqual (item.Index, 1);
				item.MoveNext ();
				Assert.AreEqual (item.Index, 1);
				item.MoveNext (true);
				Assert.AreEqual (item.Index, 0);

				changed = false;
				item.MovePrevious (true);
				Assert.IsTrue (changed);

				Assert.AreEqual (item.Index, 1);
				item.MovePrevious ();
				Assert.AreEqual (item.Index, 0);
				item.MovePrevious ();
				Assert.AreEqual (item.Index, 0);
			}
		}
#endif
	}
}	
