// 
// Prelight.cs
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

using Hyena.Gui.Theming;
using Hyena.Gui.Theatrics;
using Hyena.Gui.Canvas;

namespace Hyena.Gui.Canvas
{
    public static class Prelight
    {
        public static void Gradient (Cairo.Context cr, Theme theme, Rect rect, double opacity)
        {
            cr.Save ();
            cr.Translate (rect.X, rect.Y);

            var x = rect.Width / 2.0;
            var y = rect.Height / 2.0;
            var grad = new Cairo.RadialGradient (x, y, 0, x, y, rect.Width / 2.0);
            grad.AddColorStop (0, new Cairo.Color (0, 0, 0, 0.1 * opacity));
            grad.AddColorStop (1, new Cairo.Color (0, 0, 0, 0.35 * opacity));
            cr.Pattern = grad;
            CairoExtensions.RoundedRectangle (cr, rect.X, rect.Y, rect.Width, rect.Height, theme.Context.Radius);
            cr.Fill ();
            grad.Destroy ();

            cr.Restore ();
        }
    }
}
