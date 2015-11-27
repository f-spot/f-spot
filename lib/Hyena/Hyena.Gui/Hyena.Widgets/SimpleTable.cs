//
// SimpleTable.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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
using System.Collections.Generic;
using Gtk;

namespace Hyena.Widgets
{
    public class SimpleTable<T> : Table
    {
        private bool added_any;

        private List<T> items = new List<T> ();
        private Dictionary<T, Widget []> item_widgets = new Dictionary<T, Widget []> ();
        private AttachOptions default_options = AttachOptions.Fill | AttachOptions.Expand;

        public SimpleTable () : this (2) {}

        public SimpleTable (int n_columns) : base (1, (uint)n_columns, false)
        {
            ColumnSpacing = 5;
            RowSpacing = 5;

            XOptions = new AttachOptions [n_columns];
            for (int i = 0; i < n_columns; i++) {
                XOptions[i] = default_options;
            }
        }

        public void AddRow (T item, params Widget [] cols)
        {
            InsertRow (item, (uint)items.Count, cols);
        }

        public AttachOptions [] XOptions { get; private set; }

        public void InsertRow (T item, uint row, params Widget [] cols)
        {
            if (!added_any) {
                added_any = true;
            } else if (NColumns != cols.Length) {
                throw new ArgumentException ("cols", String.Format ("Expected {0} column widgets, same as previous calls to Add", NColumns));
            }

            Resize ((uint) items.Count + 1, (uint) cols.Length);

            for (int y = items.Count - 1; y >= row; y--) {
                for (uint x = 0; x < NColumns; x++) {
                    var widget = item_widgets[items[y]][x];
                    Remove (widget);
                    Attach (widget, x, x + 1, (uint) y + 1, (uint) y + 2, XOptions[x], default_options, 0, 0);
                }
            }

            items.Insert ((int)row, item);
            item_widgets[item] = cols;

            for (uint x = 0; x < NColumns; x++) {
                Attach (cols[x], x, x + 1, row, row + 1, XOptions[x], default_options, 0, 0);
            }
        }

        public void RemoveRow (T item)
        {
            FreezeChildNotify ();

            foreach (var widget in item_widgets[item]) {
                Remove (widget);
            }

            int index = items.IndexOf (item);
            for (int y = index + 1; y < items.Count; y++) {
                for (uint x = 0; x < NColumns; x++) {
                    var widget = item_widgets[items[y]][x];
                    Remove (widget);
                    Attach (widget, x, x + 1, (uint) y - 1, (uint) y, XOptions[x], default_options, 0, 0);
                }
            }

            Resize ((uint) Math.Max (1, items.Count - 1), NColumns);

            ThawChildNotify ();
            items.Remove (item);
            item_widgets.Remove (item);
        }
    }
}
