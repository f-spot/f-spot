//
// ShadingTestWindow.cs
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

namespace Hyena.Gui
{
    public class ShadingTestWindow : Window
    {
        private int steps = 16;

        public ShadingTestWindow () : base ("Shading Test")
        {
            SetSizeRequest (512, 512);
        }

        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {
            Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window);

            double step_width = Allocation.Width / (double)steps;
            double step_height = Allocation.Height / (double)steps;
            double h = 1.0;
            double s = 0.0;

            for (int xi = 0, i = 0; xi < steps; xi++) {
                for (int yi = 0; yi < steps; yi++, i++) {
                    double bg_b = (double)(i / 255.0);
                    double fg_b = 1.0 - bg_b;

                    double x = Allocation.X + xi * step_width;
                    double y = Allocation.Y + yi * step_height;

                    cr.Rectangle (x, y, step_width, step_height);
                    cr.Color = CairoExtensions.ColorFromHsb (h, s, bg_b);
                    cr.Fill ();

                    int tw, th;
                    Pango.Layout layout = new Pango.Layout (PangoContext);
                    layout.SetText (((int)(bg_b * 255.0)).ToString ());
                    layout.GetPixelSize (out tw, out th);

                    cr.Translate (0.5, 0.5);
                    cr.MoveTo (x + (step_width - tw) / 2.0, y + (step_height - th) / 2.0);
                    cr.Color = CairoExtensions.ColorFromHsb (h, s, fg_b);
                    PangoCairoHelper.ShowLayout (cr, layout);
                    cr.Translate (-0.5, -0.5);
                }
            }

            CairoExtensions.DisposeContext (cr);
            return true;
        }

    }
}
