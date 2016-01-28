//
// Size.cs
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
    public struct Size
    {
        private double width;
        private double height;

        public Size (double width, double height) : this ()
        {
            Width = width;
            Height = height;
        }

        public override bool Equals (object o)
        {
            if (!(o is Size)) {
                return false;
            }

            return Equals ((Size)o);
        }

        public bool Equals (Size value)
        {
            return value.width == width && value.height == height;
        }

        public override int GetHashCode ()
        {
            return ((int)width) ^ ((int)height);
        }

        public static bool operator == (Size size1, Size size2)
        {
            return size1.width == size2.width && size1.height == size2.height;
        }

        public static bool operator != (Size size1, Size size2)
        {
            return size1.width != size2.width || size1.height != size2.height;
        }

        public double Height {
            get { return height; }
            set {
                if (value < 0) {
                    Log.Exception (String.Format ("Height value to set: {0}", value),
                                   new ArgumentException ("Height setter should not receive negative values", "value"));
                    value = 0;
                }

                height = value;
            }
        }

        public double Width {
            get { return width; }
            set {
                if (value < 0) {
                    Log.Exception (String.Format ("Width value to set: {0}", value),
                                   new ArgumentException ("Width setter should not receive negative values", "value"));
                    value = 0;
                }

                width = value;
            }
        }

        public bool IsEmpty {
            get { return width == Double.NegativeInfinity && height == Double.NegativeInfinity; }
        }

        public static Size Empty {
            get {
                Size size = new Size ();
                size.width = Double.NegativeInfinity;
                size.height = Double.NegativeInfinity;
                return size;
            }
        }

        public override string ToString ()
        {
            if (IsEmpty) {
                return "Empty";
            }

            return String.Format ("{0}x{1}", width, height);
        }
    }
}
