//
// ColorCIELab.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using lcmsNET;

namespace FSpot.Cms
{
	public struct ColorCIELab
	{
		public double L;
		public double a;
		public double b;

		public ColorCIEXYZ ToXYZ (ColorCIEXYZ wp)
		{
			return ColorCIEXYZ.FromNative (Colorimetric.Lab2XYZ (wp.ToNative (), ToNative ()));
		}

		internal CIELab ToNative ()
		{
			return new CIELab { L = L, a = a, b = b };
		}

		internal static ColorCIELab FromNative (CIELab native)
		{
			return new ColorCIELab { L = native.L, a = native.a, b = native.b };
		}

		public override string ToString ()
		{
			return $"(L={L}, a={a}, b={b})";
		}
	}
}
