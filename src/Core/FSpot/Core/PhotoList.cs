//
// PhotoList.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace FSpot.Core
{
	public class PhotoList : IBrowsableCollection
	{
		protected List<IPhoto> list { get; set; }

		public PhotoList (IEnumerable<IPhoto> photos)
		{
			list = new List<IPhoto> (photos);
		}

		public PhotoList (params IPhoto[] photos) : this(photos as IEnumerable<IPhoto>)
		{
		}

		public int Count => list.Count;

		public void Clear ()
		{
			list.Clear ();
			Reload ();
		}

		public int Capacity {
			set { list.Capacity = value; }
		}

		public void Add (IPhoto photo)
		{
			list.Add (photo);
			Reload ();
		}

		public void Add (IEnumerable<IPhoto> items)
		{
			list.AddRange (items);
			Reload ();
		}

		public int IndexOf (IPhoto item)
		{
			return list.IndexOf (item);
		}

		public bool Contains (IPhoto item)
		{
			return list.Contains (item);
		}

		public IPhoto this[int index] {
			get { return list[index]; }
			set {
				list[index] = value;
				MarkChanged (index, FullInvalidate.Instance);
			}
		}

		public void Sort (IComparer<IPhoto> compare)
		{
			list.Sort (compare);
			Reload ();
		}

		public void Reload ()
		{
			Changed?.Invoke (this);
		}

		public void MarkChanged (int num, IBrowsableItemChanges changes)
		{
			MarkChanged (new BrowsableEventArgs (num, changes));
		}

		public void MarkChanged (BrowsableEventArgs args)
		{
			ItemsChanged?.Invoke (this, args);
		}

		public IEnumerable<IPhoto> Items => list.AsEnumerable ();

		public event IBrowsableCollectionChangedHandler Changed;
		public event IBrowsableCollectionItemsChangedHandler ItemsChanged;
	}
}
