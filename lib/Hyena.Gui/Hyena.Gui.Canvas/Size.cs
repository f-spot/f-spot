//
// Size.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2009-2010 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Hyena.Gui.Canvas
{
	public struct Size
	{
		double width;
		double height;

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
					Log.Exception (string.Format ("Height value to set: {0}", value),
								   new ArgumentException ("Height setter should not receive negative values", nameof (value)));
					value = 0;
				}

				height = value;
			}
		}

		public double Width {
			get { return width; }
			set {
				if (value < 0) {
					Log.Exception (string.Format ("Width value to set: {0}", value),
								   new ArgumentException ("Width setter should not receive negative values", nameof (value)));
					value = 0;
				}

				width = value;
			}
		}

		public bool IsEmpty {
			get { return width == double.NegativeInfinity && height == double.NegativeInfinity; }
		}

		public static Size Empty {
			get {
				var size = new Size ();
				size.width = double.NegativeInfinity;
				size.height = double.NegativeInfinity;
				return size;
			}
		}

		public override string ToString ()
		{
			if (IsEmpty) {
				return "Empty";
			}

			return string.Format ("{0}x{1}", width, height);
		}
	}
}
