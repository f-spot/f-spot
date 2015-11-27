//
// RatingRenderer.cs
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
using Cairo;

namespace Hyena.Gui
{
    public class RatingRenderer
    {
        private static double [,] star_plot = new double[,] {
            { 0, 0.425 },
            { 0.375, 0.375 },
            { 0.5, 0.05 },
            { 0.625, 0.375 },
            { 1, 0.425 },
            { 0.75, 0.625 },
            { 0.8, 0.95 },
            { 0.5, 0.75 },
            { 0.2, 0.95 },
            { 0.25, 0.625 },
            { 0, 0.425 },
        };

        public RatingRenderer ()
        {
        }

        public virtual void Render (Context cr, Gdk.Rectangle area, Color color, bool showEmptyStars, bool isHovering,
            int hoverValue, double fillOpacity, double hoverFillOpacity, double strokeOpacity)
        {
            if (Value == MinRating && !isHovering && !showEmptyStars) {
                return;
            }

            cr.Save ();

            Cairo.Color fill_color = color;
            fill_color.A = fillOpacity;
            Cairo.Color stroke_color = fill_color;
            stroke_color.A = strokeOpacity;
            Cairo.Color hover_fill_color = fill_color;
            hover_fill_color.A = hoverFillOpacity;

            double x, y;
            ComputePosition (area, out x, out y);

            cr.LineWidth = 1.0;
            cr.Translate (0.5, 0.5);

            for (int i = MinRating + 1, s = isHovering || showEmptyStars ? MaxRating : Value; i <= s; i++, x += Size) {
                bool fill = i <= Value && Value > MinRating;
                bool hover_fill = i <= hoverValue && hoverValue > MinRating;
                double scale = fill || hover_fill ? Size : Size - 2;
                double ofs = fill || hover_fill ? 0 : 1;

                for (int p = 0, n = star_plot.GetLength (0); p < n; p++) {
                    double px = x + ofs + star_plot[p, 0] * scale;
                    double py = y + ofs + star_plot[p, 1] * scale;
                    if (p == 0) {
                        cr.MoveTo (px, py);
                    } else {
                        cr.LineTo (px, py);
                    }
                }
                cr.ClosePath ();

                if (fill || hover_fill) {
                    if (!isHovering || hoverValue >= Value) {
                        cr.Color = fill ? fill_color : hover_fill_color;
                    } else {
                        cr.Color = hover_fill ? fill_color : hover_fill_color;
                    }
                    cr.Fill ();
                } else {
                    cr.Color = stroke_color;
                    cr.Stroke ();
                }
            }
            cr.Restore ();
        }

        protected void ComputePosition (Gdk.Rectangle area, out double x, out double y)
        {
            double cell_width = area.Width - 2 * Xpad;
            double cell_height = area.Height - 2 * Ypad;

            double stars_width = MaxRating * Size;
            double stars_height = Size;

            x = area.X + Xpad + (cell_width - stars_width) / 2.0;
            y = area.Y + Ypad + (cell_height - stars_height) / 2.0;
        }

        public int RatingFromPosition (Gdk.Rectangle area, double x)
        {
            double r_x, r_y;
            ComputePosition (area, out r_x, out r_y);
            return x <= r_x ? 0 : Clamp (MinRating, MaxRating, (int)Math.Ceiling ((x - r_x) / Size) + MinRating);
        }

        private static int Clamp (int min, int max, int value)
        {
            return Math.Max (min, Math.Min (max, value));
        }

        public int ClampValue (int value)
        {
            return Clamp (MinRating, MaxRating, value);
        }

        private int value;
        public int Value {
            get { return ClampValue (this.value); }
            set { this.value = ClampValue (value); }
        }

        private int size = 14;
        public int Size {
            get { return size; }
            set { size = value; }
        }

        private int min_rating = 0;
        public int MinRating {
            get { return min_rating; }
            set { min_rating = value; }
        }

        private int max_rating = 5;
        public int MaxRating {
            get { return max_rating; }
            set { max_rating = value; }
        }

        public int RatingLevels {
            get { return MaxRating - MinRating + 1; }
        }

        private int xpad = 2;
        public int Xpad {
            get { return xpad; }
            set { xpad = value; }
        }

        public int ypad = 2;
        public int Ypad {
            get { return ypad; }
            set { ypad = value; }
        }

        public int Width {
            get { return Xpad * 2 + RatingLevels * Size; }
        }

        public int Height {
            get { return Ypad * 2 + Size; }
        }
    }
}
