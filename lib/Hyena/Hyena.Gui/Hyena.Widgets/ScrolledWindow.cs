//
// ScrolledWindow.cs
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
using System.Reflection;

using Gtk;
using Gdk;
using Cairo;

using Hyena.Gui;
using Hyena.Data.Gui;

namespace Hyena.Widgets
{
    public class ScrolledWindow : Gtk.ScrolledWindow
    {
        private Widget adjustable;
        private RoundedFrame rounded_frame;

        public ScrolledWindow ()
        {
        }

        public void AddWithFrame (Widget widget)
        {
            RoundedFrame frame = new RoundedFrame ();
            frame.Add (widget);
            frame.Show ();

            Add (frame);
            ProbeAdjustable (widget);
        }

        protected override void OnAdded (Widget widget)
        {
            if (widget is RoundedFrame) {
                rounded_frame = (RoundedFrame)widget;
                rounded_frame.Added += OnFrameWidgetAdded;
                rounded_frame.Removed += OnFrameWidgetRemoved;
            }

            base.OnAdded (widget);
        }

        protected override void OnRemoved (Widget widget)
        {
            if (widget == rounded_frame) {
                rounded_frame.Added -= OnFrameWidgetAdded;
                rounded_frame.Removed -= OnFrameWidgetRemoved;
                rounded_frame = null;
            }

            base.OnRemoved (widget);
        }

        private void OnFrameWidgetAdded (object o, AddedArgs args)
        {
            if (rounded_frame != null) {
                ProbeAdjustable (args.Widget);
            }
        }

        private void OnFrameWidgetRemoved (object o, RemovedArgs args)
        {
            if (adjustable != null && adjustable == args.Widget) {
                Hadjustment = null;
                Vadjustment = null;
                adjustable = null;
            }
        }

        private void ProbeAdjustable (Widget widget)
        {
            Type type = widget.GetType ();

            PropertyInfo hadj_prop = type.GetProperty ("Hadjustment");
            PropertyInfo vadj_prop = type.GetProperty ("Vadjustment");

            if (hadj_prop == null || vadj_prop == null) {
                return;
            }

            object hadj_value = hadj_prop.GetValue (widget, null);
            object vadj_value = vadj_prop.GetValue (widget, null);

            if (hadj_value == null || vadj_value == null
                || hadj_value.GetType () != typeof (Adjustment)
                || vadj_value.GetType () != typeof (Adjustment)) {
                return;
            }

            Hadjustment = (Adjustment)hadj_value;
            Vadjustment = (Adjustment)vadj_value;
        }
    }
}
