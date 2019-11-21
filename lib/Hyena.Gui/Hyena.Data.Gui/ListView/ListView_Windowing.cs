//
// ListView_Windowing.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using Gdk;
using Gtk;

using Hyena.Gui.Theming;

namespace Hyena.Data.Gui
{
    public partial class ListView<T> : ListViewBase
    {
        private Rectangle list_rendering_alloc;
        private Rectangle header_rendering_alloc;
        private Rectangle list_interaction_alloc;
        private Rectangle header_interaction_alloc;

        private Gdk.Window event_window;

        protected Rectangle ListAllocation {
            get { return list_rendering_alloc; }
        }

        protected Gdk.Window EventWindow {
            get { return event_window; }
        }

        protected override void OnRealized ()
        {
            WidgetFlags |= WidgetFlags.Realized | WidgetFlags.NoWindow;

            GdkWindow = Parent.GdkWindow;
            cell_context.Drawable = GdkWindow;

            WindowAttr attributes = new WindowAttr ();
            attributes.WindowType = Gdk.WindowType.Child;
            attributes.X = Allocation.X;
            attributes.Y = Allocation.Y;
            attributes.Width = Allocation.Width;
            attributes.Height = Allocation.Height;
            attributes.Wclass = WindowClass.InputOnly;
            attributes.EventMask = (int)(
                EventMask.PointerMotionMask |
                EventMask.KeyPressMask |
                EventMask.KeyReleaseMask |
                EventMask.ButtonPressMask |
                EventMask.ButtonReleaseMask |
                EventMask.LeaveNotifyMask |
                EventMask.ExposureMask);

            WindowAttributesType attributes_mask =
                WindowAttributesType.X | WindowAttributesType.Y | WindowAttributesType.Wmclass;

            event_window = new Gdk.Window (GdkWindow, attributes, attributes_mask);
            event_window.UserData = Handle;

            OnDragSourceSet ();
            MoveResize (Allocation);

            base.OnRealized ();
        }

        protected override void OnUnrealized ()
        {
            WidgetFlags &= ~WidgetFlags.Realized;

            event_window.UserData = IntPtr.Zero;
            Hyena.Gui.GtkWorkarounds.WindowDestroy (event_window);
            event_window = null;

            base.OnUnrealized ();
        }

        protected override void OnMapped ()
        {
            WidgetFlags |= WidgetFlags.Mapped;
            event_window.Show ();
        }

        protected override void OnUnmapped ()
        {
            WidgetFlags &= ~WidgetFlags.Mapped;
            event_window.Hide ();
        }

        protected int TranslateToListY (int y)
        {
            return y - list_interaction_alloc.Y;
        }

        private void MoveResize (Rectangle allocation)
        {
            if (Theme == null) {
                return;
            }

            header_rendering_alloc = allocation;
            header_rendering_alloc.Height = HeaderHeight;

            list_rendering_alloc.X = header_rendering_alloc.X + Theme.TotalBorderWidth;
            list_rendering_alloc.Y = header_rendering_alloc.Bottom + Theme.TotalBorderWidth;
            list_rendering_alloc.Width = allocation.Width - Theme.TotalBorderWidth * 2;
            list_rendering_alloc.Height = allocation.Height - (list_rendering_alloc.Y - allocation.Y) -
                Theme.TotalBorderWidth;

            header_interaction_alloc = header_rendering_alloc;
            header_interaction_alloc.X = list_rendering_alloc.X;
            header_interaction_alloc.Width = list_rendering_alloc.Width;
            header_interaction_alloc.Height += Theme.BorderWidth;
            header_interaction_alloc.Offset (-allocation.X, -allocation.Y);

            list_interaction_alloc = list_rendering_alloc;
            list_interaction_alloc.Offset (-allocation.X, -allocation.Y);

            header_width = header_interaction_alloc.Width;
        }

        protected override void OnSizeRequested (ref Requisition requisition)
        {
            // TODO give the minimum height of the header
            if (Theme == null) {
                return;
            }
            requisition.Width = Theme.TotalBorderWidth * 2;
            requisition.Height = HeaderHeight + Theme.TotalBorderWidth * 2;
        }

        protected override void OnSizeAllocated (Rectangle allocation)
        {
            base.OnSizeAllocated (allocation);

            if (IsRealized) {
                event_window.MoveResize (allocation);
            }

            MoveResize (allocation);
            RecalculateColumnSizes ();
            RegenerateColumnCache ();

            if (ViewLayout != null) {
                ViewLayout.Allocate ((Hyena.Gui.Canvas.Rect)list_rendering_alloc);
            }

            if (vadjustment != null) {
                hadjustment.PageSize = header_interaction_alloc.Width;
                hadjustment.PageIncrement = header_interaction_alloc.Width;
                vadjustment.PageSize = list_rendering_alloc.Height;
                vadjustment.PageIncrement = list_rendering_alloc.Height;
                UpdateAdjustments ();
            }

            ICareAboutView model = Model as ICareAboutView;
            if (model != null) {
                model.RowsInView = RowsInView;
            }

            OnInvalidateMeasure ();
            InvalidateList ();
        }

        protected int ItemsInView {
            get {
                // FIXME hardcoded grid
                int columns = ViewLayout == null ? 1 : (ViewLayout as DataViewLayoutGrid).Columns;
                return RowsInView * columns;
            }
        }

        // FIXME: obsolete
        protected int RowsInView {
            get {
                if (ChildSize.Height <= 0) {
                    return 0;
                }

                return (int)Math.Ceiling ((list_rendering_alloc.Height +
                    ChildSize.Height) / (double)ChildSize.Height);
            }
        }

        private DataViewLayout view_layout;
        protected DataViewLayout ViewLayout {
            get { return view_layout; }
            set {
                view_layout = value;
                QueueResize ();
            }
        }
    }
}
