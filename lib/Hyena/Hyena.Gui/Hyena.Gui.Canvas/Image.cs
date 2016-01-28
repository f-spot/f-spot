//
// Image.cs
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

namespace Hyena.Gui.Canvas
{
    public class Image : CanvasItem
    {
        public Image ()
        {
        }

        protected override void ClippedRender (Cairo.Context cr)
        {
            Brush brush = Background;
            if (!brush.IsValid) {
                return;
            }

            double x = Double.IsNaN (brush.Width)
                ? 0
                : (RenderSize.Width - brush.Width) * XAlign;

            double y = Double.IsNaN (brush.Height)
                ? 0
                : (RenderSize.Height - brush.Height) * YAlign;

            cr.Rectangle (0, 0, RenderSize.Width, RenderSize.Height);
            cr.ClipPreserve ();

            if (x != 0 || y != 0) {
                cr.Translate (x, y);
            }

            cr.Antialias = Cairo.Antialias.None;
            brush.Apply (cr);
            cr.Fill ();
        }

        public double XAlign { get; set; }
        public double YAlign { get; set; }
    }
}
