using NUnit.Framework;

namespace FSpot.Tests
{
	[TestFixture]
	public class IBrowsableItemTests
	{
		BrowsablePointer item;
		UriCollection collection;
		bool changed;

		public IBrowsableItemTests ()
		{
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

			collection.MarkChanged (new BrowsableEventArgs (0, FullInvalidate.Instance));
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
}
