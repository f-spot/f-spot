//
// ShadowMarginStyle.cs
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

using Cairo;

namespace Hyena.Gui.Canvas
{
    public class ShadowMarginStyle : MarginStyle
    {
        private int shadow_size;
        private double shadow_opacity = 0.75;
        private Brush fill;

        public ShadowMarginStyle ()
        {
        }

        public override void Apply (CanvasItem item, Context cr)
        {
            int steps = ShadowSize;
            double opacity_step = ShadowOpacity / ShadowSize;
            Color color = new Color (0, 0, 0);

            double width = Math.Round (item.Allocation.Width);
            double height = Math.Round (item.Allocation.Height);

            if (Fill != null) {
                cr.Rectangle (shadow_size, shadow_size, width - ShadowSize * 2, height - ShadowSize * 2);
                Fill.Apply (cr);
                cr.Fill ();
            }

            cr.LineWidth = 1.0;

            for (int i = 0; i < steps; i++) {
                CairoExtensions.RoundedRectangle (cr,
                    i + 0.5,
                    i + 0.5,
                    (width - 2 * i) - 1,
                    (height - 2 * i) - 1,
                    steps - i);

                color.A = opacity_step * (i + 1);
                cr.Color = color;
                cr.Stroke ();
            }
        }

        public double ShadowOpacity {
            get { return shadow_opacity; }
            set { shadow_opacity = value; }
        }

        public int ShadowSize {
            get { return shadow_size; }
            set { shadow_size = value; }
        }

        public Brush Fill {
            get { return fill; }
            set { fill = value; }
        }
    }
}
