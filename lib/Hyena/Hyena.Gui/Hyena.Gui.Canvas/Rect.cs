//
// Rect.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2009-2010 Novell, Inc.
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

namespace Hyena.Gui.Canvas
{
    public struct Rect
    {
        private double x, y, w, h;

        public Rect (double x, double y, double width, double height) : this ()
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public Rect (Point point1, Point point2) : this ()
        {
            X = Math.Min (point1.X, point2.X);
            Y = Math.Min (point1.Y, point2.Y);
            Width = Math.Abs (point2.X - point1.X);
            Height = Math.Abs (point2.Y - point1.Y);
        }

        public Rect (Point location, Size size) : this ()
        {
            X = location.X;
            Y = location.Y;
            Width = size.Width;
            Height = size.Height;
        }

        public override string ToString ()
        {
            if (IsEmpty) {
                return "Empty";
            }

            return String.Format ("{0}+{1},{2}x{3}", x, y, w, h);
        }

        public double X {
            get { return x; }
            set { x = value; }
        }

        public double Y {
            get { return y; }
            set { y = value; }
        }

        public double Width {
            get { return w; }
            set {
                if (value < 0) {
                    Log.Exception (String.Format ("Width value to set: {0}", value),
                                   new ArgumentException ("Width setter should not receive negative values", "value"));
                    value = 0;
                }

                w = value;
            }
        }

        public double Height {
            get { return h; }
            set {
                if (value < 0) {
                    Log.Exception (String.Format ("Height value to set: {0}", value),
                                   new ArgumentException ("Height setter should not receive negative values", "value"));
                    value = 0;
                }

                h = value;
            }
        }

        public bool Contains (double px, double py)
        {
            return !(px < x || px > x + w || py < y || py > y + h);
        }

        public bool Contains (Point point)
        {
            return Contains (point.X, point.Y);
        }

        public bool IntersectsWith (Rect rect)
        {
            return !(Left > rect.Right ||
                Right < rect.Left ||
                Top > rect.Bottom ||
                Bottom < rect.Top);
        }

        public static Rect Empty {
            get {
                var empty = new Rect (Double.PositiveInfinity, Double.PositiveInfinity, 0, 0);
                empty.w = empty.h = Double.NegativeInfinity;
                return empty;
            }
        }

        public bool IsEmpty {
            get { return w < 0 && h < 0; }
        }

        public double Left {
            get { return x; }
        }

        public double Top {
            get { return y; }
        }

        public double Right {
            get { return IsEmpty ? Double.NegativeInfinity : x + w; }
        }

        public double Bottom {
            get { return IsEmpty ? Double.NegativeInfinity : y + h; }
        }

        public Size Size {
            get { return new Size (Width, Height); }
        }

        public Point Point {
            get { return new Point (X, Y); }
        }

        public void Intersect (Rect rect)
        {
            if (IsEmpty || rect.IsEmpty) {
                this = Rect.Empty;
                return;
            }

            double new_x = Math.Max (x, rect.x);
            double new_y = Math.Max (y, rect.y);
            double new_w = Math.Min (Right, rect.Right) - new_x;
            double new_h = Math.Min (Bottom, rect.Bottom) - new_y;

            x = new_x;
            y = new_y;
            w = new_w;
            h = new_h;

            if (w < 0 || h < 0) {
                this = Rect.Empty;
            }
        }

        public void Union (Rect rect)
        {
            if (IsEmpty) {
                x = rect.x;
                y = rect.y;
                h = rect.h;
                w = rect.w;
            } else if (!rect.IsEmpty) {
                double new_x = Math.Min (Left, rect.Left);
                double new_y = Math.Min (Top, rect.Top);
                double new_w = Math.Max (Right, rect.Right) - new_x;
                double new_h = Math.Max (Bottom, rect.Bottom) - new_y;

                x = new_x;
                y = new_y;
                w = new_w;
                h = new_h;
            }
        }

        public void Union (Point point)
        {
            Union (new Rect (point, point));
        }

        public void Offset (Rect rect)
        {
            x += rect.X;
            y += rect.Y;
        }

        public void Offset (Point point)
        {
            x += point.X;
            y += point.Y;
        }

        public void Offset (double dx, double dy)
        {
            x += dx;
            y += dy;
        }

        public static bool operator == (Rect rect1, Rect rect2)
        {
            return rect1.x == rect2.x && rect1.y == rect2.y && rect1.w == rect2.w && rect1.h == rect2.h;
        }

        public static bool operator != (Rect rect1, Rect rect2)
        {
            return !(rect1 == rect2);
        }

        public override bool Equals (object o)
        {
            if (o is Rect) {
                return this == (Rect)o;
            }
            return false;
        }

        public bool Equals (Rect value)
        {
            return this == value;
        }

        public override int GetHashCode ()
        {
            return x.GetHashCode () ^ y.GetHashCode () ^ w.GetHashCode () ^ h.GetHashCode ();
        }

#region GDK/Cairo extensions

        public static explicit operator Rect (Gdk.Rectangle rect)
        {
            return new Rect () {
                X = rect.X,
                Y = rect.Y,
                Width = rect.Width,
                Height = rect.Height
            };
        }

        public static explicit operator Gdk.Rectangle (Rect rect)
        {
            return new Gdk.Rectangle () {
                X = (int)Math.Floor (rect.X),
                Y = (int)Math.Floor (rect.Y),
                Width = (int)Math.Ceiling (rect.Width),
                Height = (int)Math.Ceiling (rect.Height)
            };
        }

        public static explicit operator Rect (Cairo.Rectangle rect)
        {
            return new Rect (rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static explicit operator Cairo.Rectangle (Rect rect)
        {
            return new Cairo.Rectangle (rect.X, rect.Y, rect.Width, rect.Height);
        }

#endregion

    }
}
