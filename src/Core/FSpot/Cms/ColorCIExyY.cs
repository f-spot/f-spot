//
// ColorCIExyY.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using lcmsNET;

namespace FSpot.Cms
{
	public struct ColorCIExyY
	{
		public double x;
		public double y;
		public double Y;

		public ColorCIExyY (double x, double y, double Y)
		{
			this.x = x;
			this.y = y;
			this.Y = Y;
		}

		public static ColorCIExyY WhitePointFromTemperature (int temp)
		{
			CctTable.GetXY (temp, out var x, out var y);
			return new ColorCIExyY (x, y, 1.0);
		}

		public ColorCIEXYZ ToXYZ ()
		{
			return ColorCIEXYZ.FromNative (Colorimetric.xyY2XYZ (ToNative ()));
		}

		public static ColorCIExyY D50 => FromNative (Colorimetric.D50_xyY);

		internal CIExyY ToNative ()
		{
			return new CIExyY { x = x, y = y, Y = Y };
		}

		internal static ColorCIExyY FromNative (CIExyY native)
		{
			return new ColorCIExyY (native.x, native.y, native.Y);
		}

		public override string ToString ()
		{
			return $"(x={x}, y={y}, Y={Y})";
		}
	}

	internal struct ColorCIExyYTriple
	{
		public ColorCIExyY Red;
		public ColorCIExyY Green;
		public ColorCIExyY Blue;

		public ColorCIExyYTriple (ColorCIExyY red, ColorCIExyY green, ColorCIExyY blue)
		{
			Red = red;
			Green = green;
			Blue = blue;
		}

		internal CIExyYTRIPLE ToNative ()
		{
			return new CIExyYTRIPLE { Red = Red.ToNative (), Green = Green.ToNative (), Blue = Blue.ToNative () };
		}
	}
}
