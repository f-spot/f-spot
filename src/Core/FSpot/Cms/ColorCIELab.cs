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

using System;

namespace FSpot.Cms
{
	public struct ColorCIELab
	{
		public double L { get; set; }
		public double a { get; set; }
		public double b { get; set; }

		public ColorCIELCh ToLCh ()
		{
			NativeMethods.CmsLab2LCh (out var lch, ref this);

			return lch;
		}

		public ColorCIEXYZ ToXYZ (ColorCIEXYZ wp)
		{
			NativeMethods.CmsLab2XYZ (ref wp, out var xyz, ref this);

			return xyz;
		}

		public override string ToString ()
		{
			return $"(L={L}, a={a}, b={b})";
		}
	}
}
