//
// Point.cs
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
			return string.Format ("{0},{1}", X, Y);
		}
	}
}
