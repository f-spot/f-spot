//
// CellGridView.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
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
using System.Collections.Generic;

using Gtk;
using Gdk;

namespace FSpot.Widgets
{
    /// <summary>
    ///    This class provides the base functionality for displaying cells in a grid. The
    ///    paramters to set up the grid are gathered by abstract properties which must be
    ///    implemented by a subclass.
    /// </summary>
    public abstract class CellGridView : Gtk.Layout
    {

#region Constructors

	protected CellGridView (IntPtr raw) : base (raw)
        {
        }

	protected CellGridView () : base (null, null)
        {
        }

#endregion

#region Abstract Layout Description

        /// <summary>
        ///    Must return the width which each cell should have.
        /// </summary>
        protected abstract int MinCellWidth { get; }

        /// <summary>
        ///    Must return the height which each cell should have.
        /// </summary>
        protected abstract int MinCellHeight { get; }

        /// <summary>
        ///    Must return the number of cells which should be displayed.
        /// </summary>
        protected abstract int CellCount { get; }

#endregion

#region Abstract Drawing Functions

        /// <summary>
        ///   The function is called to draw a Cell.
        /// </summary>
        protected abstract void DrawCell (int cell_num, Rectangle cell_area, Rectangle expose_area);

        /// <summary>
        ///    The function is called to preload a cell.
        /// </summary>
        protected abstract void PreloadCell (int cell_num);

#endregion

#region Private Layout Values

        /// <summary>
        ///    The number of cells per row (columns).
        /// </summary>
        protected int cells_per_row;

        /// <summary>
        ///    The width of each cell. It is set, when the layout is updated. The
        ///    property <see cref="MinCellWidth"/> is only used when the layout is updated.
        /// </summary>
        protected int cell_width;

        /// <summary>
        ///    The height of each cell. It is set, when the layout is updated. The
        ///    property <see cref="MinCellHeight"/> is only used when the layout is updated.
        /// </summary>
        protected int cell_height;

        /// <summary>
        ///    The total number of cells the layout is computed with. It is set, when the
        ///    layout is updated. The property <see cref="CellCount"/> is only used when
        ///    the layout is updated.
        /// </summary>
        private int cell_count;

        /// <summary>
        ///    Holds the number of rows which are displayed at once regarded to the current
        ///    size of the widget.
        /// </summary>
        private int displayed_rows;

        /// <summary>
        ///    The number of rows which are needed to display all cells.
        /// </summary>
        private int total_rows;

        /// <summary>
        ///    The border size the current layout is computed with.
        /// </summary>
        private int border_size = 6;

        /// <summary>
        ///    The maximal number of columns.
        /// </summary>
        private int max_columns = -1;

        // preserve the scroll postion when possible
        private bool scroll;
        private double scroll_value;

        // suppress scroll is currently not used. where do we need it?
        private bool suppress_scroll = false;

#endregion

#region Public Layout Properties

        public int MaxColumns {
            get { return max_columns; }
            set {
                max_columns = value;
                QueueResize ();
            }
        }

        public int BorderSize {
            get { return border_size; }
            set {
                if (value < 0)
                    throw new ArgumentException ("value");

                border_size = value;
                QueueResize ();
            }
        }

        public int VisibleRows {
            get { return displayed_rows; }
        }

        public int VisibleColums {
            get { return cells_per_row; }
        }

#endregion

#region Public Methods

        public int CellAtPosition (Point p)
        {
            return CellAtPosition (p.X, p.Y);
        }

        public int CellAtPosition (int x, int y)
        {
            return CellAtPosition (x, y, true);
        }

        public int CellAtPosition (int x, int y, bool crop_visible)
        {
            if (crop_visible
                && ((y < (int)Vadjustment.Value || y > (int)Vadjustment.Value + Allocation.Height)
                || (x < (int)Hadjustment.Value || x > (int)Hadjustment.Value + Allocation.Width)))
                return -1;

            if (x < border_size || x >= border_size + cells_per_row * cell_width)
                return -1;

            if (y < border_size || y >= border_size + (cell_count / cells_per_row + 1) * cell_height)
                return -1;

            int column = (int) ((x - border_size) / cell_width);
            int row = (int) ((y - border_size) / cell_height);

            int cell_num = column + row * cells_per_row;
            if (cell_num >= cell_count)
                return -1;

            return cell_num;
        }

        public int TopLeftVisibleCell ()
        {
            // TODO: Where does the 8 come from?
            return CellAtPosition (border_size, (int) (Vadjustment.Value + Allocation.Height * (Vadjustment.Value / Vadjustment.Upper)) + border_size + 8);
        }

        public void CellPosition (int cell_num, out int x, out int y)
        {
            // TODO: compare the values with the ones in GetCellCenter.
            if (cells_per_row == 0) {
                x = 0;
                y = 0;
                return;
            }

            int col = cell_num % cells_per_row;
            int row = cell_num / cells_per_row;

            x = col * cell_width + border_size;
            y = row * cell_height + border_size;
        }

        public void CellCenter (int cell_num, out int x, out int y)
        {
            // TODO: compare the values with the ones in GetCellPosition.
            if (cell_num == -1) {
                x = -1;
                y = -1;
            }

            CellPosition (cell_num, out x, out y);

            x += cell_width / 2;
            y += cell_height / 2;
        }

        public Gdk.Rectangle CellBounds (int cell_num)
        {
            Rectangle bounds;

            CellPosition (cell_num, out bounds.X, out bounds.Y);

            bounds.Width = cell_width;
            bounds.Height = cell_height;

            return bounds;
        }

        public IEnumerable<int> CellsInRect (Rectangle area)
        {
            if (cell_width <= 0 || cell_height <= 0) {
                yield break;
            }

            int start_cell_column = Math.Max (0, (area.X - border_size) / cell_width);
            int start_cell_row = Math.Max (0, (area.Y - border_size) / cell_height);

            int end_cell_column = Math.Max (0, (area.X + area.Width - border_size) / cell_width);
            int end_cell_row = Math.Max (0, (area.Y + area.Height - border_size) / cell_height);

            for (int cell_row = start_cell_row; cell_row <= end_cell_row; cell_row ++) {

                for (int cell_column = start_cell_column; cell_column <= end_cell_column; cell_column ++) {

                    int cell_num = cell_column + cell_row * cells_per_row;

                    if (cell_num < cell_count)
                        yield return cell_num;
                }
            }
        }

        public void ScrollTo (int cell_num)
        {
            ScrollTo (cell_num, true);
        }

        public void ScrollTo (int cell_num, bool center)
        {
            if (!IsRealized)
                return;

            Adjustment adjustment = Vadjustment;
            int x;
            int y;

            CellPosition (cell_num, out x, out y);

            if (center)
                y += cell_height / 2 - Allocation.Height / 2;

            // the maximal possible adjustment value
            // (otherwise, we are scrolling to far ...)
            int max = (int) (Height - Allocation.Height);

            adjustment.Value = Math.Min (y, max);
            adjustment.ChangeValue ();
        }

        public void InvalidateCell (int cell_num)
        {
            Rectangle cell_area = CellBounds (cell_num);

            // FIXME where are we computing the bounds incorrectly
            cell_area.Width -= 1;
            cell_area.Height -= 1;

            Gdk.Rectangle visible =
                new Gdk.Rectangle ((int) Hadjustment.Value,
                                   (int) Vadjustment.Value,
                                   Allocation.Width,
                                   Allocation.Height);
            Gdk.Rectangle intersection;
            if (BinWindow != null && cell_area.Intersect (visible, out intersection))
                BinWindow.InvalidateRect (intersection, false);
        }

#endregion

#region Event Handlers

        [GLib.ConnectBefore]
        private void HandleAdjustmentValueChanged (object sender, EventArgs args)
        {
            Scroll ();
        }

#endregion

#region Determine Layout

        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            scroll_value = (Vadjustment.Value)/ (Vadjustment.Upper);
            scroll = ! suppress_scroll;
            suppress_scroll = false;
            UpdateLayout (allocation);

            base.OnSizeAllocated (allocation);
        }

        protected override void OnScrollAdjustmentsSet (Adjustment hadjustment, Adjustment vadjustment)
        {
            base.OnScrollAdjustmentsSet (hadjustment, vadjustment);

            if (vadjustment != null)
		vadjustment.ValueChanged += HandleAdjustmentValueChanged;
        }

        protected override bool OnExposeEvent (Gdk.EventExpose args)
        {
            foreach (Rectangle area in args.Region.GetRectangles ()) {
                DrawAllCells (area);
            }
            return base.OnExposeEvent (args);
        }

        private void UpdateLayout ()
        {
            UpdateLayout (Allocation);
        }

        private void UpdateLayout (Gdk.Rectangle allocation)
        {
            // get the basic values for the layout ...
            cell_width = MinCellWidth;
            cell_height = MinCellHeight;
            cell_count = CellCount;

            // ... and compute the remaining ones.
            int available_width = allocation.Width - 2 * border_size;
            int available_height = allocation.Height - 2 * border_size;

            cells_per_row = Math.Max ((int) (available_width / cell_width), 1);
            if (MaxColumns > 0)
                cells_per_row = Math.Min (MaxColumns, cells_per_row);

            cell_width += (available_width - cells_per_row * cell_width) / cells_per_row;

            displayed_rows = (int) Math.Max (available_height / cell_height, 1);

            total_rows = cell_count / cells_per_row;
            if (cell_count % cells_per_row != 0)
                total_rows ++;

            int height = total_rows * cell_height + 2 * border_size;

            Vadjustment.StepIncrement = cell_height;
            int x = (int)(Hadjustment.Value);
            int y = (int)(height * scroll_value);
            SetSize (x, y, (int) allocation.Width, (int) height);
        }

        private void SetSize (int x, int y, int width, int height)
        {
            Hadjustment.Upper = Math.Max (Allocation.Width, width);
            Vadjustment.Upper = Math.Max (Allocation.Height, height);

            bool xchange = scroll && (int)(Hadjustment.Value) != x;
            bool ychange = scroll && (int)(Vadjustment.Value) != y;

            // reset scroll
            scroll = false;

            if (IsRealized)
                BinWindow.FreezeUpdates ();

            if (xchange || ychange) {
                if (IsRealized)
                    BinWindow.MoveResize (-x, -y, (int)(Hadjustment.Upper), (int)(Vadjustment.Upper));
                Vadjustment.Value = y;
                Hadjustment.Value = x;
            }

            if (this.Width != Allocation.Width || this.Height != Allocation.Height)
                SetSize ((uint)Allocation.Width, (uint)height);

            if (xchange || ychange) {
                Vadjustment.ChangeValue ();
                Hadjustment.ChangeValue ();
            }

            if (IsRealized) {
                BinWindow.ThawUpdates ();
                BinWindow.ProcessUpdates (true);
            }
        }

        private void DrawAllCells (Gdk.Rectangle area)
        {
            foreach (var cell_num in CellsInRect (area)) {
                DrawCell (cell_num, CellBounds (cell_num), area);
            }
        }

        // The first pixel line that is currently on the screen (i.e. in the current
        // scroll region).  Used to compute the area that went offscreen in the "changed"
        // signal handler for the vertical GtkAdjustment.
        private int y_offset;
        private int x_offset;
        private void Scroll ()
        {
            int ystep = (int)(Vadjustment.Value - y_offset);
            int xstep = (int)(Hadjustment.Value - x_offset);

            if (xstep > 0)
                xstep = Math.Max (xstep, Allocation.Width);
            else
                xstep = Math.Min (xstep, -Allocation.Width);

            if (ystep > 0)
                ystep = Math.Max (ystep, Allocation.Height);
            else
                ystep = Math.Min (ystep, -Allocation.Height);

            Gdk.Rectangle area;

            Gdk.Region offscreen = new Gdk.Region ();
            /*
            Log.Debug ("step ({0}, {1}) allocation ({2},{3},{4},{5})",
                    xstep, ystep, Hadjustment.Value, Vadjustment.Value,
                    Allocation.Width, Allocation.Height);
            */
            /*
            area = new Gdk.Rectangle (Math.Max ((int) (Hadjustment.Value + 4 * xstep), 0),
                    Math.Max ((int) (Vadjustment.Value + 4 * ystep), 0),
                    Allocation.Width,
                    Allocation.Height);
            offscreen.UnionWithRect (area);
            area = new Gdk.Rectangle (Math.Max ((int) (Hadjustment.Value + 3 * xstep), 0),
                    Math.Max ((int) (Vadjustment.Value + 3 * ystep), 0),
                    Allocation.Width,
                    Allocation.Height);
            offscreen.UnionWithRect (area);
            */
            area = new Gdk.Rectangle (Math.Max ((int) (Hadjustment.Value + 2 * xstep), 0),
                    Math.Max ((int) (Vadjustment.Value + 2 * ystep), 0),
                    Allocation.Width,
                    Allocation.Height);
            offscreen.UnionWithRect (area);
            area = new Gdk.Rectangle (Math.Max ((int) (Hadjustment.Value + xstep), 0),
                    Math.Max ((int) (Vadjustment.Value + ystep), 0),
                    Allocation.Width,
                    Allocation.Height);
            offscreen.UnionWithRect (area);
            area = new Gdk.Rectangle ((int) Hadjustment.Value,
                    (int) Vadjustment.Value,
                    Allocation.Width,
                    Allocation.Height);

            // always load the onscreen area last to make sure it
            // is first in the loading
            Gdk.Region onscreen = Gdk.Region.Rectangle (area);
            offscreen.Subtract (onscreen);

            PreloadRegion (offscreen, ystep);
            Preload (area, false);

            y_offset = (int) Vadjustment.Value;
            x_offset = (int) Hadjustment.Value;
        }

        private void PreloadRegion (Gdk.Region region, int step)
        {
            Gdk.Rectangle [] rects = region.GetRectangles ();

            if (step < 0)
                Array.Reverse (rects);

            foreach (Gdk.Rectangle preload in rects) {
                Preload (preload, false);
            }
        }

        private void Preload (Gdk.Rectangle area, bool back)
        {
            if (cells_per_row ==0)
                return;

            int start_cell_column = Math.Max ((area.X - border_size) / cell_width, 0);
            int start_cell_row = Math.Max ((area.Y - border_size) / cell_height, 0);
            int start_cell_num = start_cell_column + start_cell_row * cells_per_row;

            int end_cell_column = Math.Max ((area.X + area.Width - border_size) / cell_width, 0);
            int end_cell_row = Math.Max ((area.Y + area.Height - border_size) / cell_height, 0);

            int i;

            int cols = end_cell_column - start_cell_column + 1;
            int rows = end_cell_row - start_cell_row + 1;
            int len = rows * cols;
            int scell = start_cell_num;
            int ecell = scell + len;
            if (scell > cell_count - len) {
                ecell = cell_count;
                scell = Math.Max (0, scell - len);
            } else
                ecell = scell + len;

            int mid = (ecell - scell) / 2;
            for (i = 0; i < mid; i++) {

                // The order of Preloading is kept from the previous version, because it provides
                // smooth appearance (alternating for begin and end of the viewport) of the cells.
                // Maybe, this can be done better in a subclass ? (e.g. by calling a PreloadCells
                // with an Array/Enumeration of all cells to be preloaded, or with lower and upper
                // bound of cells to be preloaded)
                int cell = back ? ecell - i - 1 : scell + mid + i;
                PreloadCell (cell);

                cell = back ? scell + i : scell + mid - i - 1;
                PreloadCell (cell);
            }
        }

#endregion

    }
}

