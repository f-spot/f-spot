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

namespace FSpot.Cms
{
	public struct ColorCIELab
	{
		public double L;
		public double a;
		public double b;

		public ColorCIELCh ToLCh ()
		{
			ColorCIELCh lch;
			NativeMethods.CmsLab2LCh (out lch, ref this);

			return lch;
		}

		public ColorCIEXYZ ToXYZ (ColorCIEXYZ wp)
		{
			ColorCIEXYZ xyz;
			NativeMethods.CmsLab2XYZ (ref wp, out xyz, ref this);

			return xyz;
		}

		public override string ToString ()
		{
			return string.Format ("(L={0}, a={1}, b={2})", L, a, b);
		}
	}
}
