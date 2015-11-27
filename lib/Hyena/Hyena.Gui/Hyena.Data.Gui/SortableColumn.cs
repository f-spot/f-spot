//
// SortableColumn.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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
using System.Collections;
using System.Collections.Generic;
using Gtk;

namespace Hyena.Data.Gui
{
    public class SortableColumn : Column, ISortableColumn
    {
        private string sort_key;
        private SortType sort_type = SortType.Ascending;
        private Hyena.Query.QueryField field;

        public SortableColumn(string title, ColumnCell cell, double width, string sort_key, bool visible) :
            base(title, cell, width, visible)
        {
            this.sort_key = sort_key;
        }

        public SortableColumn(ColumnCell header_cell, string title, ColumnCell cell, double width, string sort_key, bool visible) :
            base(header_cell, title, cell, width, visible)
        {
            this.sort_key = sort_key;
        }

        public string SortKey {
            get { return sort_key; }
            set { sort_key = value; }
        }

        public SortType SortType {
            get { return sort_type; }
            set { sort_type = value; }
        }

        public Hyena.Query.QueryField Field {
            get { return field; }
            set { field = value; }
        }
    }
}
