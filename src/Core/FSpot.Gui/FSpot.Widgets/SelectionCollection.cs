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
using System;
using System.Collections;
using System.Collections.Generic;

using System.Linq;

using FSpot.Core;

namespace FSpot.Widgets
{
	public class SelectionCollection : IBrowsableCollection
	{
		IBrowsableCollection parent;
		Dictionary<IPhoto, int> selected_cells;
		BitArray bit_array;
		IPhoto[] items;
		IPhoto[] old;

		public SelectionCollection (IBrowsableCollection collection)
		{
			selected_cells = new Dictionary<IPhoto, int> ();
			parent = collection;
			bit_array = new BitArray (this.parent.Count);
			parent.Changed += HandleParentChanged;
			parent.ItemsChanged += HandleParentItemsChanged;
		}

		private void HandleParentChanged (IBrowsableCollection collection)
		{
			IPhoto [] local = old;
			selected_cells.Clear ();
			bit_array = new BitArray (parent.Count);
			ClearCached ();

			if (old != null) {
				int i = 0;

				for (i = 0; i < local.Length; i++) {
					int parent_index = parent.IndexOf (local [i]);
					if (parent_index >= 0)
						Add (parent_index, false);
				}
			}

			// Call the directly so that we don't reset old immediately this way the old selection
			// set isn't actually lost until we change it.
			if (Changed != null)
				Changed (this);

			if (DetailedChanged != null)
				DetailedChanged (this, null);
		}

		public void MarkChanged (int item, IBrowsableItemChanges changes)
		{
			throw new NotImplementedException ();
		}

		private void HandleParentItemsChanged (IBrowsableCollection collection, BrowsableEventArgs args)
		{
			if (ItemsChanged == null)
				return;

			List<int> local_ids = new List<int> ();
			foreach (int parent_index in args.Items) {
				// If the item isn't part of the selection ignore it
				if (!Contains (collection [parent_index]))
					return;

				int local_index = IndexOf (parent_index);
				if (local_index >= 0)
					local_ids.Add (local_index);
			}

			if (local_ids.Count == 0)
				return;

			int [] items = local_ids.ToArray ();
			ItemsChanged (this, new BrowsableEventArgs (items, args.Changes));
		}

		public BitArray ToBitArray ()
		{
			return new BitArray (bit_array);
		}

		public int [] Ids {
			get {
				// TODO: use IEnumerable<>
				return (from i in selected_cells.Values orderby i select i).ToArray ();
			}
		}

		public IPhoto this [int index] {
			get {
				int [] ids = Ids;
				return parent [ids [index]];
			}
		}

		public IPhoto [] Items {
			get {
				if (items != null)
					return items;

				int [] ids = Ids;
				items = new IPhoto [ids.Length];
				for (int i = 0; i < items.Length; i++) {
					items [i] = parent [ids [i]];
				}
				return items;
			}
		}

		public void Clear (bool update = true)
		{
			int [] ids = Ids;
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

			return Contains (parent [num]);
		}

		public void Add (int num, bool notify = true)
		{
			if (num == -1)
				return;

			if (Contains (num))
				return;

			IPhoto item = parent [num];
			selected_cells [item] = num;
			bit_array.Set (num, true);

			if (notify)
				SignalChange (new int [] {num});
		}

		public void Add (int start, int end)
		{
			if (start == -1 || end == -1)
				return;

			int current = Math.Min (start, end);
			int final = Math.Max (start, end);
			int count = final - current + 1;
			int [] ids = new int [count];

			for (int i = 0; i < count; i++) {
				Add (current, false);
				ids [i] = current;
				current++;
			}

			SignalChange (ids);
		}

		public void Remove (int cell, bool notify = true)
		{
			IPhoto item = parent [cell];
			if (item != null)
				Remove (item, notify);

		}

		private void Remove (IPhoto item, bool notify = true)
		{
			if (item == null)
				return;

			int parent_index = (int)selected_cells [item];
			selected_cells.Remove (item);
			bit_array.Set (parent_index, false);

			if (notify)
				SignalChange (new int [] {parent_index});
		}

		// Remove a range, except the start entry
		public void Remove (int start, int end)
		{
			if (start == -1 || end == -1)
				return;

			int current = Math.Min (start + 1, end);
			int final = Math.Max (start - 1, end);
			int count = final - current + 1;
			int [] ids = new int [count];

			for (int i = 0; i < count; i++) {
				Remove (current, false);
				ids [i] = current;
				current++;
			}

			SignalChange (ids);
		}

		public int IndexOf (int parent_index)
		{
			return System.Array.IndexOf (Ids, parent_index);
		}

		public int IndexOf (IPhoto item)
		{
			if (!Contains (item))
				return -1;

			int parent_index = (int)selected_cells [item];
			return System.Array.IndexOf (Ids, parent_index);
		}

		public void ToggleCell (int cell_num, bool notify = true)
		{
			if (Contains (cell_num))
				Remove (cell_num, notify);
			else
				Add (cell_num, notify);
		}

		public void SelectionInvert ()
		{
			int [] changed_cell = new int[parent.Count];
			for (int i = 0; i < parent.Count; i++) {
				ToggleCell (i, false);
				changed_cell [i] = i;
			}

			SignalChange (changed_cell);
		}

		public event IBrowsableCollectionChangedHandler Changed;
		public event IBrowsableCollectionItemsChangedHandler ItemsChanged;

		public delegate void DetailedCollectionChanged (IBrowsableCollection collection,int[] ids);

		public event DetailedCollectionChanged DetailedChanged;

		private void ClearCached ()
		{
			items = null;
		}

		public void SignalChange (int [] ids)
		{
			ClearCached ();
			old = Items;


			if (Changed != null)
				Changed (this);

			if (DetailedChanged != null)
				DetailedChanged (this, ids);
		}
	}
}
