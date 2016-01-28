//
// ListView_Interaction.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//   Eitan Isaacson <eitan@ascender.com>
//   Alex Launi <alex.launi@canonical.com>
//
// Copyright (C) 2007-2009 Novell, Inc.
// Copyright (C) 2009 Eitan Isaacson
// Copyright (C) 2010 Alex Launi
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

using Hyena.Collections;
using Hyena.Gui.Canvas;
using Selection = Hyena.Collections.Selection;

namespace Hyena.Data.Gui
{
    public partial class ListView<T> : ListViewBase
    {
        private enum KeyDirection {
            Press,
            Release
        }

        private bool header_focused = false;
        public bool HeaderFocused {
            get { return header_focused; }
            set {
                header_focused = value;
                InvalidateHeader ();
                InvalidateList ();
            }
        }

        #pragma warning disable 0067
        public event EventHandler ActiveColumnChanged;
        #pragma warning restore 0067

        private int active_column = 0;
        public int ActiveColumn {
            get { return active_column; }
            set {
                active_column = value;
                var handler = ActiveColumnChanged;
                if (handler != null) {
                    handler (this, EventArgs.Empty);
                }
            }
        }

        private Adjustment vadjustment;
        public Adjustment Vadjustment {
            get { return vadjustment; }
        }

        private Adjustment hadjustment;
        public Adjustment Hadjustment {
            get { return hadjustment; }
        }

        private SelectionProxy selection_proxy = new SelectionProxy ();
        public SelectionProxy SelectionProxy {
            get { return selection_proxy; }
        }

        public Selection Selection {
            get { return model == null ? null : model.Selection; }
        }

        private int HadjustmentValue {
            get { return hadjustment == null ? 0 : (int)hadjustment.Value; }
        }

        private int VadjustmentValue {
            get { return vadjustment == null ? 0 : (int)vadjustment.Value; }
        }

        public event RowActivatedHandler<T> RowActivated;

#region Row/Selection, Keyboard/Mouse Interaction

        private bool KeyboardScroll (Gdk.ModifierType modifier, int relative_row, bool align_y)
        {
            if (Model == null) {
                return true;
            }

            int row_limit;
            if (relative_row < 0) {
                if (Selection.FocusedIndex == -1) {
                    return false;
                }

                row_limit = 0;
            } else {
                row_limit = Model.Count - 1;
            }

            if (Selection.FocusedIndex == row_limit) {
                return true;
            }

            int scroll_target_item_index = Math.Min (Model.Count - 1, Math.Max (0, Selection.FocusedIndex + relative_row));

            if (Selection != null) {
                if ((modifier & Gdk.ModifierType.ControlMask) != 0) {
                    // Don't change the selection
                } else if ((modifier & Gdk.ModifierType.ShiftMask) != 0) {
                    // Behave like nautilus: if and arrow key + shift is pressed and the currently focused item
                    // is not selected, select it and don't move the focus or vadjustment.
                    // Otherwise, select the new row and scroll etc as necessary.
                    if (relative_row * relative_row != 1) {
                        Selection.SelectFromFirst (scroll_target_item_index, true, false);
                    } else if (Selection.Contains (Selection.FocusedIndex)) {
                        Selection.SelectFromFirst (scroll_target_item_index, true, false);
                    } else {
                        Selection.Select (Selection.FocusedIndex, false);
                        return true;
                    }
                } else {
                    Selection.Clear (false);
                    Selection.Select (scroll_target_item_index, false);
                }
            }

            // Scroll if needed
            double y_at_row = GetViewPointForModelRow (scroll_target_item_index).Y;
            if (align_y) {
                if (y_at_row < VadjustmentValue) {
                    ScrollToY (y_at_row);
                } else if (vadjustment != null) {
                    var bottom_of_item = y_at_row + ChildSize.Height;
                    var bottom_of_view = vadjustment.Value + vadjustment.PageSize;
                    if (bottom_of_item > bottom_of_view) {
                        // Scroll down just enough to put the item fully into view
                        ScrollToY (bottom_of_item - (vadjustment.PageSize));
                    }
                }
            } else if (vadjustment != null) {
                ScrollToY (vadjustment.Value + y_at_row - GetViewPointForModelRow (Selection.FocusedIndex).Y);
            }

            Selection.FocusedIndex = scroll_target_item_index;
            InvalidateList ();
            return true;
        }

        private bool UpdateSelectionForKeyboardScroll (Gdk.ModifierType modifier, int relative_row)
        {
            if (Selection != null) {
                if ((modifier & Gdk.ModifierType.ControlMask) != 0) {
                    // Don't change the selection
                } else {
                    Selection.Notify ();
                }
            }
            return true;
        }

        protected override bool OnKeyPressEvent (Gdk.EventKey press)
        {
            bool handled = false;

            switch (press.Key) {
            case Gdk.Key.a:
                if ((press.State & Gdk.ModifierType.ControlMask) != 0 && Model.Count > 0) {
                    SelectionProxy.Selection.SelectAll ();
                    handled = true;
                }
                break;

            case Gdk.Key.A:
                if ((press.State & Gdk.ModifierType.ControlMask) != 0 && Selection.Count > 0) {
                    SelectionProxy.Selection.Clear ();
                    handled = true;
                }
                break;

            case Gdk.Key.Return:
            case Gdk.Key.KP_Enter:
                if (!HeaderFocused) {
                    handled = ActivateSelection ();
                } else if (HeaderFocused && ActiveColumn >= 0) {
                    OnColumnLeftClicked (
                        column_cache[ActiveColumn].Column);
                    handled = true;
                }
                break;

            case Gdk.Key.Escape:
                handled = CancelColumnDrag ();
                break;

            case Gdk.Key.space:
                if (Selection != null && Selection.FocusedIndex != 1 &&
                    !HeaderFocused) {
                    Selection.ToggleSelect (Selection.FocusedIndex);
                    handled = true;
                }
                break;

            case Gdk.Key.F10:
                if ((press.State & Gdk.ModifierType.ShiftMask) != 0)
                    goto case Gdk.Key.Menu;
                break;

            case Gdk.Key.Menu:
                // OnPopupMenu() is reserved for list items in derived classes.
                if (HeaderFocused) {
                    InvokeColumnHeaderMenu (ActiveColumn);
                    handled = true;
                }
                break;

            default:
                handled = HandleKeyboardScrollKey (press, KeyDirection.Press);
                break;
            }

            return handled ? true : base.OnKeyPressEvent (press);
        }

        private bool HandleKeyboardScrollKey (Gdk.EventKey press, KeyDirection direction)
        {
            bool handled = false;
            // FIXME: hard-coded grid logic here...
            bool grid = ViewLayout != null;
            int items_per_row = grid ? (ViewLayout as DataViewLayoutGrid).Columns : 1;

            switch (press.Key) {
            case Gdk.Key.k:
            case Gdk.Key.K:
            case Gdk.Key.Up:
            case Gdk.Key.KP_Up:
                if (!HeaderFocused) {
                    handled = (direction == KeyDirection.Press)
                        ? KeyboardScroll (press.State, -items_per_row, true)
                        : UpdateSelectionForKeyboardScroll (press.State, -items_per_row);
                }
                break;

            case Gdk.Key.j:
            case Gdk.Key.J:
            case Gdk.Key.Down:
            case Gdk.Key.KP_Down:
                if (direction == KeyDirection.Press) {
                    if (!HeaderFocused) {
                        handled = KeyboardScroll (press.State, items_per_row, true);
                    } else {
                        handled = true;
                        HeaderFocused = false;
                    }
                } else if (!HeaderFocused) {
                    handled = UpdateSelectionForKeyboardScroll (press.State, items_per_row);
                }
                break;

            case Gdk.Key.l:
            case Gdk.Key.L:
            case Gdk.Key.Right:
            case Gdk.Key.KP_Right:
                handled = true;
                if (direction == KeyDirection.Press) {
                    if (grid && !HeaderFocused) {
                        handled = KeyboardScroll (press.State, 1, true);
                    } else if (ActiveColumn + 1 < column_cache.Length) {
                        ActiveColumn++;
                        InvalidateHeader ();
                    }
                } else if (grid && !HeaderFocused) {
                    handled = UpdateSelectionForKeyboardScroll (press.State, 1);
                }
                break;

            case Gdk.Key.h:
            case Gdk.Key.H:
            case Gdk.Key.Left:
            case Gdk.Key.KP_Left:
                handled = true;
                if (direction == KeyDirection.Press) {
                    if (grid && !HeaderFocused) {
                        handled = KeyboardScroll (press.State, -1, true);
                    } else if (ActiveColumn - 1 >= 0) {
                        ActiveColumn--;
                        InvalidateHeader ();
                    }
                } else if (grid && !HeaderFocused) {
                    handled = UpdateSelectionForKeyboardScroll (press.State, -1);
                }
                break;

            case Gdk.Key.Page_Up:
            case Gdk.Key.KP_Page_Up:
                if (!HeaderFocused) {
                    int relativeRow = (int)(-vadjustment.PageIncrement / (double)ChildSize.Height) * items_per_row;
                    handled = vadjustment != null && (direction == KeyDirection.Press
                                                      ? KeyboardScroll (press.State, relativeRow, false)
                                                      : UpdateSelectionForKeyboardScroll (press.State, relativeRow));
                }
                break;

            case Gdk.Key.Page_Down:
            case Gdk.Key.KP_Page_Down:
                if (!HeaderFocused) {
                    int relativeRow = (int)(vadjustment.PageIncrement / (double)ChildSize.Height) * items_per_row;
                    handled = vadjustment != null && (direction == KeyDirection.Press
                                                          ? KeyboardScroll (press.State, relativeRow, false)
                                                          : UpdateSelectionForKeyboardScroll (press.State, relativeRow));
                }
                break;

            case Gdk.Key.Home:
            case Gdk.Key.KP_Home:
                if (!HeaderFocused) {
                    handled = direction == KeyDirection.Press
                        ? KeyboardScroll (press.State, int.MinValue, true)
                        : UpdateSelectionForKeyboardScroll (press.State, int.MinValue);
                }
                break;

            case Gdk.Key.End:
            case Gdk.Key.KP_End:
                if (!HeaderFocused) {
                    handled = direction == KeyDirection.Press
                        ? KeyboardScroll (press.State, int.MaxValue, true)
                        : UpdateSelectionForKeyboardScroll (press.State, int.MaxValue);
                }
                break;
            }

            return handled;
        }

        protected override bool OnKeyReleaseEvent (Gdk.EventKey press)
        {
            return HandleKeyboardScrollKey (press, KeyDirection.Release) ? true : base.OnKeyReleaseEvent (press);
        }

        protected bool ActivateSelection ()
        {
            if (Selection != null && Selection.FocusedIndex != -1) {
                Selection.Clear (false);
                Selection.Select (Selection.FocusedIndex);
                OnRowActivated ();
                return true;
            }
            return false;
        }

#region DataViewLayout Interaction Events

        private CanvasItem last_layout_child;

        private bool LayoutChildHandlesEvent (Gdk.Event evnt, bool press)
        {
            if (ViewLayout == null) {
                return false;
            }

            var point = new Point (0, 0);
            bool handled = false;

            var evnt_button = evnt as Gdk.EventButton;
            var evnt_motion = evnt as Gdk.EventMotion;

            if (evnt_motion != null) {
                point = new Point (evnt_motion.X, evnt_motion.Y);
            } else if (evnt_button != null) {
                point = new Point (evnt_button.X, evnt_button.Y);
            } else if (evnt is Gdk.EventCrossing && last_layout_child != null) {
                last_layout_child.CursorLeaveEvent ();
                last_layout_child = null;
                return false;
            }

            var child = GetLayoutChildAt (point);
            if (child == null) {
                return false;
            }

            point.Offset (-list_interaction_alloc.X, -list_interaction_alloc.Y);
            point.Offset (-child.VirtualAllocation.X, -child.VirtualAllocation.Y);

            if (evnt_motion != null) {
                if (last_layout_child != child) {
                    if (last_layout_child != null) {
                        last_layout_child.CursorLeaveEvent ();
                    }
                    last_layout_child = child;
                    child.CursorEnterEvent ();
                }
                handled = child.CursorMotionEvent (point);
            } else if (evnt_button != null) {
                handled = child.ButtonEvent (point, press, evnt_button.Button);
            }

            return handled;
        }

        private CanvasItem GetLayoutChildAt (Point point)
        {
            point.Offset (-list_interaction_alloc.X, -list_interaction_alloc.Y);
            return ViewLayout.FindChildAtPoint (point);
        }

#endregion

#region Cell Event Proxy (FIXME: THIS ENTIRE SECTION IS OBSOLETE YAY YAY YAY!)

        private IInteractiveCell last_icell;
        private Gdk.Rectangle last_icell_area = Gdk.Rectangle.Zero;

        private void InvalidateLastIcell ()
        {
            if (last_icell != null && last_icell.CursorLeaveEvent ()) {
                QueueDirtyRegion (last_icell_area);
                last_icell = null;
                last_icell_area = Gdk.Rectangle.Zero;
            }
        }

        private void ProxyEventToCell (Gdk.Event evnt, bool press)
        {
            IInteractiveCell icell;
            Gdk.Rectangle icell_area;
            bool redraw = ProxyEventToCell (evnt, press, out icell, out icell_area);

            int xoffset = HadjustmentValue;
            int yoffset = VadjustmentValue;

            if (last_icell_area != icell_area) {
                if (last_icell != null && last_icell.CursorLeaveEvent ()) {
                    QueueDirtyRegion (new Gdk.Rectangle () {
                        X = last_icell_area.X - xoffset,
                        Y = last_icell_area.Y - yoffset,
                        Width = last_icell_area.Width,
                        Height = last_icell_area.Height
                    });
                }
                last_icell = icell;
                last_icell_area = icell_area;
            }

            if (redraw) {
                QueueDirtyRegion (new Gdk.Rectangle () {
                    X = icell_area.X - xoffset,
                    Y = icell_area.Y - yoffset,
                    Width = icell_area.Width,
                    Height = icell_area.Height
                });
            }
        }

        private bool ProxyEventToCell (Gdk.Event evnt, bool press,
            out IInteractiveCell icell, out Gdk.Rectangle icell_area)
        {
            icell = null;
            icell_area = Gdk.Rectangle.Zero;

            int evnt_x, evnt_y;
            int x, y, row_index;
            x = y = row_index = 0;

            var evnt_button = evnt as Gdk.EventButton;
            var evnt_motion = evnt as Gdk.EventMotion;

            if (evnt_motion != null) {
                evnt_x = (int)evnt_motion.X;
                evnt_y = (int)evnt_motion.Y;
            } else if (evnt_button != null) {
                evnt_x = (int)evnt_button.X;
                evnt_y = (int)evnt_button.Y;
            } else {
                // Possibly EventCrossing, for the leave event
                icell = last_icell;
                return false;
            }

            Column column;
            if (!GetEventCell<IInteractiveCell> (evnt_x, evnt_y, out icell, out column, out row_index)) {
                return false;
            }

            x = evnt_x - list_interaction_alloc.X;
            y = evnt_y - list_interaction_alloc.Y;

            // Turn the view-absolute coordinates into cell-relative coordinates
            CachedColumn cached_column = GetCachedColumnForColumn (column);
            x -= cached_column.X1 - HadjustmentValue;
            int page_offset = VadjustmentValue % ChildSize.Height;
            y = (y + page_offset) % ChildSize.Height;

            var view_point = GetViewPointForModelRow (row_index);
            icell_area.Y = (int)view_point.Y + list_interaction_alloc.Y + Allocation.Y;
            icell_area.X = cached_column.X1 + list_rendering_alloc.X;
            icell_area.Width = cached_column.Width;
            icell_area.Height = ChildSize.Height;

            // Send the cell a synthesized input event
            if (evnt_motion != null) {
                return icell.CursorMotionEvent (new Hyena.Gui.Canvas.Point (x, y));
            } else {
                return icell.ButtonEvent (new Hyena.Gui.Canvas.Point (x, y), press, evnt_button.Button);
            }
        }

        #pragma warning disable 0169

        private bool GetEventCell<G> (int x, int y, out G icell, out Column column, out int row_index) where G : class
        {
            icell = null;
            column = null;
            row_index = 0;

            if (Model == null) {
                return false;
            }

            x -= list_interaction_alloc.X;
            y -= list_interaction_alloc.Y;

            row_index = GetModelRowAt (x, y);
            if (row_index < 0 || row_index >= Model.Count) {
                return false;
            }

            column = GetColumnAt (x);
            if (column == null) {
                return false;
            }

            ColumnCell cell = column.GetCell (0);
            icell = cell as G;
            if (icell == null) {
                return false;
            }

            // Bind the row to the cell
            cell.Bind (model[row_index]);
            return true;
        }

        #pragma warning restore 0169

#endregion

#region OnButtonPress

        protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
        {
            HasFocus = true;
            if (header_visible && header_interaction_alloc.Contains ((int)evnt.X, (int)evnt.Y)) {
                HeaderFocused = true;
                return OnHeaderButtonPressEvent (evnt);
            } else if (list_interaction_alloc.Contains ((int)evnt.X, (int)evnt.Y) && model != null) {
                HeaderFocused = false;
                return OnListButtonPressEvent (evnt);
            }
            return true;
        }

        private bool OnHeaderButtonPressEvent (Gdk.EventButton evnt)
        {
            int x = (int)evnt.X - header_interaction_alloc.X;
            int y = (int)evnt.Y - header_interaction_alloc.Y;

            if (evnt.Button == 3 && ColumnController.EnableColumnMenu) {
                Column menu_column = GetColumnAt (x);
                if (menu_column != null) {
                    OnColumnRightClicked (menu_column, x + Allocation.X, y + Allocation.Y);
                }
                return true;
            } else if (evnt.Button != 1) {
                return true;
            }

            Gtk.Drag.SourceUnset (this);

            Column column = GetColumnForResizeHandle (x);
            if (column != null) {
                resizing_column_index = GetCachedColumnForColumn (column).Index;
            } else {
                column = GetColumnAt (x);
                if (column != null) {
                    CachedColumn column_c = GetCachedColumnForColumn (column);
                    pressed_column_index = column_c.Index;
                    pressed_column_x_start = x;
                    pressed_column_x_offset = pressed_column_x_start - column_c.X1;
                    pressed_column_x_start_hadjustment = HadjustmentValue;
                }
            }

            return true;
        }

        private bool OnListButtonPressEvent (Gdk.EventButton evnt)
        {
            if (Model == null) {
                return true;
            }

            int x = (int)evnt.X - list_interaction_alloc.X;
            int y = (int)evnt.Y - list_interaction_alloc.Y;

            GrabFocus ();

            int row_index = GetModelRowAt (x, y);

            if (row_index < 0 || row_index >= Model.Count) {
                Gtk.Drag.SourceUnset (this);
                return true;
            }

            if (LayoutChildHandlesEvent (evnt, true)) {
                return true;
            }

            ProxyEventToCell (evnt, true);

            object item = model[row_index];
            if (item == null) {
                return true;
            }

            if (evnt.Button == 1 && evnt.Type == Gdk.EventType.TwoButtonPress) {
                // Double clicked
                OnRowActivated ();
            } else if (Selection != null) {
                if ((evnt.State & Gdk.ModifierType.ControlMask) != 0) {
                    if (evnt.Button == 3) {
                        // Right clicked with ctrl pressed, so make sure row selected
                        if (!Selection.Contains (row_index)) {
                            Selection.Select (row_index);
                        }
                    } else {
                        // Normal ctrl-click, so toggle
                        Selection.ToggleSelect (row_index);
                    }
                } else if ((evnt.State & Gdk.ModifierType.ShiftMask) != 0) {
                    // Shift-click, so select from first-row-selected (if any) to the current row
                    Selection.SelectFromFirst (row_index, true);
                } else {
                    if (evnt.Button == 3) {
                        // Normal right-click, make sure row is only row selected
                        if (!Selection.Contains (row_index)) {
                            Selection.Clear (false);
                            Selection.Select (row_index);
                        }
                    } else {
                        // Normal click, if row not already selected, select only it right now,
                        // but if it's already selected, wait until the Release to unselect any others so that
                        // drag and drop of 2+ items works.
                        if (!Selection.Contains (row_index)) {
                            Selection.Clear (false);
                            Selection.Select (row_index);
                        }
                    }
                }

                FocusModelRow (row_index);

                // Now that we've worked out the selections, open the context menu
                if (evnt.Button == 3) {
                    OnPopupMenu ();
                }
            }

            InvalidateList ();
            return true;
        }

#endregion

#region OnButtonRelease

        protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
        {
            OnDragSourceSet ();
            StopDragScroll ();

            if (resizing_column_index >= 0) {
                pressed_column_index = -1;
                resizing_column_index = -1;
                GdkWindow.Cursor = null;
                return true;
            }

            if (pressed_column_drag_started) {
                CancelColumnDrag ();
                pressed_column_drag_started = false;
                QueueDraw ();
                return true;
            }

            if (header_visible && header_interaction_alloc.Contains ((int)evnt.X, (int)evnt.Y)) {
                return OnHeaderButtonRelease (evnt);
            } else if (list_interaction_alloc.Contains ((int)evnt.X, (int)evnt.Y) && model != null &&
                (evnt.State & (Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask)) == 0) {
                if (LayoutChildHandlesEvent (evnt, false)) {
                    return true;
                }
                ProxyEventToCell (evnt, false);
                return OnListButtonRelease (evnt);
            }

            return true;
        }

        private bool OnHeaderButtonRelease (Gdk.EventButton evnt)
        {
            if (pressed_column_index >= 0 && pressed_column_index < column_cache.Length) {
                Column column = column_cache[pressed_column_index].Column;
                ActiveColumn = pressed_column_index;
                if (column != null)
                    OnColumnLeftClicked (column);

                pressed_column_index = -1;
                return true;
            } else {
                return false;
            }
        }

        private bool OnListButtonRelease (Gdk.EventButton evnt)
        {
            if (Model == null) {
                return true;
            }

            int x = (int)evnt.X - list_interaction_alloc.X;
            int y = (int)evnt.Y - list_interaction_alloc.Y;

            GrabFocus ();

            int row_index = GetModelRowAt (x, y);

            if (row_index >= Model.Count) {
                return true;
            }

            object item = model[row_index];
            if (item == null) {
                return true;
            }

            //if (Selection != null && Selection.Contains (row_index) && Selection.Count > 1) {
            if (Selection != null && evnt.Button == 1 && Hyena.Gui.GtkUtilities.NoImportantModifiersAreSet ()) {
                if (Selection.Count > 1) {
                    Selection.Clear (false);
                    Selection.Select (row_index);
                    FocusModelRow (row_index);
                    InvalidateList ();
                }
            }

            return true;
        }

#endregion

        protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
        {
            if (evnt == null) {
                throw new ArgumentNullException ("evnt");
            }

            int x = (int)evnt.X - header_interaction_alloc.X;

            if (pressed_column_index >= 0 && !pressed_column_is_dragging &&
                Gtk.Drag.CheckThreshold (this, pressed_column_x_start, 0, x, 0)) {
                pressed_column_is_dragging = true;
                pressed_column_drag_started = true;
                InvalidateHeader ();
                InvalidateList ();
            }

            pressed_column_x = x;

            if (OnMotionNotifyEvent (x)) {
                return true;
            }

            GdkWindow.Cursor = header_interaction_alloc.Contains ((int)evnt.X, (int)evnt.Y) &&
                (resizing_column_index >= 0 || GetColumnForResizeHandle (x) != null)
                ? resize_x_cursor
                : null;

            if (resizing_column_index >= 0) {
                ResizeColumn (x);
            }

            if (LayoutChildHandlesEvent (evnt, false)) {
                return true;
            }

            ProxyEventToCell (evnt, false);

            return true;
        }

        private bool OnMotionNotifyEvent (int x)
        {
            if (!pressed_column_is_dragging) {
                return false;
            }

            OnDragScroll (OnDragHScrollTimeout, header_interaction_alloc.Width * 0.1, header_interaction_alloc.Width, x);

            GdkWindow.Cursor = drag_cursor;

            Column swap_column = GetColumnAt (x);

            if (swap_column != null) {
                CachedColumn swap_column_c = GetCachedColumnForColumn (swap_column);
                bool reorder = false;

                if (swap_column_c.Index < pressed_column_index) {
                    // Moving from right to left
                    reorder = pressed_column_x_drag <= swap_column_c.X1 + swap_column_c.Width / 2;
                } else if (swap_column_c.Index > pressed_column_index) {
                    if (column_cache.Length > pressed_column_index && pressed_column_index >= 0) {
                        // Moving from left to right
                        reorder = pressed_column_x_drag + column_cache[pressed_column_index].Width >=
                            swap_column_c.X1 + swap_column_c.Width / 2;
                    }
                }

                if (reorder) {
                    int actual_pressed_index = ColumnController.IndexOf (column_cache[pressed_column_index].Column);
                    int actual_swap_index = ColumnController.IndexOf (swap_column_c.Column);
                    ColumnController.Reorder (actual_pressed_index, actual_swap_index);
                    pressed_column_index = swap_column_c.Index;
                    RegenerateColumnCache ();
                }
            }

            pressed_column_x_drag = x - pressed_column_x_offset - (pressed_column_x_start_hadjustment - HadjustmentValue);

            QueueDraw ();
            return true;
        }

        private bool OnDragHScrollTimeout ()
        {
            ScrollToY (hadjustment, HadjustmentValue + (drag_scroll_velocity * drag_scroll_velocity_max));
            OnMotionNotifyEvent (pressed_column_x);
            return true;
        }

        protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
        {
            if (evnt.Mode == Gdk.CrossingMode.Normal) {
                GdkWindow.Cursor = null;
                if (LayoutChildHandlesEvent (evnt, false)) {
                    return true;
                }
                ProxyEventToCell (evnt, false);
            }
            return base.OnLeaveNotifyEvent (evnt);
        }

        protected override bool OnFocused (Gtk.DirectionType directionType)
        {
            if (!HeaderVisible)
                return base.OnFocused (directionType);

            if (HasFocus) {
                if (directionType == DirectionType.TabForward && HeaderFocused)
                    HeaderFocused = false;
                else if (directionType == DirectionType.TabBackward && !HeaderFocused)
                    HeaderFocused = true;
                else
                    return base.OnFocused (directionType);

                return true;
            } else {
                if (directionType == DirectionType.TabForward )
                    HeaderFocused = true;
                else if (directionType == DirectionType.TabBackward)
                    HeaderFocused = false;

                return base.OnFocused (directionType);
            }
        }

        protected virtual void OnRowActivated ()
        {
            if (Selection.FocusedIndex != -1) {
                RowActivatedHandler<T> handler = RowActivated;
                if (handler != null) {
                    handler (this, new RowActivatedArgs<T> (Selection.FocusedIndex, model[Selection.FocusedIndex]));
                }
            }
        }

        private bool CancelColumnDrag ()
        {
            if (pressed_column_index >= 0 && pressed_column_is_dragging) {
                pressed_column_is_dragging = false;
                pressed_column_index = -1;
                GdkWindow.Cursor = null;
                QueueDirtyRegion ();
                return true;
            }
            return false;
        }

        // FIXME: replace all invocations with direct call to ViewLayout
        protected int GetModelRowAt (int x, int y)
        {
            if (ViewLayout != null) {
                var child = ViewLayout.FindChildAtPoint (new Point (x, y));
                return child == null ? -1 : ViewLayout.GetModelIndex (child);
            } else {
                if (y < 0 || ChildSize.Height <= 0) {
                    return -1;
                }

                int v_page_offset = VadjustmentValue % ChildSize.Height;
                int first_row = VadjustmentValue / ChildSize.Height;
                int row_offset = (y + v_page_offset) / ChildSize.Height;
                return first_row + row_offset;
            }
        }

        protected Gdk.Point GetViewPointForModelRow (int row)
        {
            // FIXME: hard-coded grid logic
            if (ViewLayout != null) {
                int cols = ((DataViewLayoutGrid)ViewLayout).Columns;
                if (cols == 0 || row == 0) {
                    return new Gdk.Point (0, 0);
                } else {
                    return new Gdk.Point ((row % cols) * ChildSize.Width, (int)(Math.Floor ((double)row / (double)cols) * ChildSize.Height));
                }
            } else {
                return new Gdk.Point (0, ChildSize.Height * row);
            }
        }

        private void FocusModelRow (int index)
        {
            Selection.FocusedIndex = index;
        }

#endregion

#region Adjustments & Scrolling

        private void UpdateAdjustments ()
        {
            UpdateAdjustments (null, null);
        }

        private void UpdateAdjustments (Adjustment hadj, Adjustment vadj)
        {
            if (hadj != null) {
                hadjustment = hadj;
            }

            if (vadj != null) {
                vadjustment = vadj;
            }

            // FIXME: with ViewLayout, hadj and vadj should be unified
            // since the layout will take the header into account...
            if (hadjustment != null) {
                hadjustment.Upper = header_width;
                hadjustment.StepIncrement = 10.0;
                if (hadjustment.Value + hadjustment.PageSize > hadjustment.Upper) {
                    hadjustment.Value = hadjustment.Upper - hadjustment.PageSize;
                }
            }

            if (vadjustment != null && model != null) {
                // FIXME: hard-coded grid logic
                if (ViewLayout != null) {
                    vadjustment.Upper = ViewLayout.VirtualSize.Height;
                    vadjustment.StepIncrement = ViewLayout.ChildSize.Height;
                } else {
                    vadjustment.Upper = ChildSize.Height * model.Count;
                    vadjustment.StepIncrement = ChildSize.Height;
                }

                if (vadjustment.Value + vadjustment.PageSize > vadjustment.Upper) {
                    vadjustment.Value = vadjustment.Upper - vadjustment.PageSize;
                }
            } else if (vadjustment != null) {
                // model is null
                vadjustment.Upper = 0;
                vadjustment.Lower = 0;
            }

            if (hadjustment != null) {
                hadjustment.Change ();
            }

            if (vadjustment != null) {
                vadjustment.Change ();
            }
        }

        private void OnHadjustmentChanged (object o, EventArgs args)
        {
            InvalidateLastIcell ();
            InvalidateHeader ();
            InvalidateList ();

            if (ViewLayout != null) {
                ViewLayout.UpdatePosition (HadjustmentValue, VadjustmentValue);
            }
        }

        private void OnVadjustmentChanged (object o, EventArgs args)
        {
            InvalidateLastIcell ();
            InvalidateList ();

            if (ViewLayout != null) {
                ViewLayout.UpdatePosition (HadjustmentValue, VadjustmentValue);
            }
        }

        public void ScrollToY (double val)
        {
            ScrollToY (vadjustment, val);
        }

        private void ScrollToY (Adjustment adjustment, double val)
        {
            if (adjustment != null) {
                adjustment.Value = Math.Max (0.0, Math.Min (val, adjustment.Upper - adjustment.PageSize));
            }
        }

        public void ScrollTo (int index)
        {
            ScrollToY (GetViewPointForModelRow (index).Y);
        }

        public void CenterOn (int index)
        {
            ScrollTo (index - ItemsInView / 2 + 1);
        }

        public bool IsRowVisible (int index)
        {
            double y = GetViewPointForModelRow (index).Y;
            return vadjustment.Value <= y && y < vadjustment.Value + vadjustment.PageSize;
        }

        protected void CenterOnSelection ()
        {
            if (Selection != null && Selection.Count > 0 && !Selection.AllSelected) {
                bool selection_in_view = false;
                int first_row = GetModelRowAt (0, 0);
                for (int i = 0; i < ItemsInView; i++) {
                    if (Selection.Contains (first_row + i)) {
                        selection_in_view = true;
                        break;
                    }
                }

                if (!selection_in_view) {
                    CenterOn (Selection.Ranges[0].Start);
                }
            }
        }

        protected override void OnSetScrollAdjustments (Adjustment hadj, Adjustment vadj)
        {
            if (hadj == null || vadj == null) {
                return;
            }

            hadj.ValueChanged += OnHadjustmentChanged;
            vadj.ValueChanged += OnVadjustmentChanged;

            UpdateAdjustments (hadj, vadj);
        }

#endregion

    }
}
