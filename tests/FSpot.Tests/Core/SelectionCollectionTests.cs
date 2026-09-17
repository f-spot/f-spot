// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq;

using FSpot.Core;
using FSpot.Core.Tests.Mocks;
using FSpot.Widgets;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Core.Tests
{
	[TestClass]
	public sealed class SelectionCollectionTests
	{
		readonly IPhoto photo1 = new FilePhoto (new SafeUri ("/1"));
		readonly IPhoto photo2 = new FilePhoto (new SafeUri ("/2"));
		readonly IPhoto photo3 = new FilePhoto (new SafeUri ("/3"));

		[TestMethod]
		public void NewSelection_IsEmpty ()
		{
			var selection = new SelectionCollection (new BrowsableCollectionMock (photo1, photo2));

			Assert.AreEqual (0, selection.Count);
			Assert.IsEmpty (selection.Items);
			Assert.IsFalse (selection.Contains (photo1));
			Assert.IsFalse (selection.Contains (0));
			Assert.IsFalse (selection.Contains (-1));
			Assert.IsFalse (selection.Contains (99));
			Assert.IsEmpty (selection.Ids);
		}

		[TestMethod]
		public void Add_Index_SelectsThatPhotoOnce ()
		{
			var selection = new SelectionCollection (new BrowsableCollectionMock (photo1, photo2));
			int changes = 0;
			selection.Changed += _ => changes++;

			selection.Add (1);
			selection.Add (1);

			Assert.AreEqual (1, selection.Count);
			Assert.AreSame (photo2, selection[0]);
			Assert.IsTrue (selection.Contains (photo2));
			Assert.IsTrue (selection.Contains (1));
			Assert.AreSequenceEqual (new[] { 1 }, selection.Ids);
			Assert.AreEqual (1, changes);
		}

		[TestMethod]
		public void Add_Photo_UsesTheParentIndex ()
		{
			var selection = new SelectionCollection (new BrowsableCollectionMock (photo1, photo2));
			selection.Add (photo2);

			Assert.IsTrue (selection.Contains (1));
			Assert.AreEqual (0, selection.IndexOf (photo2));
		}

		[TestMethod]
		public void Add_MinusOne_IsIgnored ()
		{
			var selection = new SelectionCollection (new BrowsableCollectionMock (photo1));
			selection.Add (-1);
			Assert.AreEqual (0, selection.Count);
		}

		[TestMethod]
		public void Add_Range_SelectsInclusiveBounds ()
		{
			var selection = new SelectionCollection (new BrowsableCollectionMock (photo1, photo2, photo3));
			selection.Add (2, 0);

			Assert.AreEqual (3, selection.Count);
			Assert.AreSequenceEqual (new[] { 0, 1, 2 }, selection.Ids);
		}

		[TestMethod]
		public void Remove_DropsThePhotoAndNotifies ()
		{
			var selection = new SelectionCollection (new BrowsableCollectionMock (photo1, photo2));
			selection.Add (0);
			selection.Add (1);
			int[] detailed = null;
			selection.DetailedChanged += (_, ids) => detailed = ids;

			selection.Remove (photo1);

			Assert.AreEqual (1, selection.Count);
			Assert.IsFalse (selection.Contains (photo1));
			Assert.AreSequenceEqual (new[] { 0 }, detailed);
		}

		[TestMethod]
		public void Remove_Range_KeepsTheStartIndex ()
		{
			var selection = new SelectionCollection (new BrowsableCollectionMock (photo1, photo2, photo3));
			selection.Add (0, 2);

			selection.Remove (0, 2);

			Assert.IsTrue (selection.Contains (0));
			Assert.IsFalse (selection.Contains (1));
			Assert.IsFalse (selection.Contains (2));
		}

		[TestMethod]
		public void ToggleCell_AddsThenRemoves ()
		{
			var selection = new SelectionCollection (new BrowsableCollectionMock (photo1));
			selection.ToggleCell (0);
			Assert.IsTrue (selection.Contains (0));
			selection.ToggleCell (0);
			Assert.IsFalse (selection.Contains (0));
		}

		[TestMethod]
		public void SelectionInvert_FlipsEveryParentIndex ()
		{
			var selection = new SelectionCollection (new BrowsableCollectionMock (photo1, photo2, photo3));
			selection.Add (1);

			selection.SelectionInvert ();

			Assert.IsTrue (selection.Contains (0));
			Assert.IsFalse (selection.Contains (1));
			Assert.IsTrue (selection.Contains (2));
		}

		[TestMethod]
		public void Clear_EmptiesTheSet ()
		{
			var selection = new SelectionCollection (new BrowsableCollectionMock (photo1, photo2));
			selection.Add (0, 1);

			selection.Clear ();

			Assert.AreEqual (0, selection.Count);
			Assert.IsFalse (selection.ToBitArray ().Get (0));
		}

		[TestMethod]
		public void ParentChanged_KeepsPhotosThatAreStillPresent ()
		{
			var parent = new BrowsableCollectionMock (photo1, photo2, photo3);
			var selection = new SelectionCollection (parent);
			selection.Add (1);
			selection.Add (2);

			parent.RemoveAt (0);

			Assert.IsTrue (selection.Contains (photo2));
			Assert.IsTrue (selection.Contains (photo3));
			Assert.AreSequenceEqual (new[] { 0, 1 }, selection.Ids);
		}

		[TestMethod]
		public void Items_AreTheSelectedPhotosInIndexOrder ()
		{
			var selection = new SelectionCollection (new BrowsableCollectionMock (photo1, photo2, photo3));
			selection.Add (2);
			selection.Add (0);

			Assert.AreSequenceEqual (new[] { photo1, photo3 }, selection.Items.ToArray ());
		}

		[TestMethod]
		public void IndexOf_MissingPhoto_IsMinusOne ()
		{
			var selection = new SelectionCollection (new BrowsableCollectionMock (photo1, photo2));
			selection.Add (0);

			Assert.AreEqual (-1, selection.IndexOf (photo2));
			Assert.AreEqual (-1, selection.IndexOf (99));
		}

		[TestMethod]
		public void Add_PhotoNotInParent_IsIgnored ()
		{
			var selection = new SelectionCollection (new BrowsableCollectionMock (photo1));
			selection.Add (photo2);
			Assert.AreEqual (0, selection.Count);
		}

		[TestMethod]
		public void ParentItemsChanged_SkipsUnselectedIndexesAndStillNotifiesTheRest ()
		{
			var parent = new BrowsableCollectionMock (photo1, photo2, photo3);
			var selection = new SelectionCollection (parent);
			selection.Add (0);
			BrowsableEventArgs received = null;
			selection.ItemsChanged += (_, args) => received = args;

			parent.RaiseItemsChanged (new[] { 1, 0 }, FullInvalidate.Instance);

			Assert.IsNotNull (received);
			Assert.AreSequenceEqual (new[] { 0 }, received.Items);
			Assert.AreSame (FullInvalidate.Instance, received.Changes);
		}
	}
}
