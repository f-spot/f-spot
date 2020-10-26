//
// Vector.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2006 Novell, Inc.
// Copyright (C) 2006 Larry Ewing
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace FSpot.Utils
{
	public class Vector
	{
		double X;
		double Y;

		public Vector (double x, double y)
		{
			X = x;
			Y = y;
		}

		public void Normalize ()
		{
			double len = Length;
			X /= len;
			Y /= len;
		}

		public double AngleBetween (Vector v)
		{
			return AngleBetween (this, v);
		}

		public static double AngleBetween (Vector v1, Vector v2)
		{
			double val = Dot (v1, v2) / (v1.Length * v2.Length);
			val = Math.Acos (val);
			if (!Right (v1, v2))
				return -val;
			return val;
		}

		static bool Right (Vector v1, Vector v2)
		{
			return (v1.X * v2.Y - v1.Y * v2.X) > 0;
		}

		public static double Dot (Vector v1, Vector v2)
		{
			return v1.X * v2.X + v1.Y * v2.Y;
		}

		double Length => Math.Sqrt (X * X + Y * Y);

		public override string ToString ()
		{
			return $"v({X},{Y}).{Length}";
		}
	}
}
