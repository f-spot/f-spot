//
// Brush.cs
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
    public class Brush
    {
        private Cairo.Color color;

        public Brush ()
        {
        }

        public Brush (byte r, byte g, byte b) : this (r, g, b, 255)
        {
        }

        public Brush (byte r, byte g, byte b, byte a)
            : this ((double)r / 255.0, (double)g / 255.0, (double)b / 255.0, (double)a / 255.0)
        {
        }

        public Brush (double r, double g, double b) : this (r, g, b, 1)
        {
        }

        public Brush (double r, double g, double b, double a) : this (new Cairo.Color (r, g, b, a))
        {
        }

        public Brush (Cairo.Color color)
        {
            this.color = color;
        }

        public virtual bool IsValid {
            get { return true; }
        }

        public virtual void Apply (Cairo.Context cr)
        {
            cr.Color = color;
        }

        public static readonly Brush Black = new Brush (0.0, 0.0, 0.0);
        public static readonly Brush White = new Brush (1.0, 1.0, 1.0);

        public virtual double Width {
            get { return Double.NaN; }
        }

        public virtual double Height {
            get { return Double.NaN; }
        }
    }
}
