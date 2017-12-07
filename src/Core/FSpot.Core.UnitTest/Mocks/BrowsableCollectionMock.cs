//
//  BrowsableCollectionMock.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Collections.Generic;
using System.Linq;

namespace FSpot.Core.UnitTest.Mocks
{
	internal class BrowsableCollectionMock : IBrowsableCollection
	{
		List<IPhoto> itemCollection;

		public BrowsableCollectionMock (params IPhoto[] items)
		{
			itemCollection = new List<IPhoto> (items);
		}

		public void RemoveAt (int index)
		{
			itemCollection.RemoveAt (index);
			if (Changed != null)
				Changed (this);
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
		public IEnumerable<IPhoto> Items {
			get {
				return itemCollection.AsEnumerable ();
			}
		}
		public IPhoto this [int index] {
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
