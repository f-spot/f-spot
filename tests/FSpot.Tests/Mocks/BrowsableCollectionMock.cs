//
//  BrowsableCollectionMock.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace FSpot.Core.Tests.Mocks
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
		public event IBrowsableCollectionItemsChangedHandler ItemsChanged;

		public void RaiseItemsChanged (int[] indexes, IBrowsableItemChanges changes)
			=> ItemsChanged?.Invoke (this, new BrowsableEventArgs (indexes, changes));
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
		public IEnumerable<IPhoto> Items {
			get {
				return itemCollection.AsEnumerable ();
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
