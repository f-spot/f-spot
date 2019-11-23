//
// ListViewAccessible_Selection.cs
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
    public partial class ListViewAccessible<T> : Atk.SelectionImplementor
    {
        public void ListViewAccessible_Selection ()
        {
            list_view.SelectionProxy.Changed += OnSelectionChanged;
        }

        public bool AddSelection (int index)
        {
            return AddRowSelection (GetRowAtIndex (index));
        }

        public bool ClearSelection ()
        {
            list_view.Selection.Clear ();
            return true;
        }

        public bool IsChildSelected (int index)
        {
            return IsRowSelected (GetRowAtIndex (index));
        }

        public bool RemoveSelection (int index)
        {
            int row = list_view.Selection.RangeCollection [index / n_columns];
            return RemoveRowSelection (row);
        }

        public Atk.Object RefSelection (int index)
        {
            int row = list_view.Selection.RangeCollection [index / n_columns];
            int column = index % n_columns;
            return RefAt (row, column);
        }

        public int SelectionCount {
            get { return list_view.Selection.Count * n_columns; }
        }

        public bool SelectAllSelection ()
        {
            list_view.Selection.SelectAll ();
            return true;
        }

        private void OnSelectionChanged (object o, EventArgs a)
        {
            GLib.Signal.Emit (this, "selection_changed");
        }
    }
}
