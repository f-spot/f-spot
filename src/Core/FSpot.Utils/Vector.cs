//
// Vector.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2006 Novell, Inc.
// Copyright (C) 2006 Larry Ewing
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
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

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
			X = X / len;
			Y = Y / len;
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
