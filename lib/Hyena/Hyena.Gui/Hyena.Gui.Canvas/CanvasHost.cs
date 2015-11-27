//
// CanvasHost.cs
//
// Author:
//       Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Aaron Bockover
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Gtk;
using Gdk;

using Hyena.Gui.Theming;

namespace Hyena.Gui.Canvas
{
    public class CanvasHost : Widget, ICanvasHost
    {
        private Gdk.Window event_window;
        private CanvasItem canvas_child;
        private Theme theme;
        private CanvasManager manager;
        private bool debug = false;
        private FpsCalculator fps = new FpsCalculator ();
        private Hyena.Data.Gui.CellContext context = new Hyena.Data.Gui.CellContext ();

        public CanvasHost ()
        {
            WidgetFlags |= WidgetFlags.NoWindow;
            manager = new CanvasManager (this);
        }

        protected CanvasHost (IntPtr native) : base (native)
        {
        }

        protected override void OnRealized ()
        {
            base.OnRealized ();

            WindowAttr attributes = new WindowAttr ();
            attributes.WindowType = Gdk.WindowType.Child;
            attributes.X = Allocation.X;
            attributes.Y = Allocation.Y;
            attributes.Width = Allocation.Width;
            attributes.Height = Allocation.Height;
            attributes.Wclass = WindowClass.InputOnly;
            attributes.EventMask = (int)(
                EventMask.PointerMotionMask |
                EventMask.ButtonPressMask |
                EventMask.ButtonReleaseMask |
                EventMask.EnterNotifyMask |
                EventMask.LeaveNotifyMask |
                EventMask.ExposureMask);

            WindowAttributesType attributes_mask =
                WindowAttributesType.X | WindowAttributesType.Y | WindowAttributesType.Wmclass;

            event_window = new Gdk.Window (GdkWindow, attributes, attributes_mask);
            event_window.UserData = Handle;

            AllocateChild ();
            QueueResize ();
        }

        protected override void OnUnrealized ()
        {
            WidgetFlags ^= WidgetFlags.Realized;

            event_window.UserData = IntPtr.Zero;
            Hyena.Gui.GtkWorkarounds.WindowDestroy (event_window);
            event_window = null;

            base.OnUnrealized ();
        }

        protected override void OnMapped ()
        {
            event_window.Show ();
            base.OnMapped ();
        }

        protected override void OnUnmapped ()
        {
            event_window.Hide ();
            base.OnUnmapped ();
        }

        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated (allocation);

            if (IsRealized) {
                event_window.MoveResize (allocation);
                AllocateChild ();
            }
        }

        protected override void OnSizeRequested (ref Gtk.Requisition requisition)
        {
            if (canvas_child != null) {
                Size size = canvas_child.Measure (Size.Empty);

                if (size.Width > 0) {
                    requisition.Width = (int)Math.Ceiling (size.Width);
                }

                if (size.Height > 0) {
                    requisition.Height = (int)Math.Ceiling (size.Height);
                }
            }
        }

        private Random rand;

        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {
            if (canvas_child == null || !canvas_child.Visible || !Visible || !IsMapped) {
                return true;
            }

            Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window);
            context.Context = cr;

            foreach (Gdk.Rectangle damage in evnt.Region.GetRectangles ()) {
                cr.Rectangle (damage.X, damage.Y, damage.Width, damage.Height);
                cr.Clip ();

                cr.Translate (Allocation.X, Allocation.Y);
                canvas_child.Render (context);
                cr.Translate (-Allocation.X, -Allocation.Y);

                if (Debug) {
                    cr.LineWidth = 1.0;
                    cr.Color = CairoExtensions.RgbToColor (
                        (uint)(rand = rand ?? new Random ()).Next (0, 0xffffff));
                    cr.Rectangle (damage.X + 0.5, damage.Y + 0.5, damage.Width - 1, damage.Height - 1);
                    cr.Stroke ();
                }

                cr.ResetClip ();
            }

            CairoExtensions.DisposeContext (cr);

            if (fps.Update ()) {
                // Console.WriteLine ("FPS: {0}", fps.FramesPerSecond);
            }

            return true;
        }

        private void AllocateChild ()
        {
            if (canvas_child != null) {
                canvas_child.Allocation = new Rect (0, 0, Allocation.Width, Allocation.Height);
                canvas_child.Measure (new Size (Allocation.Width, Allocation.Height));
                canvas_child.Arrange ();
            }
        }

        public void QueueRender (CanvasItem item, Rect rect)
        {
            double x = Allocation.X;
            double y = Allocation.Y;
            double w, h;

            if (rect.IsEmpty) {
                w = item.Allocation.Width;
                h = item.Allocation.Height;
            } else {
                x += rect.X;
                y += rect.Y;
                w = rect.Width;
                h = rect.Height;
            }

            while (item != null) {
                x += item.ContentAllocation.X;
                y += item.ContentAllocation.Y;
                item = item.Parent;
            }

            QueueDrawArea (
                (int)Math.Floor (x),
                (int)Math.Floor (y),
                (int)Math.Ceiling (w),
                (int)Math.Ceiling (h)
            );
        }

        private bool changing_style = false;

        protected override void OnStyleSet (Style old_style)
        {
            if (changing_style) {
                return;
            }

            changing_style = true;

            theme = new GtkTheme (this);
            context.Theme = theme;
            if (canvas_child != null) {
                canvas_child.Theme = theme;
            }

            changing_style = false;

            base.OnStyleSet (old_style);
        }

        protected override bool OnButtonPressEvent (Gdk.EventButton press)
        {
            if (canvas_child != null) {
                canvas_child.ButtonEvent (new Point (press.X, press.Y), true, press.Button);
            }
            return true;
        }

        protected override bool OnButtonReleaseEvent (Gdk.EventButton press)
        {
            if (canvas_child != null) {
                canvas_child.ButtonEvent (new Point (press.X, press.Y), false, press.Button);
            }
            return true;
        }

        protected override bool OnMotionNotifyEvent (EventMotion evnt)
        {
            if (canvas_child != null) {
                canvas_child.CursorMotionEvent (new Point (evnt.X, evnt.Y));
            }
            return true;
        }

        public void Add (CanvasItem child)
        {
            if (Child != null) {
                throw new InvalidOperationException ("Child is already set, remove it first");
            }

            Child = child;
        }

        public void Remove (CanvasItem child)
        {
            if (Child != child) {
                throw new InvalidOperationException ("child does not already belong to host");
            }

            Child = null;
        }

        private void OnCanvasChildLayoutUpdated (object o, EventArgs args)
        {
            QueueDraw ();
        }

        private void OnCanvasChildSizeChanged (object o, EventArgs args)
        {
            QueueResize ();
        }

        public CanvasItem Child {
            get { return canvas_child; }
            set {
                if (canvas_child == value) {
                    return;
                } else if (canvas_child != null) {
                    canvas_child.Theme = null;
                    canvas_child.Manager = null;
                    canvas_child.LayoutUpdated -= OnCanvasChildLayoutUpdated;
                    canvas_child.SizeChanged -= OnCanvasChildSizeChanged;
                }

                canvas_child = value;

                if (canvas_child != null) {
                    canvas_child.Theme = theme;
                    canvas_child.Manager = manager;
                    canvas_child.LayoutUpdated += OnCanvasChildLayoutUpdated;
                    canvas_child.SizeChanged += OnCanvasChildSizeChanged;
                }

                AllocateChild ();
            }
        }

        Pango.Layout layout;
        public Pango.Layout PangoLayout {
            get {
                if (layout == null) {
                    if (GdkWindow == null || !IsRealized) {
                        return null;
                    }

                    using (var cr = Gdk.CairoHelper.Create (GdkWindow)) {
                        layout = CairoExtensions.CreateLayout (this, cr);
                        FontDescription = layout.FontDescription;
                    }
                }

                return layout;
            }
        }

        public Pango.FontDescription FontDescription { get; private set; }

        public bool Debug {
            get { return debug; }
            set { debug = value; }
        }
    }
}
