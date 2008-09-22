using System;

namespace FSpot {
	public class Vector {
		double X;
		double Y;
		
		public Vector (Gdk.Point p)
		{
			X = p.X;
			Y = p.Y;
		}

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

		double Dot (Vector v)
		{
			return Dot (this, v);
		}

		double Length {
			get {
				return Math.Sqrt (X * X + Y * Y);
			}
		}

		public override string ToString ()
		{
			return String.Format ("v({0},{1}).{2}", X, Y, Length);
		}
	}
}
