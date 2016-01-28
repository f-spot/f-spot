//
// BaseWidgetAccessible.cs
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
using System.Linq;
using System.Collections.Generic;

using Atk;

namespace Hyena.Gui
{
    public class BaseWidgetAccessible : Gtk.Accessible, Atk.ComponentImplementor
    {
        private Gtk.Widget widget;
        private uint focus_id = 0;
        private Dictionary<uint, Atk.FocusHandler> focus_handlers = new Dictionary<uint, Atk.FocusHandler> ();

        public BaseWidgetAccessible (Gtk.Widget widget)
        {
            this.widget = widget;
            widget.SizeAllocated += OnAllocated;
            widget.Mapped += OnMap;
            widget.Unmapped += OnMap;
            widget.FocusInEvent += OnFocus;
            widget.FocusOutEvent += OnFocus;
            widget.AddNotification ("sensitive", (o, a) => NotifyStateChange (StateType.Sensitive, widget.Sensitive));
            widget.AddNotification ("visible",   (o, a) => NotifyStateChange (StateType.Visible, widget.Visible));
        }

        protected BaseWidgetAccessible (IntPtr raw) : base (raw)
        {
        }

        public virtual new Atk.Layer Layer {
            get { return Layer.Widget; }
        }

        protected override Atk.StateSet OnRefStateSet ()
        {
            var s = base.OnRefStateSet ();

            AddStateIf (s, widget.CanFocus,   StateType.Focusable);
            AddStateIf (s, widget.HasFocus,   StateType.Focused);
            AddStateIf (s, widget.Sensitive,  StateType.Sensitive);
            AddStateIf (s, widget.Sensitive,  StateType.Enabled);
            AddStateIf (s, widget.HasDefault, StateType.Default);
            AddStateIf (s, widget.Visible,    StateType.Visible);
            AddStateIf (s, widget.Visible && widget.IsMapped, StateType.Showing);

            return s;
        }

        private static void AddStateIf (StateSet s, bool condition, StateType t)
        {
            if (condition) {
                s.AddState (t);
            }
        }

        private void OnFocus (object o, EventArgs args)
        {
            NotifyStateChange (StateType.Focused, widget.HasFocus);
            var handler = FocusChanged;
            if (handler != null) {
                handler (this, widget.HasFocus);
            }
        }

        private void OnMap (object o, EventArgs args)
        {
            NotifyStateChange (StateType.Showing, widget.Visible && widget.IsMapped);
        }

        private void OnAllocated (object o, EventArgs args)
        {
            var a = widget.Allocation;
            var bounds = new Atk.Rectangle () { X = a.X, Y = a.Y, Width = a.Width, Height = a.Height };
            GLib.Signal.Emit (this, "bounds_changed", bounds);
            /*var handler = BoundsChanged;
            if (handler != null) {
                handler (this, new BoundsChangedArgs () { Args = new object [] { bounds } });
            }*/
        }

        private event FocusHandler FocusChanged;

        #region Atk.Component

        public uint AddFocusHandler (Atk.FocusHandler handler)
        {
            if (!focus_handlers.ContainsValue (handler)) {
                FocusChanged += handler;
                focus_handlers[++focus_id] = handler;
                return focus_id;
            }
            return 0;
        }

        public bool Contains (int x, int y, Atk.CoordType coordType)
        {
            int x_extents, y_extents, w, h;
            GetExtents (out x_extents, out y_extents, out w, out h, coordType);
            Gdk.Rectangle extents = new Gdk.Rectangle (x_extents, y_extents, w, h);
            return extents.Contains (x, y);
        }

        public virtual Atk.Object RefAccessibleAtPoint (int x, int y, Atk.CoordType coordType)
        {
            return new NoOpObject (widget);
        }

        public void GetExtents (out int x, out int y, out int w, out int h, Atk.CoordType coordType)
        {
            w = widget.Allocation.Width;
            h = widget.Allocation.Height;

            GetPosition (out x, out y, coordType);
        }

        public void GetPosition (out int x, out int y, Atk.CoordType coordType)
        {
            Gdk.Window window = null;

            if (!widget.IsDrawable) {
                x = y = Int32.MinValue;
                return;
            }

            if (widget.Parent != null) {
                x = widget.Allocation.X;
                y = widget.Allocation.Y;
                window = widget.ParentWindow;
            } else {
                x = 0;
                y = 0;
                window = widget.GdkWindow;
            }

            int x_window, y_window;
            window.GetOrigin (out x_window, out y_window);
            x += x_window;
            y += y_window;

            if (coordType == Atk.CoordType.Window) {
                window = widget.GdkWindow.Toplevel;
                int x_toplevel, y_toplevel;
                window.GetOrigin (out x_toplevel, out y_toplevel);

                x -= x_toplevel;
                y -= y_toplevel;
            }
        }

        public void GetSize (out int w, out int h)
        {
            w = widget.Allocation.Width;
            h = widget.Allocation.Height;
        }

        public bool GrabFocus ()
        {
            if (!widget.CanFocus) {
                return false;
            }

            widget.GrabFocus ();

            var toplevel_window = widget.Toplevel as Gtk.Window;
            if (toplevel_window != null) {
                toplevel_window.Present ();
            }

            return true;
        }

        public void RemoveFocusHandler (uint handlerId)
        {
            if (focus_handlers.ContainsKey (handlerId)) {
                FocusChanged -= focus_handlers[handlerId];
                focus_handlers.Remove (handlerId);
            }
        }

        public bool SetExtents (int x, int y, int w, int h, Atk.CoordType coordType)
        {
            return SetSizeAndPosition (x, y, w, h, coordType, true);
        }

        public bool SetPosition (int x, int y, Atk.CoordType coordType)
        {
            return SetSizeAndPosition (x, y, 0, 0, coordType, false);
        }

        private bool SetSizeAndPosition (int x, int y, int w, int h, Atk.CoordType coordType, bool setSize)
        {
            if (!widget.IsTopLevel) {
                return false;
            }

            if (coordType == CoordType.Window) {
                int x_off, y_off;
                widget.GdkWindow.GetOrigin (out x_off, out y_off);
                x += x_off;
                y += y_off;

                if (x < 0 || y < 0) {
                    return false;
                }
            }

            #pragma warning disable 0612
            widget.SetUposition (x, y);
            #pragma warning restore 0612

            if (setSize) {
                widget.SetSizeRequest (w, h);
            }

            return true;
        }

        public bool SetSize (int w, int h)
        {
            if (widget.IsTopLevel) {
                widget.SetSizeRequest (w, h);
                return true;
            } else {
                return false;
            }
        }

        public double Alpha {
            get { return 1.0; }
        }

        #endregion Atk.Component

    }
}
