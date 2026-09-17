// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace FSpot.Utils
{
	// GTK Loupe.Layout + DrawShape: lens offset from the pointer so the
	// handle/hotspot sits on the sampled pixel instead of covering it.
	public readonly struct LoupeLayout
	{
		public LoupeLayout (double windowX, double windowY, double width, double height,
			double lensX, double lensY, double hotspotX, double hotspotY,
			double stemEndX, double stemEndY, double angle, int radius, int inner, int border)
		{
			WindowX = windowX;
			WindowY = windowY;
			Width = width;
			Height = height;
			LensX = lensX;
			LensY = lensY;
			HotspotX = hotspotX;
			HotspotY = hotspotY;
			StemEndX = stemEndX;
			StemEndY = stemEndY;
			Angle = angle;
			Radius = radius;
			Inner = inner;
			Border = border;
		}

		public double WindowX { get; }
		public double WindowY { get; }
		public double Width { get; }
		public double Height { get; }
		public double LensX { get; }
		public double LensY { get; }
		public double HotspotX { get; }
		public double HotspotY { get; }
		public double StemEndX { get; }
		public double StemEndY { get; }
		public double Angle { get; }
		public int Radius { get; }
		public int Inner { get; }
		public int Border { get; }

		public static LoupeLayout ForPointer (double pointerX, double pointerY,
			double angle = Math.PI / 4, int radius = LoupeRegion.DefaultRadius, int inner = LoupeRegion.DefaultRadius, int border = 6)
		{
			double a = radius + border;
			double b = inner + border;
			double xProj = (a + b - border) * Math.Cos (angle);
			double yProj = (a + b - border) * Math.Sin (angle);
			double lensX = Math.Ceiling (Math.Max (-xProj + b, a));
			double lensY = Math.Ceiling (Math.Max (-yProj + b, a));
			double width = Math.Ceiling (Math.Max (Math.Abs (xProj) + b, a) + b + a);
			double height = Math.Ceiling (Math.Max (Math.Abs (yProj) + b, a) + b + a);
			double hotspotX = Math.Ceiling (lensX + xProj);
			double hotspotY = Math.Ceiling (lensY + yProj);
			double stem = radius + border + inner;
			double stemEndX = lensX + stem * Math.Cos (angle);
			double stemEndY = lensY + stem * Math.Sin (angle);
			return new LoupeLayout (
				pointerX - hotspotX, pointerY - hotspotY, width, height,
				lensX, lensY, hotspotX, hotspotY, stemEndX, stemEndY,
				angle, radius, inner, border);
		}
	}
}
