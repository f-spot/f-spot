//
// ColumnCellRating.cs
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

using Hyena.Gui;
using Hyena.Gui.Canvas;
using Hyena.Gui.Theming;

namespace Hyena.Data.Gui
{
    public class ColumnCellRating : ColumnCell, IInteractiveCell, ISizeRequestCell
    {
        private object last_pressed_bound;
        private object hover_bound;
        private int hover_value;
        private Gdk.Rectangle actual_area_hack;
        private RatingRenderer renderer = new RatingRenderer ();

        public ColumnCellRating (string property, bool expand) : base (property, expand)
        {
            Xpad = 0;
        }

        public override Size Measure (Size available)
        {
            Width = renderer.Width;
            Height = renderer.Height;
            return DesiredSize = new Size (Width + Margin.X, Height + Margin.Y);
        }

        public override void Render (CellContext context, double cellWidth, double cellHeight)
        {
            Gdk.Rectangle area = new Gdk.Rectangle (0, 0, (int)cellWidth, (int)cellHeight);

            // FIXME: Compute font height and set to renderer.Size

            renderer.Value = Value;
            bool is_hovering = hover_bound == BoundObjectParent && hover_bound != null;
            renderer.Render (context.Context, area, context.Theme.Colors.GetWidgetColor (GtkColorClass.Text, context.State),
                is_hovering, is_hovering, hover_value, 0.8, 0.45, 0.35);

            // FIXME: Something is hosed in the view when computing cell dimensions
            // The cell width request is always smaller than the actual cell, so
            // this value is preserved once we compute it from rendering so the
            // input stuff can do its necessary calculations
            actual_area_hack = area;
        }

        public override bool ButtonEvent (Point press, bool pressed, uint button)
        {
            if (ReadOnly) {
                return false;
            }

            if (pressed) {
                last_pressed_bound = BoundObjectParent;
                return false;
            }

            if (last_pressed_bound == BoundObjectParent && last_pressed_bound != null) {
                Value = RatingFromPosition (press.X);
                InvalidateRender ();
                last_pressed_bound = null;
            }

            return true;
        }

        public override bool CursorMotionEvent (Point motion)
        {
            if (ReadOnly) {
                return false;
            }

            int value = RatingFromPosition (motion.X);
            if (hover_bound == BoundObjectParent && value == hover_value) {
                return false;
            }

            hover_bound = BoundObjectParent;
            hover_value = value;
            InvalidateRender ();
            return true;
        }

        public override bool CursorLeaveEvent ()
        {
            base.CursorLeaveEvent ();
            hover_bound = null;
            hover_value = MinRating - 1;
            InvalidateRender ();
            return true;
        }

        public void GetWidthRange (Pango.Layout layout, out int min, out int max)
        {
            min = max = renderer.Width;
        }

        private int RatingFromPosition (double x)
        {
            return renderer.RatingFromPosition (actual_area_hack, x);
        }

        private bool restrict_size = true;
        public bool RestrictSize {
            get { return restrict_size; }
            set { restrict_size = value; }
        }

        private int Value {
            get { return BoundObject == null ? MinRating : renderer.ClampValue ((int)BoundObject); }
            set { BoundObject = renderer.ClampValue (value); }
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

        public int Xpad {
            get { return renderer.Xpad; }
            set { renderer.Xpad = value; }
        }

        public int Ypad {
            get { return renderer.Ypad; }
            set { renderer.Ypad = value; }
        }

        public bool ReadOnly { get; set; }
    }
}
