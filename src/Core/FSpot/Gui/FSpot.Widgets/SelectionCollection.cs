//
// SelectionCollection.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using FSpot.Core;

namespace FSpot.Widgets
{
	public class SelectionCollection : IBrowsableCollection
	{
		readonly IBrowsableCollection parent;
		readonly Dictionary<IPhoto, int> selectedCells;
		BitArray bitArray;
		IPhoto[] items;
		IPhoto[] old;

		public SelectionCollection (IBrowsableCollection collection)
		{
			selectedCells = new Dictionary<IPhoto, int> ();
			parent = collection;
			bitArray = new BitArray (parent.Count);
			parent.Changed += HandleParentChanged;
			parent.ItemsChanged += HandleParentItemsChanged;
		}

		void HandleParentChanged (IBrowsableCollection collection)
		{
			IPhoto[] local = old;
			selectedCells.Clear ();
			bitArray = new BitArray (parent.Count);
			ClearCached ();

			if (old != null) {
				for (int i = 0; i < local.Length; i++) {
					int parentIndex = parent.IndexOf (local[i]);
					if (parentIndex >= 0)
						Add (parentIndex, false);
				}
			}

			// Call the directly so that we don't reset old immediately this way the old selection
			// set isn't actually lost until we change it.
			Changed?.Invoke (this);

			DetailedChanged?.Invoke (this, null);
		}

		public void MarkChanged (int item, IBrowsableItemChanges changes)
		{
			throw new NotImplementedException ();
		}

		void HandleParentItemsChanged (IBrowsableCollection collection, BrowsableEventArgs args)
		{
			if (ItemsChanged == null)
				return;

			var localIds = new List<int> ();
			foreach (int parent_index in args.Items) {
				// If the item isn't part of the selection ignore it
				if (!Contains (collection[parent_index]))
					return;

				int localIndex = IndexOf (parent_index);
				if (localIndex >= 0)
					localIds.Add (localIndex);
			}

			if (localIds.Count == 0)
				return;

			int[] localIdsItems = localIds.ToArray ();
			ItemsChanged (this, new BrowsableEventArgs (localIdsItems, args.Changes));
		}

		public BitArray ToBitArray ()
		{
			return new BitArray (bitArray);
		}

		public int[] Ids {
			get {
				// TODO: use IEnumerable<>
				return (from i in selectedCells.Values orderby i select i).ToArray ();
			}
		}

		public IPhoto this[int index] {
			get {
				int[] ids = Ids;
				return parent[ids[index]];
			}
		}

		public IEnumerable<IPhoto> Items {
			get {
				if (items != null)
					return items;

				int[] ids = Ids;
				items = new IPhoto[ids.Length];
				for (int i = 0; i < items.Length; i++) {
					items[i] = parent[ids[i]];
				}
				return items;
			}
		}

		public void Clear ()
		{
			Clear (true);
		}

		public void Clear (bool update)
		{
			int[] ids = Ids;
			selectedCells.Clear ();
			bitArray.SetAll (false);
			if (update)
				SignalChange (ids);
		}

		public void Add (IPhoto item)
		{
			if (Contains (item))
				return;

			int index = parent.IndexOf (item);
			Add (index);
		}

		public int Count {
			get {
				return selectedCells.Count;
			}
		}

		public bool Contains (IPhoto item)
		{
			return selectedCells.ContainsKey (item);
		}

		public bool Contains (int num)
		{
			if (num < 0 || num >= parent.Count)
				return false;

			return Contains (parent[num]);
		}

		public void Add (int num)
		{
			Add (num, true);
		}

		public void Add (int num, bool notify)
		{
			if (num == -1)
				return;

			if (Contains (num))
				return;

			IPhoto item = parent[num];
			selectedCells[item] = num;
			bitArray.Set (num, true);

			if (notify)
				SignalChange (new[] { num });
		}

		public void Add (int start, int end)
		{
			if (start == -1 || end == -1)
				return;

			int current = Math.Min (start, end);
			int final = Math.Max (start, end);
			int count = final - current + 1;
			var ids = new int[count];

			for (int i = 0; i < count; i++) {
				Add (current, false);
				ids[i] = current;
				current++;
			}

			SignalChange (ids);
		}

		public void Remove (int cell, bool notify)
		{
			IPhoto item = parent[cell];
			if (item != null)
				Remove (item, notify);

		}

		public void Remove (IPhoto item)
		{
			Remove (item, true);
		}

		public void Remove (int cell)
		{
			Remove (cell, true);
		}

		void Remove (IPhoto item, bool notify)
		{
			if (item == null)
				return;

			int parent_index = selectedCells[item];
			selectedCells.Remove (item);
			bitArray.Set (parent_index, false);

			if (notify)
				SignalChange (new[] { parent_index });
		}

		// Remove a range, except the start entry
		public void Remove (int start, int end)
		{
			if (start == -1 || end == -1)
				return;

			int current = Math.Min (start + 1, end);
			int final = Math.Max (start - 1, end);
			int count = final - current + 1;
			var ids = new int[count];

			for (int i = 0; i < count; i++) {
				Remove (current, false);
				ids[i] = current;
				current++;
			}

			SignalChange (ids);
		}

		public int IndexOf (int parentIndex)
		{
			return Array.IndexOf (Ids, parentIndex);
		}

		public int IndexOf (IPhoto item)
		{
			if (!Contains (item))
				return -1;

			int parentIndex = selectedCells[item];
			return Array.IndexOf (Ids, parentIndex);
		}

		public void ToggleCell (int cellNum, bool notify)
		{
			if (Contains (cellNum))
				Remove (cellNum, notify);
			else
				Add (cellNum, notify);
		}

		public void ToggleCell (int cellNum)
		{
			ToggleCell (cellNum, true);
		}

		public void SelectionInvert ()
		{
			var changedCell = new int[parent.Count];
			for (int i = 0; i < parent.Count; i++) {
				ToggleCell (i, false);
				changedCell[i] = i;
			}

			SignalChange (changedCell);
		}

		public event IBrowsableCollectionChangedHandler Changed;
		public event IBrowsableCollectionItemsChangedHandler ItemsChanged;

		public delegate void DetailedCollectionChanged (IBrowsableCollection collection, int[] ids);

		public event DetailedCollectionChanged DetailedChanged;

		void ClearCached ()
		{
			items = null;
		}

		public void SignalChange (int[] ids)
		{
			ClearCached ();
			old = Items.ToArray ();

			Changed?.Invoke (this);
			DetailedChanged?.Invoke (this, ids);
		}
	}
}
