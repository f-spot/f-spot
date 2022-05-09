//
//  BrowsableCollectionMock.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using FSpot.Models;

namespace FSpot.Core.UnitTest.Mocks
{
	class BrowsableCollectionMock : IBrowsableCollection
	{
		readonly List<IPhoto> itemCollection;

		public BrowsableCollectionMock (params IPhoto[] items)
		{
			itemCollection = new List<IPhoto> (items);
		}

		public void RemoveAt (int index)
		{
			itemCollection.RemoveAt (index);
			Changed?.Invoke (this);
		}

		#region IBrowsableCollection implementation
		public event IBrowsableCollectionChangedHandler Changed;
#pragma warning disable 67 // ItemsChanged event unused in mock
		public event IBrowsableCollectionItemsChangedHandler ItemsChanged;
#pragma warning restore 67
		public int IndexOf (IPhoto item)
		{
			return itemCollection.IndexOf (item);
		}
		public bool Contains (IPhoto item)
		{
			throw new System.NotImplementedException ();
		}
		public void MarkChanged (int index, IBrowsableItemChanges changes)
		{
			throw new System.NotImplementedException ();
		}
		public List<IPhoto> Items {
			get {
				return itemCollection;
			}
		}
		public IPhoto this[int index] {
			get {
				return itemCollection[index];
			}
		}
		public int Count {
			get {
				return itemCollection.Count;
			}
		}
		#endregion
	}
}
