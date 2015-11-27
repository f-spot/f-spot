//
// ListViewAccessible.cs
//
// Authors:
//   Eitan Isaacson <eitan@ascender.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Eitan Isaacson
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Linq;
using System.Collections.Generic;

using Hyena.Data.Gui;

namespace Hyena.Data.Gui.Accessibility
{
    public partial class ListViewAccessible<T> : Hyena.Gui.BaseWidgetAccessible, ICellAccessibleParent
    {
        private ListView<T> list_view;
        private Dictionary<int, ColumnCellAccessible> cell_cache;

        public ListViewAccessible (GLib.Object widget) : base (widget as Gtk.Widget)
        {
            list_view = (ListView<T>) widget;
            // TODO replace with list_view.Name?
            Name = "ListView";
            Description = "ListView";
            Role = Atk.Role.Table;
            if (list_view.Parent != null) {
                Parent = list_view.Parent.RefAccessible ();
            }

            cell_cache = new Dictionary<int, ColumnCellAccessible> ();

            list_view.ModelChanged += OnModelChanged;
            list_view.ModelReloaded += OnModelChanged;
            OnModelChanged (null, null);

            list_view.SelectionProxy.FocusChanged += OnSelectionFocusChanged;
            list_view.ActiveColumnChanged += OnSelectionFocusChanged;

            ListViewAccessible_Selection ();
            ListViewAccessible_Table ();
        }

        protected ListViewAccessible (IntPtr raw) : base (raw)
        {
        }

        protected override Atk.StateSet OnRefStateSet ()
        {
            Atk.StateSet states = base.OnRefStateSet ();
            states.AddState (Atk.StateType.ManagesDescendants);

            return states;
        }

        protected override int OnGetIndexInParent ()
        {
            for (int i=0; i < Parent.NAccessibleChildren; i++) {
                if (Parent.RefAccessibleChild (i) == this) {
                    return i;
                }
            }

            return -1;
        }

        protected override int OnGetNChildren ()
        {
            return n_columns * n_rows + n_columns;
        }

        protected override Atk.Object OnRefChild (int index)
        {
            ColumnCellAccessible child;

            if (cell_cache.ContainsKey (index)) {
                return cell_cache[index];
            }

            // FIXME workaround to prevent crashing on Grid ListViews
            if (list_view.ColumnController == null)
                return null;

            var columns = list_view.ColumnController.Where (c => c.Visible);

            if (index - n_columns < 0) {
                child = columns.ElementAtOrDefault (index)
                               .HeaderCell
                               .GetAccessible (this) as ColumnCellAccessible;
            } else {
                int column = (index - n_columns) % n_columns;
                int row = (index - n_columns) / n_columns;
                var cell = columns.ElementAtOrDefault (column).GetCell (0);
                cell.Bind (list_view.Model[row]);
                child = (ColumnCellAccessible) cell.GetAccessible (this);
            }

            cell_cache.Add (index, child);

            return child;
        }

        public override Atk.Object RefAccessibleAtPoint (int x, int y, Atk.CoordType coordType)
        {
            int row, col;
            list_view.GetCellAtPoint (x, y, coordType, out row, out col);
            return RefAt (row, col);
        }


        private void OnModelChanged (object o, EventArgs a)
        {
            ThreadAssist.ProxyToMain (EmitModelChanged);
        }

        private void EmitModelChanged ()
        {
            GLib.Signal.Emit (this, "model_changed");
            cell_cache.Clear ();
            /*var handler = ModelChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }*/
        }

        private void OnSelectionFocusChanged (object o, EventArgs a)
        {
            ThreadAssist.ProxyToMain (EmitDescendantChanged);
        }

        private void EmitDescendantChanged ()
        {
            var cell = ActiveCell;
            if (cell != null) {
                GLib.Signal.Emit (this, "active-descendant-changed", cell.Handle);
            }
        }

        private Atk.Object ActiveCell {
            get {
                if (list_view.HeaderFocused) {
                    return OnRefChild (list_view.ActiveColumn);
                } else {
                    if (list_view.Selection != null) {
                        return RefAt (list_view.Selection.FocusedIndex, list_view.ActiveColumn);
                    } else {
                        return null;
                    }
                }
            }
        }

        private int n_columns {
            get {
                return list_view.ColumnController != null
                    ? list_view.ColumnController.Count (c => c.Visible)
                    : 1;
            }
        }

        private int n_rows {
            get { return list_view.Model.Count; }
        }

        #region ICellAccessibleParent

        public int GetCellIndex (ColumnCellAccessible cell)
        {
            foreach (KeyValuePair<int, ColumnCellAccessible> kv in cell_cache)
            {
                if ((ColumnCellAccessible)kv.Value == cell)
                    return (int)kv.Key;
            }

            return -1;
        }

        public Gdk.Rectangle GetCellExtents (ColumnCellAccessible cell, Atk.CoordType coord_type)
        {
            int cache_index = GetCellIndex (cell);
            int minval = Int32.MinValue;
            if (cache_index == -1)
                return new Gdk.Rectangle (minval, minval, minval, minval);

            if (cache_index - n_columns >= 0)
            {
                int column = (cache_index - NColumns)%NColumns;
                int row = (cache_index - NColumns)/NColumns;
                return list_view.GetColumnCellExtents (row, column, true, coord_type);
            } else
            {
                return list_view.GetColumnHeaderCellExtents (cache_index, true, coord_type);
            }
        }

        public bool IsCellShowing (ColumnCellAccessible cell)
        {
            Gdk.Rectangle cell_extents = GetCellExtents (cell, Atk.CoordType.Window);

            if (cell_extents.X == Int32.MinValue && cell_extents.Y == Int32.MinValue)
                return false;

            return true;
        }

        public bool IsCellFocused (ColumnCellAccessible cell)
        {
            int cell_index = GetCellIndex (cell);
            if (cell_index % NColumns != 0)
                return false; // Only 0 column cells get focus now.

            int row = cell_index / NColumns;

            return row == list_view.Selection.FocusedIndex;
        }

        public bool IsCellSelected (ColumnCellAccessible cell)
        {
            return IsChildSelected (GetCellIndex (cell));
        }

        public bool IsCellActive (ColumnCellAccessible cell)
        {
            return (ActiveCell == (Atk.Object)cell);
        }

        public void InvokeColumnHeaderMenu (ColumnCellAccessible cell)
        {
            list_view.InvokeColumnHeaderMenu (GetCellIndex (cell));
        }

        public void ClickColumnHeader (ColumnCellAccessible cell)
        {
            list_view.ClickColumnHeader (GetCellIndex (cell));
        }

        public void CellRedrawn (int column, int row)
        {
            int index;
            if (row >= 0)
                index = row * n_columns + column + n_columns;
            else
                index = column;

            if (cell_cache.ContainsKey (index)) {
                cell_cache[index].Redrawn ();
            }
        }

        #endregion
    }
}
