//
// ColorCIEXYZ.cs
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
	public struct ColorCIEXYZ
	{
		public double x;
		public double y;
		public double z;

		public ColorCIEXYZ (double x, double y, double z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public ColorCIExyY ToxyY ()
		{
			return ColorCIExyY.FromNative (Colorimetric.XYZ2xyY (ToNative ()));
		}

		public ColorCIELab ToLab (ColorCIEXYZ wp)
		{
			return ColorCIELab.FromNative (Colorimetric.XYZ2Lab (wp.ToNative (), ToNative ()));
		}

		public static ColorCIEXYZ D50 => FromNative (Colorimetric.D50_XYZ);

		public ColorCIELab ToLab (ColorCIExyY wp)
		{
			return ToLab (wp.ToXYZ ());
		}

		internal CIEXYZ ToNative ()
		{
			return new CIEXYZ { X = x, Y = y, Z = z };
		}

		internal static ColorCIEXYZ FromNative (CIEXYZ native)
		{
			return new ColorCIEXYZ (native.X, native.Y, native.Z);
		}

		public override string ToString ()
		{
			return $"(x={x}, y={y}, z={z})";
		}
	}
}
