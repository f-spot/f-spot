//
// Point.cs
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
    public struct Point
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point (double x, double y) : this ()
        {
            X = x;
            Y = y;
        }

        public void Offset (double dx, double dy)
        {
            X += dx;
            Y += dy;
        }

        public void Offset (Point delta)
        {
            X += delta.X;
            Y += delta.Y;
        }

        public override bool Equals (object o)
        {
            return o is Point ? Equals ((Point)o) : false;
        }

        public bool Equals (Point value)
        {
            return value.X == X && value.Y == Y;
        }

        public static bool operator == (Point point1, Point point2)
        {
            return point1.X == point2.X && point1.Y == point2.Y;
        }

        public static bool operator != (Point point1, Point point2)
        {
            return !(point1 == point2);
        }

        public override int GetHashCode ()
        {
            return X.GetHashCode () ^ Y.GetHashCode ();
        }

        public override string ToString ()
        {
            return String.Format ("{0},{1}", X, Y);
        }
    }
}
