//
// RatingMenuItem.cs
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
using Gtk;
using Mono.Unix;

using Hyena.Gui;

namespace Hyena.Widgets
{
    public class RatingMenuItem : ComplexMenuItem
    {
        private RatingEntry entry;
        private bool can_activate = true;
        private Box box;

        protected RatingMenuItem (RatingEntry entry) : base ()
        {
            box = new HBox ();
            box.Spacing = 5;

            Label label = new Label ();
            label.Markup = String.Format ("<i>{0}</i>",
                GLib.Markup.EscapeText (Catalog.GetString ("Rating:")));
            box.PackStart (label, false, false, 0);
            label.Show ();

            this.entry = entry;
            entry.HasFrame = false;
            entry.PreviewOnHover = true;
            entry.AlwaysShowEmptyStars = true;
            entry.Changed += OnEntryChanged;
            box.PackStart (entry, false, false, 0);

            box.ShowAll ();
            Add (box);
        }

        public RatingMenuItem () : this (new RatingEntry ())
        {
        }

        protected RatingMenuItem (IntPtr raw) : base (raw)
        {
        }

        private int TransformX (double inx)
        {
            int x = (int)inx - entry.Allocation.X;

            if (x < 0) {
                x = 0;
            } else if (x > entry.Allocation.Width) {
                x = entry.Allocation.Width;
            }

            return x;
        }

        protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
        {
            if (evnt.X == 0 && evnt.Y == 0) {
                return false;
            }
            entry.SetValueFromPosition (TransformX (evnt.X));
            return true;
        }

        protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
        {
            return entry.HandleMotionNotify (evnt.State, TransformX (evnt.X));
        }

        protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
        {
            return entry.HandleLeaveNotify (evnt);
        }

        protected override bool OnScrollEvent (Gdk.EventScroll evnt)
        {
            return entry.HandleScroll (evnt);
        }

        protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
        {
            return entry.HandleKeyPress (evnt);
        }

        private void OnEntryChanged (object o, EventArgs args)
        {
            if (can_activate) {
                Activate ();
            }
        }

        public void Reset (int value)
        {
            can_activate = false;
            Value = value;
            entry.ClearHover ();
            can_activate = true;
        }

        public int Value {
            get { return entry.Value; }
            set { entry.Value = value; }
        }

        public RatingEntry RatingEntry {
            get { return entry; }
        }
    }
}
