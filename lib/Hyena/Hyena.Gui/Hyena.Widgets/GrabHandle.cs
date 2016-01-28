//
// GrabHandle.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2010 Novell, Inc.
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

namespace Hyena.Widgets
{
    public class GrabHandle : EventBox
    {
        Gtk.DrawingArea da;

        public GrabHandle () : this (5, 28) {}

        public GrabHandle (int w, int h)
        {
            da = new DrawingArea ();
            da.SetSizeRequest (w, h);
            Orientation = Gtk.Orientation.Vertical;

            Child = da;
            ShowAll ();

            ButtonPressEvent += (o, a) => Dragging = true;
            ButtonReleaseEvent += (o, a) => Dragging = false;
            EnterNotifyEvent += (o, a) => Inside = true;
            LeaveNotifyEvent += (o, a) => Inside = false;

            da.ExposeEvent += (o, a) => {
                if (da.IsDrawable) {
                    Gtk.Style.PaintHandle (da.Style, da.GdkWindow, da.State, ShadowType.In,
                        a.Event.Area, this, "entry", 0, 0, da.Allocation.Width, da.Allocation.Height, Orientation);
                }
            };
        }

        public void ControlWidthOf (Widget widget, int min, int max, bool grabberOnRight)
        {
            MotionNotifyEvent += (o, a) => {
                var x = a.Event.X;
                var w = Math.Min (max, Math.Max (min, widget.WidthRequest + (grabberOnRight ? 1 : -1 ) * x));
                widget.WidthRequest = (int)w;
            };
        }

        public Gtk.Orientation Orientation { get; set; }

        private bool inside, dragging;
        private bool Inside {
            set {
                inside = value;
                GdkWindow.Cursor = dragging || inside ? resize_cursor : null;
            }
        }

        private bool Dragging {
            set {
                dragging = value;
                GdkWindow.Cursor = dragging || inside ? resize_cursor : null;
            }
        }

        private static Gdk.Cursor resize_cursor = new Gdk.Cursor (Gdk.CursorType.SbHDoubleArrow);
    }
}
