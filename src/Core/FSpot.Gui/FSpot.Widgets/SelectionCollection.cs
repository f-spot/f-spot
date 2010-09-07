using System;
using System.Collections;

using FSpot.Core;

namespace FSpot.Widgets
{
    public class SelectionCollection : IBrowsableCollection
    {
        IBrowsableCollection parent;
        Hashtable selected_cells;
        BitArray bit_array;
        int [] selection;
        IPhoto [] items;
        IPhoto [] old;

        public SelectionCollection (IBrowsableCollection collection)
        {
            this.selected_cells = new Hashtable ();
            this.parent = collection;
            this.bit_array = new BitArray (this.parent.Count);
            this.parent.Changed += HandleParentChanged;
            this.parent.ItemsChanged += HandleParentItemsChanged;
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
                        this.Add (parent_index, false);
                }
            }

            // Call the directly so that we don't reset old immediately this way the old selection
            // set isn't actually lost until we change it.
            if (this.Changed != null)
                Changed (this);

            if (this.DetailedChanged != null)
                DetailedChanged (this, null);

        }

        public void MarkChanged (int item, IBrowsableItemChanges changes)
        {
            // Forward the change event up to our parent
            // we'll fire the event when the parent calls us back
            parent.MarkChanged ((int) selected_cells [item], changes);
        }

        private void HandleParentItemsChanged (IBrowsableCollection collection, BrowsableEventArgs args)
        {
            if (this.ItemsChanged == null)
                return;

            ArrayList local_ids = new ArrayList ();
            foreach (int parent_index in args.Items) {
                // If the item isn't part of the selection ignore it
                if (!this.Contains (collection [parent_index]))
                    return;

                int local_index = this.IndexOf (parent_index);
                if (local_index >= 0)
                    local_ids.Add (local_index);
            }

            if (local_ids.Count == 0)
                return;

            int [] items = (int [])local_ids.ToArray (typeof (int));
            ItemsChanged (this, new BrowsableEventArgs (items, args.Changes));
        }

        public BitArray ToBitArray () {
            return new BitArray (bit_array);
        }

        public int [] Ids {
            get {
                if (selection != null)
                    return selection;

                selection = new int [selected_cells.Count];

                int i = 0;
                foreach (int cell in selected_cells.Values)
                    selection [i ++] = cell;

                Array.Sort (selection);
                return selection;
            }
        }

        public IPhoto this [int index] {
            get {
                int [] ids = this.Ids;
                return parent [ids[index]];
            }
        }

        public IPhoto [] Items {
            get {
                if (items != null)
                    return items;

                int [] ids = this.Ids;
                items = new IPhoto [ids.Length];
                for (int i = 0; i < items.Length; i++) {
                    items [i] = parent [ids[i]];
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
            int [] ids = Ids;
            selected_cells.Clear ();
            bit_array.SetAll (false);
            SignalChange (ids);
        }

        public void Add (IPhoto item)
        {
            if (this.Contains (item))
                return;

            int index = parent.IndexOf (item);
            this.Add (index);
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

            return this.Contains (parent [num]);
        }

        public void Add (int num)
        {
            this.Add (num, true);
        }

        public void Add (int num, bool notify)
        {
            if (num == -1)
                return;

            if (this.Contains (num))
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
                this.Add (current, false);
                ids [i] = current;
                current++;
            }

            SignalChange (ids);
        }

        public void Remove (int cell, bool notify)
        {
            IPhoto item = parent [cell];
            if (item != null)
                this.Remove (item, notify);

        }

        public void Remove (IPhoto item)
        {
            Remove (item, true);
        }

        public void Remove (int cell)
        {
            Remove (cell, true);
        }

        private void Remove (IPhoto item, bool notify)
        {
            if (item == null)
                return;

            int parent_index = (int) selected_cells [item];
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
                this.Remove (current, false);
                ids [i] = current;
                current++;
            }

            SignalChange (ids);
        }

        public int IndexOf (int parent_index)
        {
            return System.Array.IndexOf (this.Ids, parent_index);
        }

        public int IndexOf (IPhoto item)
        {
            if (!this.Contains (item))
                return -1;

            int parent_index = (int) selected_cells [item];
            return System.Array.IndexOf (Ids, parent_index);
        }

        public void ToggleCell (int cell_num, bool notify)
        {
            if (Contains (cell_num))
                Remove (cell_num, notify);
            else
                Add (cell_num, notify);
        }

        public void ToggleCell (int cell_num)
        {
            ToggleCell (cell_num, true);
        }

        public void SelectionInvert ()
        {
            int [] changed_cell = new int[parent.Count];
            for (int i = 0; i < parent.Count; i++) {
                ToggleCell (i, false);
                changed_cell[i] = i;
            }

            SignalChange (changed_cell);
        }

        public void SelectRect (int start_row, int end_row, int start_line, int end_line, int cells_per_row)
        {
            for (int row = start_row; row < end_row; row++)
                for (int line = start_line; line < end_line; line++) {
                    int index = line*cells_per_row + row;
                    if (index < parent.Count)
                        Add (index, false);
                }
        }

        public void ToggleRect (int start_row, int end_row, int start_line, int end_line, int cells_per_row)
        {
            for  (int row = start_row; row < end_row; row++)
                for (int line = start_line; line < end_line; line++) {
                    int index = line*cells_per_row + row;
                    if (index < parent.Count)
                        ToggleCell (index, false);
                }
        }


        public event IBrowsableCollectionChangedHandler Changed;
        public event IBrowsableCollectionItemsChangedHandler ItemsChanged;

        public delegate void DetailedCollectionChanged (IBrowsableCollection collection, int [] ids);
        public event DetailedCollectionChanged DetailedChanged;

        private void ClearCached ()
        {
            selection = null;
            items = null;
        }

        public void SignalChange (int [] ids)
        {
            ClearCached ();
            old = this.Items;


            if (Changed != null)
                Changed (this);

            if (DetailedChanged!= null)
                DetailedChanged (this, ids);
        }
    }
}

