// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace FSpot.Editors
{
	/// <summary>
	/// GtkCurve in spline mode, which is the curve the black-and-white editor samples.
	/// A straight line from (0, 0) to (255, 255) is the table ConvertToBw uses when no curve is set.
	/// </summary>
	public sealed class SplineCurve
	{
		public const int Radius = 3;
		public const int MinDistance = 8;
		public const float Min = 0;
		public const float Max = 255;
		public const int Length = 256;

		readonly List<Point> points = new List<Point> ();
		int grab = -1;

		public SplineCurve ()
			=> Reset ();

		public IReadOnlyList<(float X, float Y)> ControlPoints {
			get {
				var visible = new List<(float X, float Y)> ();
				foreach (var point in points) {
					if (point.X >= Min)
						visible.Add ((point.X, point.Y));
				}
				return visible;
			}
		}

		public void Reset ()
		{
			points.Clear ();
			points.Add (new Point (Min, Min));
			points.Add (new Point (Max, Max));
			grab = -1;
		}

		public byte[] ToBytes ()
		{
			float[] samples = Sample (Length);
			var bytes = new byte[Length];
			for (int i = 0; i < Length; i++)
				bytes[i] = (byte)samples[i];
			return bytes;
		}

		public float[] Sample (int length)
		{
			if (length < 2)
				throw new ArgumentOutOfRangeException (nameof (length));

			var activeX = new List<float> ();
			var activeY = new List<float> ();
			float previous = Min - 1;
			foreach (var point in points) {
				if (point.X <= previous)
					continue;
				previous = point.X;
				activeX.Add (point.X);
				activeY.Add (point.Y);
			}

			var vector = new float[length];
			if (activeX.Count < 2) {
				float y = activeY.Count > 0 ? activeY[0] : Min;
				y = Clamp (y);
				for (int i = 0; i < length; i++)
					vector[i] = y;
				return vector;
			}

			float[] y2 = SplineSolve (activeX, activeY);
			float x = Min;
			float dx = (Max - Min) / (length - 1);
			for (int i = 0; i < length; i++, x += dx)
				vector[i] = Clamp (SplineEval (activeX, activeY, y2, x));
			return vector;
		}

		// Widget coordinates. Width and height are the graph inside the Radius margin, as in GtkCurve.
		public void Press (float tx, float ty, int width, int height)
		{
			if (width < 2 || height < 2)
				return;

			int x = ClampPixel ((int)tx - Radius, width);
			int y = ClampPixel ((int)ty - Radius, height);
			int closest = Closest (x, width, out int distance);
			if (distance > MinDistance) {
				int cx = Project (points[closest].X, width);
				if (x > cx)
					closest++;
				points.Insert (closest, new Point (Min, Min));
			}

			grab = closest;
			points[grab] = new Point (Unproject (x, width), Unproject (height - y, height));
		}

		public void Drag (float tx, float ty, int width, int height)
		{
			if (grab < 0 || width < 2 || height < 2)
				return;

			int x = ClampPixel ((int)tx - Radius, width);
			int y = ClampPixel ((int)ty - Radius, height);
			float left = -MinDistance;
			if (grab > 0)
				left = Project (points[grab - 1].X, width);
			float right = width + Radius * 2 + MinDistance;
			if (grab + 1 < points.Count)
				right = Project (points[grab + 1].X, width);

			if (tx <= left || tx >= right
				|| ty > height + Radius * 2 + MinDistance
				|| ty < -MinDistance)
				points[grab] = new Point (Min - 1, points[grab].Y);
			else
				points[grab] = new Point (Unproject (x, width), Unproject (height - y, height));
		}

		public void Release ()
		{
			points.RemoveAll (point => point.X < Min);
			if (points.Count == 0)
				points.Add (new Point (Min, Min));
			grab = -1;
		}

		int Closest (int x, int width, out int distance)
		{
			int closest = 0;
			distance = int.MaxValue;
			for (int i = 0; i < points.Count; i++) {
				int gap = Math.Abs (x - Project (points[i].X, width));
				if (gap < distance) {
					distance = gap;
					closest = i;
				}
			}
			return closest;
		}

		static int ClampPixel (int value, int size)
		{
			if (value < 0)
				return 0;
			if (value > size - 1)
				return size - 1;
			return value;
		}

		static float Clamp (float y)
		{
			if (y < Min)
				return Min;
			if (y > Max)
				return Max;
			return y;
		}

		static int Project (float value, int norm)
			=> (int)((norm - 1) * ((value - Min) / (Max - Min)) + 0.5f);

		static float Unproject (int value, int norm)
			=> value / (float)(norm - 1) * (Max - Min) + Min;

		static float[] SplineSolve (List<float> x, List<float> y)
		{
			int n = x.Count;
			var y2 = new float[n];
			var u = new float[n - 1];
			for (int i = 1; i < n - 1; i++) {
				float sig = (x[i] - x[i - 1]) / (x[i + 1] - x[i - 1]);
				float p = sig * y2[i - 1] + 2.0f;
				y2[i] = (sig - 1.0f) / p;
				u[i] = (y[i + 1] - y[i]) / (x[i + 1] - x[i]) - (y[i] - y[i - 1]) / (x[i] - x[i - 1]);
				u[i] = (6.0f * u[i] / (x[i + 1] - x[i - 1]) - sig * u[i - 1]) / p;
			}

			for (int k = n - 2; k >= 0; k--)
				y2[k] = y2[k] * y2[k + 1] + u[k];
			return y2;
		}

		static float SplineEval (List<float> x, List<float> y, float[] y2, float val)
		{
			int low = 0;
			int high = x.Count - 1;
			while (high - low > 1) {
				int mid = (high + low) / 2;
				if (x[mid] > val)
					high = mid;
				else
					low = mid;
			}

			float h = x[high] - x[low];
			float a = (x[high] - val) / h;
			float b = (val - x[low]) / h;
			return a * y[low] + b * y[high]
				+ ((a * a * a - a) * y2[low] + (b * b * b - b) * y2[high]) * (h * h) / 6.0f;
		}

		struct Point
		{
			public Point (float x, float y)
			{
				X = x;
				Y = y;
			}

			public float X { get; }
			public float Y { get; }
		}
	}
}
