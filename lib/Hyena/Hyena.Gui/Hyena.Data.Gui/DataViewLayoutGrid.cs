//
// DataViewLayoutGrid.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2010 Novell, Inc.
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

using Hyena.Gui.Canvas;

namespace Hyena.Data.Gui
{
    public class DataViewLayoutGrid : DataViewLayout
    {
        public int Rows { get; private set; }
        public int Columns { get; private set; }

        public bool Fill { get; set; }

        public Func<CanvasItem> ChildAllocator { get; set; }
        public event EventHandler<EventArgs<int>> ChildCountChanged;

        protected override void InvalidateChildSize ()
        {
            if (Children.Count <= 0) {
                Children.Add (CreateChild ());
            }

            ChildSize = Children[0].Measure (Size.Empty);
        }

        protected override void InvalidateVirtualSize ()
        {
            double model_rows = Model == null ? 0 : Model.Count;
            VirtualSize = new Size (
                ChildSize.Width * Math.Max (Columns, 1),
                ChildSize.Height * Math.Ceiling (model_rows / Math.Max (Columns, 1)));
        }

        protected override void InvalidateChildCollection ()
        {
            Rows = ChildSize.Height > 0
                ? (int)Math.Ceiling ((ActualAllocation.Height +
                    ChildSize.Height) / (double)ChildSize.Height)
                : 0;
            Columns = ChildSize.Width > 0
                ? (int)Math.Max (ActualAllocation.Width / ChildSize.Width, 1)
                : 0;

            ResizeChildCollection (Rows * Columns);

            var handler = ChildCountChanged;
            if (handler != null) {
                handler (this, new EventArgs<int> (Rows * Columns));
            }
        }

        protected override void InvalidateChildLayout (bool arrange)
        {
            base.InvalidateChildLayout (arrange);

            if (ChildSize.Width <= 0 || ChildSize.Height <= 0) {
                // FIXME: empty/reset all child slots here?
                return;
            }

            // Compute where we should start and end in the model
            double offset = ActualAllocation.Y - YPosition % ChildSize.Height;
            int first_model_row = (int)Math.Floor (YPosition / ChildSize.Height) * Columns;
            int last_model_row = first_model_row + Rows * Columns;

            // Setup for the layout iteration
            int child_span_width = (int)Math.Floor (ActualAllocation.Width / Columns);
            int layout_child_index = 0;
            int view_row_index = 0;
            int view_column_index = 0;

            int flex_width = Fill ? 0 : (int)Math.Floor (Math.Max (0, child_span_width - ChildSize.Width) / (Columns + 1));

            // Allocation of the first child in the layout, this
            // will change as we iterate the layout children
            var child_allocation = new Rect () {
                X = ActualAllocation.X + flex_width,
                Y = offset,
                Width = Fill ? child_span_width : ChildSize.Width,
                Height = ChildSize.Height
            };

            // Iterate the layout children and configure them for the current
            // view state to be consumed by interaction and rendering phases
            for (int i = first_model_row; i < last_model_row; i++, layout_child_index++) {
                var child = Children[layout_child_index];
                child.Allocation = child_allocation;
                child.VirtualAllocation = GetChildVirtualAllocation (child_allocation);

                SetModelIndex (child, i);
                if (Model != null) {
                    child.Bind (Model.GetItem (i));
                }

                if (arrange) {
                    child.Arrange ();
                }

                // Update the allocation for the next child
                if (++view_column_index % Columns == 0) {
                    view_row_index++;
                    view_column_index = 0;

                    child_allocation.Y += ChildSize.Height;
                    child_allocation.X = ActualAllocation.X + flex_width;
                } else {
                    child_allocation.X += child_span_width;
                }

                // FIXME: clear any layout children that go beyond the model
            }
        }

        protected virtual CanvasItem CreateChild ()
        {
            if (ChildAllocator == null) {
                throw new InvalidOperationException ("ChildAllocator is unset");
            }

            var child = ChildAllocator ();
            child.Manager = CanvasManager;
            child.Measure (Size.Empty);
            //child.ParentLayout = this;
            return child;
        }

        private void ResizeChildCollection (int newChildCount)
        {
            int difference = Children.Count - newChildCount;
            if (difference > 0) {
                Children.RemoveRange (newChildCount, difference);
            } else {
                for (int i=0; i>difference; i--) {
                    Children.Add (CreateChild ());
                }
            }
        }
    }
}
