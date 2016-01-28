//
// ColumnHeaderCellText.cs
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
using Gtk;
using Cairo;

using Hyena.Data.Gui.Accessibility;

namespace Hyena.Data.Gui
{
    public class ColumnHeaderCellText : ColumnCellText, IHeaderCell
    {
        public new delegate Column DataHandler ();

        private DataHandler data_handler;
        private bool has_sort;

        public ColumnHeaderCellText (DataHandler data_handler) : base (null, true)
        {
            UseMarkup = true;
            this.data_handler = data_handler;
        }

        public override Atk.Object GetAccessible (ICellAccessibleParent parent)
        {
            return new  ColumnHeaderCellTextAccessible (BoundObject, this, parent);
        }

        public override void Render (CellContext context, double cellWidth, double cellHeight)
        {
            if (data_handler == null) {
                return;
            }

            if (!has_sort) {
                base.Render (context, cellWidth, cellHeight);
                return;
            }

            Gdk.Rectangle arrow_alloc = new Gdk.Rectangle ();
            arrow_alloc.Width = (int)(cellHeight / 3.0);
            arrow_alloc.Height = (int)((double)arrow_alloc.Width / 1.6);
            arrow_alloc.X = (int)cellWidth - arrow_alloc.Width - (int)Padding.Left;
            arrow_alloc.Y = ((int)cellHeight - arrow_alloc.Height) / 2;

            double textWidth = arrow_alloc.X - Padding.Left;
            if (textWidth > 0) {
                base.Render (context, textWidth, cellHeight);
            }

            SortType sort_type = ((ISortableColumn)data_handler ()).SortType;
            if (sort_type != SortType.None) {
                context.Theme.DrawArrow (context.Context, arrow_alloc, sort_type);
            }
        }

        protected override string GetText (object obj)
        {
            return data_handler ().Title;
        }

        public bool HasSort {
            get { return has_sort; }
            set { has_sort = value; }
        }

        public static int GetArrowWidth (int headerHeight)
        {
            return (int)(headerHeight / 3.0) + 4;
        }
    }
}
