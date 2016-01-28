//
// RatingEntry.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gabriel.burt@gmail.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
// Copyright (C) 2006 Gabriel Burt
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
// NONINFRINGEMENT. IN NO  SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using Gtk;
using System;

using Hyena.Gui;

namespace Hyena.Widgets
{
    public class RatingEntry : Widget
    {
        private RatingRenderer renderer;
        private Gdk.Rectangle event_alloc;
        private int hover_value = -1;
        private bool interior_focus;
        private int focus_width;
        private Gdk.Window event_window;

        public event EventHandler Changing;
        public event EventHandler Changed;

        static RatingEntry ()
        {
            RatingAccessibleFactory.Init ();
        }

        public RatingEntry () : this (0)
        {
            WidgetFlags |= Gtk.WidgetFlags.NoWindow;
        }

        public RatingEntry (int rating) : this (rating, new RatingRenderer ())
        {
        }

        protected RatingEntry (int rating, RatingRenderer renderer)
        {
            this.renderer = renderer;
            this.renderer.Value = rating;
            CanFocus = true;
            Name = "GtkEntry";
        }

        protected RatingEntry (IntPtr raw) : base (raw)
        {
        }

        protected virtual void OnChanging ()
        {
            EventHandler handler = Changing;
            if (handler != null) {
                handler (this, new EventArgs ());
            }
        }

        protected virtual void OnChanged ()
        {
            QueueDraw ();

            EventHandler handler = Changed;
            if (handler != null) {
                handler (this, new EventArgs ());
            }
        }

        internal void SetValueFromPosition (int x)
        {
            Value = renderer.RatingFromPosition (event_alloc, x);
        }

#region Public Properties

        private bool always_show_empty_stars = false;
        public bool AlwaysShowEmptyStars {
            get { return always_show_empty_stars; }
            set { always_show_empty_stars = value; }
        }

        private bool preview_on_hover = true;
        public bool PreviewOnHover {
            get { return preview_on_hover; }
            set { preview_on_hover = value; }
        }

        private bool has_frame = true;
        public bool HasFrame {
            get { return has_frame; }
            set { has_frame = value; QueueResize (); }
        }

        public int Value {
            get { return renderer.Value; }
            set {
                if (renderer.Value != value && renderer.Value >= renderer.MinRating && value <= renderer.MaxRating) {
                    renderer.Value = value;
                    OnChanging ();
                    OnChanged ();
                }
            }
        }

        public int MaxRating {
            get { return renderer.MaxRating; }
            set { renderer.MaxRating = value; }
        }

        public int MinRating {
            get { return renderer.MinRating; }
            set { renderer.MinRating = value; }
        }

        public int RatingLevels {
            get { return renderer.RatingLevels; }
        }

        private object rated_object;
        public object RatedObject {
            get { return rated_object; }
            set { rated_object = value; }
        }

#endregion

#region Protected Gtk.Widget Overrides

        protected override void OnRealized ()
        {
            WidgetFlags |= WidgetFlags.Realized | WidgetFlags.NoWindow;
            GdkWindow = Parent.GdkWindow;

            Gdk.WindowAttr attributes = new Gdk.WindowAttr ();
            attributes.WindowType = Gdk.WindowType.Child;
            attributes.X = Allocation.X;
            attributes.Y = Allocation.Y;
            attributes.Width = Allocation.Width;
            attributes.Height = Allocation.Height;
            attributes.Wclass = Gdk.WindowClass.InputOnly;
            attributes.EventMask = (int)(
                Gdk.EventMask.PointerMotionMask |
                Gdk.EventMask.EnterNotifyMask |
                Gdk.EventMask.LeaveNotifyMask |
                Gdk.EventMask.KeyPressMask |
                Gdk.EventMask.KeyReleaseMask |
                Gdk.EventMask.ButtonPressMask |
                Gdk.EventMask.ButtonReleaseMask |
                Gdk.EventMask.ExposureMask);

            Gdk.WindowAttributesType attributes_mask =
                Gdk.WindowAttributesType.X |
                Gdk.WindowAttributesType.Y |
                Gdk.WindowAttributesType.Wmclass;

            event_window = new Gdk.Window (GdkWindow, attributes, attributes_mask);
            event_window.UserData = Handle;

            Style = Gtk.Rc.GetStyleByPaths (Settings, "*.GtkEntry", "*.GtkEntry", GType);

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

        private bool changing_style;
        protected override void OnStyleSet (Style previous_style)
        {
            if (changing_style) {
                return;
            }

            base.OnStyleSet (previous_style);

            changing_style = true;
            focus_width = (int)StyleGetProperty ("focus-line-width");
            interior_focus = (bool)StyleGetProperty ("interior-focus");
            changing_style = false;
        }

        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated (allocation);
            event_alloc = new Gdk.Rectangle (0, 0, allocation.Width, allocation.Height);
            if (IsRealized) {
                event_window.MoveResize (allocation);
            }
        }

        protected override void OnSizeRequested (ref Gtk.Requisition requisition)
        {
            EnsureStyle ();

            Pango.FontMetrics metrics = PangoContext.GetMetrics (Style.FontDescription, PangoContext.Language);
            renderer.Size = ((int)(metrics.Ascent + metrics.Descent) + 512) >> 10; // PANGO_PIXELS(d)
            metrics.Dispose ();

            if (HasFrame) {
                renderer.Xpad = Style.Xthickness + (interior_focus ? focus_width : 0) + 2;
                renderer.Ypad = Style.Ythickness + (interior_focus ? focus_width : 0) + 2;
            } else {
                renderer.Xpad = 0;
                renderer.Ypad = 0;
            }

            requisition.Width = renderer.Width;
            requisition.Height = renderer.Height;
        }

        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {
            if (evnt.Window != GdkWindow) {
                return true;
            }

            if (HasFrame) {
                int y_mid = (int)Math.Round ((Allocation.Height - renderer.Height) / 2.0);
                Gtk.Style.PaintFlatBox (Style, GdkWindow, State, ShadowType.None, evnt.Area, this, "entry",
                    Allocation.X, Allocation.Y + y_mid, Allocation.Width, renderer.Height);
                Gtk.Style.PaintShadow (Style, GdkWindow, State, ShadowType.In,
                    evnt.Area, this, "entry", Allocation.X, Allocation.Y + y_mid, Allocation.Width, renderer.Height);
            }

            Cairo.Context cr = Gdk.CairoHelper.Create (GdkWindow);
            renderer.Render (cr, Allocation,
                CairoExtensions.GdkColorToCairoColor (HasFrame ? Parent.Style.Text (State) : Parent.Style.Foreground (State)),
                AlwaysShowEmptyStars, PreviewOnHover && hover_value >= renderer.MinRating, hover_value,
                State == StateType.Insensitive ? 1 : 0.90,
                State == StateType.Insensitive ? 1 : 0.55,
                State == StateType.Insensitive ? 1 : 0.45);
            CairoExtensions.DisposeContext (cr);

            return true;
        }

        protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
        {
            if (evnt.Button != 1) {
                return false;
            }

            HasFocus = true;
            Value = renderer.RatingFromPosition (event_alloc, evnt.X);

            return true;
        }

        protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
        {
            hover_value = renderer.MinRating;
            QueueDraw ();
            return true;
        }

        protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing crossing)
        {
            return HandleLeaveNotify (crossing);
        }

        protected override bool OnMotionNotifyEvent (Gdk.EventMotion motion)
        {
            return HandleMotionNotify (motion.State, motion.X);
        }

        protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
        {
            switch (evnt.Key) {
                case Gdk.Key.Up:
                case Gdk.Key.Right:
                case Gdk.Key.plus:
                case Gdk.Key.equal:
                    Value++;
                    return true;

                case Gdk.Key.Down:
                case Gdk.Key.Left:
                case Gdk.Key.minus:
                    Value--;
                    return true;
            }

            if (evnt.KeyValue >= (48 + MinRating) && evnt.KeyValue <= (48 + MaxRating) && evnt.KeyValue <= 59) {
                Value = (int)evnt.KeyValue - 48;
                return true;
            }

            return false;
        }

        protected override bool OnScrollEvent (Gdk.EventScroll args)
        {
            return HandleScroll (args);
        }

#endregion

#region Internal API, primarily for RatingMenuItem

        internal void ClearHover ()
        {
            hover_value = renderer.MinRating - 1;
        }

        internal bool HandleKeyPress (Gdk.EventKey evnt)
        {
            return this.OnKeyPressEvent (evnt);
        }

        internal bool HandleScroll (Gdk.EventScroll args)
        {
            switch (args.Direction) {
                case Gdk.ScrollDirection.Up:
                case Gdk.ScrollDirection.Right:
                    Value++;
                    return true;

                case Gdk.ScrollDirection.Down:
                case Gdk.ScrollDirection.Left:
                    Value--;
                    return true;
            }

            return false;
        }

        internal bool HandleMotionNotify (Gdk.ModifierType state, double x)
        {
            hover_value = renderer.RatingFromPosition (event_alloc, x);
            /*if ((state & Gdk.ModifierType.Button1Mask) != 0) {
                Value = hover_value;
            }*/

            QueueDraw ();
            return true;
        }

        internal bool HandleLeaveNotify (Gdk.EventCrossing crossing)
        {
            ClearHover ();
            QueueDraw ();
            return true;
        }

#endregion

    }

#region Test Module

    public class RatingAccessible : Atk.Object, Atk.Value, Atk.ValueImplementor
    {
        private RatingEntry rating;

        public RatingAccessible (IntPtr raw) : base (raw)
        {
            Hyena.Log.Information ("RatingAccessible raw ctor..");
        }

        public RatingAccessible (GLib.Object widget): base ()
        {
            rating = widget as RatingEntry;
            Name = "Rating entry";
            Description = "Rating entry, from 0 to 5 stars";
            Role = Atk.Role.Slider;
        }

        public void GetMaximumValue (ref GLib.Value val)
        {
            val = new GLib.Value (5);
        }

        public void GetMinimumIncrement (ref GLib.Value val)
        {
            val = new GLib.Value (1);
        }

        public void GetMinimumValue (ref GLib.Value val)
        {
            val = new GLib.Value (0);
        }

        public void GetCurrentValue (ref GLib.Value val)
        {
            val = new GLib.Value (rating.Value);
        }

        public bool SetCurrentValue (GLib.Value val)
        {
            int r = (int) val.Val;
            if (r <= 0 || r > 5) {
                return false;
            }

            rating.Value = (int) val.Val;
            return true;
        }
    }

    internal class RatingAccessibleFactory : Atk.ObjectFactory
    {
        public static void Init ()
        {
            new RatingAccessibleFactory ();
            Atk.Global.DefaultRegistry.SetFactoryType ((GLib.GType)typeof (RatingEntry), (GLib.GType)typeof (RatingAccessibleFactory));
        }

        protected override Atk.Object OnCreateAccessible (GLib.Object obj)
        {
            return new RatingAccessible (obj);
        }

        protected override GLib.GType OnGetAccessibleType ()
        {
            return RatingAccessible.GType;
        }
    }

    [Hyena.Gui.TestModule ("Rating Entry")]
    internal class RatingEntryTestModule : Gtk.Window
    {
        public RatingEntryTestModule () : base ("Rating Entry")
        {
            VBox pbox = new VBox ();
            Add (pbox);

            Menu m = new Menu ();
            MenuBar b = new MenuBar ();
            MenuItem item = new MenuItem ("Rate Me!");
            item.Submenu = m;
            b.Append (item);
            m.Append (new MenuItem ("Apples"));
            m.Append (new MenuItem ("Pears"));
            m.Append (new RatingMenuItem ());
            m.Append (new ImageMenuItem ("gtk-remove", null));
            m.ShowAll ();
            pbox.PackStart (b, false, false, 0);

            VBox box = new VBox ();
            box.BorderWidth = 10;
            box.Spacing = 10;
            pbox.PackStart (box, true, true, 0);

            RatingEntry entry1 = new RatingEntry ();
            box.PackStart (entry1, true, true, 0);

            RatingEntry entry2 = new RatingEntry ();
            box.PackStart (entry2, false, false, 0);

            box.PackStart (new Entry ("Normal GtkEntry"), false, false, 0);

            RatingEntry entry3 = new RatingEntry ();
            Pango.FontDescription fd = entry3.PangoContext.FontDescription.Copy ();
            fd.Size = (int)(fd.Size * Pango.Scale.XXLarge);
            entry3.ModifyFont (fd);
            fd.Dispose ();
            box.PackStart (entry3, true, true, 0);

            pbox.ShowAll ();
        }
    }

#endregion

}
