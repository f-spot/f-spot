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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using FSpot.Core;
using FSpot.Models;

namespace FSpot.Widgets
{
	public class SelectionCollection : IBrowsableCollection
	{
		IBrowsableCollection parent;
		Dictionary<IPhoto, int> selected_cells;
		BitArray bit_array;
		List<IPhoto> items;
		List<IPhoto> old;

		public SelectionCollection (IBrowsableCollection collection)
		{
			selected_cells = new Dictionary<IPhoto, int> ();
			parent = collection;
			bit_array = new BitArray (parent.Count);
			parent.Changed += HandleParentChanged;
			parent.ItemsChanged += HandleParentItemsChanged;
		}

		void HandleParentChanged (IBrowsableCollection collection)
		{
			var local = old;
			selected_cells.Clear ();
			bit_array = new BitArray (parent.Count);
			ClearCached ();

			if (old != null) {
				for (int i = 0; i < local.Count; i++) {
					int parent_index = parent.IndexOf (local[i]);
					if (parent_index >= 0)
						Add (parent_index, false);
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

			var local_ids = new List<int> ();
			foreach (int parent_index in args.Items) {
				// If the item isn't part of the selection ignore it
				if (!Contains (collection[parent_index]))
					return;

				int local_index = IndexOf (parent_index);
				if (local_index >= 0)
					local_ids.Add (local_index);
			}

			if (local_ids.Count == 0)
				return;

			var localIdsItems = local_ids;
			ItemsChanged (this, new BrowsableEventArgs (localIdsItems, args.Changes));
		}

		public BitArray ToBitArray ()
		{
			return new BitArray (bit_array);
		}

		public List<int> Ids {
			get {
				// TODO: use IEnumerable<>
				return (from i in selected_cells.Values orderby i select i).ToList ();
			}
		}

		public IPhoto this[int index] {
			get {
				var ids = Ids;
				return parent[ids[index]];
			}
		}

		public List<IPhoto> Items {
			get {
				if (items != null)
					return items;

				var ids = Ids;
				items = new List<IPhoto> (ids.Count);
				for (int i = 0; i < items.Count; i++) {
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
			var ids = Ids;
			selected_cells.Clear ();
			bit_array.SetAll (false);
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
				return selected_cells.Count;
			}
		}

		public bool Contains (IPhoto item)
		{
			return selected_cells.ContainsKey (item);
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

			var item = parent[num];
			selected_cells[item] = num;
			bit_array.Set (num, true);

			if (notify)
				SignalChange (new List<int> { num });
		}

		public void Add (int start, int end)
		{
			if (start == -1 || end == -1)
				return;

			int current = Math.Min (start, end);
			int final = Math.Max (start, end);
			int count = final - current + 1;
			var ids = new List<int> (count);

			for (int i = 0; i < count; i++) {
				Add (current, false);
				ids[i] = current;
				current++;
			}

			SignalChange (ids);
		}

		public void Remove (int cell, bool notify)
		{
			var item = parent[cell];
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

			int parent_index = selected_cells[item];
			selected_cells.Remove (item);
			bit_array.Set (parent_index, false);

			if (notify)
				SignalChange (new List<int> { parent_index });
		}

		// Remove a range, except the start entry
		public void Remove (int start, int end)
		{
			if (start == -1 || end == -1)
				return;

			int current = Math.Min (start + 1, end);
			int final = Math.Max (start - 1, end);
			int count = final - current + 1;
			var ids = new List<int> (count);

			for (int i = 0; i < count; i++) {
				Remove (current, false);
				ids[i] = current;
				current++;
			}

			SignalChange (ids);
		}

		public int IndexOf (int parentIndex)
		{
			return Ids.IndexOf (parentIndex);
		}

		public int IndexOf (IPhoto item)
		{
			if (!Contains (item))
				return -1;

			int parent_index = selected_cells[item];
			return Ids.IndexOf (parent_index);
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
			var changed_cell = new List<int> (parent.Count);
			for (int i = 0; i < parent.Count; i++) {
				ToggleCell (i, false);
				changed_cell[i] = i;
			}

			SignalChange (changed_cell);
		}

		public event IBrowsableCollectionChangedHandler Changed;
		public event IBrowsableCollectionItemsChangedHandler ItemsChanged;

		public delegate void DetailedCollectionChanged (IBrowsableCollection collection, List<int> ids);

		public event DetailedCollectionChanged DetailedChanged;

		void ClearCached ()
		{
			items = null;
		}

		public void SignalChange (List<int> ids)
		{
			ClearCached ();
			old = Items;

			Changed?.Invoke (this);

			DetailedChanged?.Invoke (this, ids);
		}
	}
}
