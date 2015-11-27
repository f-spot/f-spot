//
// CairoDamageDebugger.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2010 Novell, Inc.
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
using Cairo;

namespace Hyena.Gui
{
    public static class CairoDamageDebugger
    {
        private static Random rand = new Random ();

        public static void RenderDamage (this Context cr, Gdk.Rectangle damage)
        {
            RenderDamage (cr, damage.X, damage.Y, damage.Width, damage.Height);
        }

        public static void RenderDamage (this Context cr, Cairo.Rectangle damage)
        {
            RenderDamage (cr, damage.X, damage.Y, damage.Width, damage.Height);
        }

        public static void RenderDamage (this Context cr, double x, double y, double w, double h)
        {
            cr.Save ();
            cr.LineWidth = 1.0;
            cr.Color = CairoExtensions.RgbToColor ((uint)rand.Next (0, 0xffffff));
            cr.Rectangle (x + 0.5, y + 0.5, w - 1, h - 1);
            cr.Stroke ();
            cr.Restore ();
        }
    }
}
