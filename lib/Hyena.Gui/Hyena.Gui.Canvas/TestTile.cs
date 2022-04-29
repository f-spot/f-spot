//
// TestTile.cs
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
	public class TestTile : CanvasItem
    {
        static Random rand = new Random ();
        Color color;
        bool color_set;

        bool change_on_render = true;
        public bool ChangeOnRender {
            get { return change_on_render; }
            set { change_on_render = value; }
        }

        public TestTile ()
        {
        }

        protected override void ClippedRender (Context cr)
        {
            if (!color_set || ChangeOnRender) {
                color = CairoExtensions.RgbToColor ((uint)rand.Next (0, 0xffffff));
                color_set = true;
            }

            CairoExtensions.RoundedRectangle (cr, 0, 0, RenderSize.Width, RenderSize.Height, 5);
	    cr.SetSourceColor (color);
            cr.Fill ();
        }
    }
}
