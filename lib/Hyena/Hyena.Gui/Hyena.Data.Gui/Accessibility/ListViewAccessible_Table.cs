//
// ListViewAccessible_Table.cs
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
    public partial class ListViewAccessible<T> : Atk.TableImplementor
    {
        public void ListViewAccessible_Table ()
        {
        }

        public Atk.Object Caption {
            get { return new Atk.NoOpObject (list_view); }
            set {}
        }

        public int NColumns {
            get { return n_columns; }
            set {}
        }

        public int NRows {
            get { return n_rows; }
            set {}
        }

        public Atk.Object Summary {
            get { return new Atk.NoOpObject (list_view); }
            set {}
        }

        public bool AddColumnSelection (int column)
        {
            return false;
        }

        public bool AddRowSelection (int row)
        {
            list_view.Selection.Select (row);
            return true;
        }

        public int GetColumnAtIndex (int index)
        {
            return NColumns == 0 ? -1 : (index - NColumns) % NColumns;
        }

        public string GetColumnDescription (int column)
        {
            var col = list_view.ColumnController.Where (c => c.Visible).ElementAtOrDefault (column);
            return col == null ? null : col.LongTitle;
        }

        public int GetColumnExtentAt (int row, int column)
        {
            return 1;
        }

        public Atk.Object GetColumnHeader (int column)
        {
            if (column >= NColumns)
                return new Atk.NoOpObject (list_view);
            else
                return OnRefChild (column);
        }

        public int GetIndexAt (int row, int column)
        {
            return row * NColumns + column + NColumns;
        }

        public int GetRowAtIndex (int index)
        {
            if (NColumns == 0)
                return -1;
            return (index - NColumns) / NColumns;
        }

        public string GetRowDescription (int row)
        {
            return "";
        }

        public int GetRowExtentAt (int row, int column)
        {
            return 1;
        }

        public Atk.Object GetRowHeader (int row)
        {
            return new Atk.NoOpObject (list_view);
        }

// Ensure https://bugzilla.novell.com/show_bug.cgi?id=512477 is fixed
        private static readonly int [] empty_int_array = new int[0];
        public int [] SelectedColumns {
            get { return empty_int_array; }
        }

        public int [] SelectedRows {
            get { return list_view.Selection.ToArray (); }
        }

        public bool IsColumnSelected (int column)
        {
            return false;
        }

        public bool IsRowSelected (int row)
        {
            return list_view.Selection.Contains (row);
        }

        public bool IsSelected (int row, int column)
        {
            return list_view.Selection.Contains (row);
        }

        public Atk.Object RefAt (int row, int column)
        {
            int index = NColumns * row + column + NColumns;
            return OnRefChild (index);
        }

        public bool RemoveColumnSelection (int column)
        {
            return false;
        }

        public bool RemoveRowSelection (int row)
        {
            list_view.Selection.Unselect (row);
            return true;
        }

        public void SetColumnDescription (int column, string description)
        {
        }

        public void SetColumnHeader (int column, Atk.Object header)
        {
        }

        public void SetRowDescription (int row, string description)
        {
        }

        public void SetRowHeader (int row, Atk.Object header)
        {
        }
    }
}
