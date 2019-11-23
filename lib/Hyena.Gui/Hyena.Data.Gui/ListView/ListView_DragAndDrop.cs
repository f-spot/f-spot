//
// ListView_DragAndDrop.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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

namespace Hyena.Data.Gui
{
    public static class ListViewDragDropTarget
    {
        public enum TargetType
        {
            ModelSelection
        }

        public static readonly TargetEntry ModelSelection =
            new TargetEntry ("application/x-hyena-data-model-selection", TargetFlags.App,
                (uint)TargetType.ModelSelection);
    }

    public partial class ListView<T> : ListViewBase
    {
        private static TargetEntry [] drag_drop_dest_entries = new TargetEntry [] {
            ListViewDragDropTarget.ModelSelection
        };

        protected virtual TargetEntry [] DragDropDestEntries {
            get { return drag_drop_dest_entries; }
        }

        protected virtual TargetEntry [] DragDropSourceEntries {
            get { return drag_drop_dest_entries; }
        }

        private bool is_reorderable = false;
        public bool IsReorderable {
            get { return is_reorderable && IsEverReorderable; }
            set {
                is_reorderable = value;
                OnDragSourceSet ();
                OnDragDestSet ();
            }
        }

        private bool is_ever_reorderable = false;
        public bool IsEverReorderable {
            get { return is_ever_reorderable; }
            set {
                is_ever_reorderable = value;
                OnDragSourceSet ();
                OnDragDestSet ();
            }
        }

        private bool force_drag_source_set = false;
        protected bool ForceDragSourceSet {
            get { return force_drag_source_set; }
            set {
                force_drag_source_set = true;
                OnDragSourceSet ();
            }
        }

        private bool force_drag_dest_set = false;
        protected bool ForceDragDestSet {
            get { return force_drag_dest_set; }
            set {
                force_drag_dest_set = true;
                OnDragDestSet ();
            }
        }

        protected virtual void OnDragDestSet ()
        {
            if (ForceDragDestSet || IsReorderable) {
                Gtk.Drag.DestSet (this, DestDefaults.All, DragDropDestEntries, Gdk.DragAction.Move);
            } else {
                Gtk.Drag.DestUnset (this);
            }
        }

        protected virtual void OnDragSourceSet ()
        {
            if (ForceDragSourceSet || IsReorderable) {
                Gtk.Drag.SourceSet (this, Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
                    DragDropSourceEntries, Gdk.DragAction.Copy | Gdk.DragAction.Move);
            } else {
                Gtk.Drag.SourceUnset (this);
            }
        }

        private uint drag_scroll_timeout_id;
        private uint drag_scroll_timeout_duration = 50;
        private double drag_scroll_velocity;
        private double drag_scroll_velocity_max = 100.0;
        private int drag_reorder_row_index = -1;
        private int drag_reorder_motion_y = -1;

        private void StopDragScroll ()
        {
            drag_scroll_velocity = 0.0;

            if (drag_scroll_timeout_id > 0) {
                GLib.Source.Remove (drag_scroll_timeout_id);
                drag_scroll_timeout_id = 0;
            }
        }

        private void OnDragScroll (GLib.TimeoutHandler handler, double threshold, int total, int position)
        {
            if (position < threshold) {
                drag_scroll_velocity = -1.0 + (position / threshold);
            } else if (position > total - threshold) {
                drag_scroll_velocity = 1.0 - ((total - position) / threshold);
            } else {
                StopDragScroll ();
                return;
            }

            if (drag_scroll_timeout_id == 0) {
                drag_scroll_timeout_id = GLib.Timeout.Add (drag_scroll_timeout_duration, handler);
            }
        }

        protected override bool OnDragMotion (Gdk.DragContext context, int x, int y, uint time)
        {
            if (!IsReorderable) {
                StopDragScroll ();
                drag_reorder_row_index = -1;
                drag_reorder_motion_y = -1;
                InvalidateList ();
                return false;
            }

            drag_reorder_motion_y = y;
            DragReorderUpdateRow ();

            OnDragScroll (OnDragVScrollTimeout, Allocation.Height * 0.3, Allocation.Height, y);

            return true;
        }

        protected override void OnDragLeave (Gdk.DragContext context, uint time)
        {
            StopDragScroll ();
        }

        protected override void OnDragEnd (Gdk.DragContext context)
        {
            StopDragScroll ();
            drag_reorder_row_index = -1;
            drag_reorder_motion_y = -1;
            InvalidateList ();
        }

        private bool OnDragVScrollTimeout ()
        {
            ScrollToY (VadjustmentValue + (drag_scroll_velocity * drag_scroll_velocity_max));
            DragReorderUpdateRow ();
            return true;
        }

        private void DragReorderUpdateRow ()
        {
            int row = GetDragRow (drag_reorder_motion_y);
            if (row != drag_reorder_row_index) {
                drag_reorder_row_index = row;
                InvalidateList ();
            }
        }

        protected int GetDragRow (int y)
        {
            y = TranslateToListY (y);
            int row = GetModelRowAt (0, y);

            if (row == -1) {
                return -1;
            }

            if (row != GetModelRowAt (0, y + ChildSize.Height / 2)) {
                row++;
            }

            return row;
        }
    }
}
