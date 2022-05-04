//
// Thickness.cs
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
	public struct Thickness
	{
		double left;
		double top;
		double right;
		double bottom;

		public static readonly Thickness Zero = new Thickness (0);

		public Thickness (double thickness)
			: this (thickness, thickness, thickness, thickness)
		{
		}

		public Thickness (double xthickness, double ythickness)
			: this (xthickness, ythickness, xthickness, ythickness)
		{
		}

		public Thickness (double left, double top, double right, double bottom)
		{
			this.left = left;
			this.top = top;
			this.right = right;
			this.bottom = bottom;
		}

		public override string ToString ()
		{
			return string.Format ("{0},{1},{2},{3}", double.IsNaN (left) ? "Auto" : left.ToString (),
				double.IsNaN (top) ? "Auto" : top.ToString (),
				double.IsNaN (right) ? "Auto" : right.ToString (),
				double.IsNaN (bottom) ? "Auto" : bottom.ToString ());
		}

		public override bool Equals (object o)
		{
			if (!(o is Thickness)) {
				return false;
			}

			return this == (Thickness)o;
		}

		public bool Equals (Thickness thickness)
		{
			return this == thickness;
		}

		public override int GetHashCode ()
		{
			return left.GetHashCode () ^ top.GetHashCode () ^ right.GetHashCode () ^ bottom.GetHashCode ();
		}

		public static bool operator == (Thickness t1, Thickness t2)
		{
			return t1.left == t2.left &&
				t1.right == t2.right &&
				t1.top == t2.top &&
				t1.bottom == t2.bottom;
		}

		public static bool operator != (Thickness t1, Thickness t2)
		{
			return !(t1 == t2);
		}

		public double Left {
			get { return left; }
			set { left = value; }
		}

		public double Top {
			get { return top; }
			set { top = value; }
		}

		public double Right {
			get { return right; }
			set { right = value; }
		}

		public double Bottom {
			get { return bottom; }
			set { bottom = value; }
		}

		public double X {
			get { return Left + Right; }
		}

		public double Y {
			get { return Top + Bottom; }
		}
	}
}
