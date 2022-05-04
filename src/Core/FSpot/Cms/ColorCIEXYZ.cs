//
// ColorCIEXYZ.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

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
			NativeMethods.CmsXYZ2xyY (out var dest, ref this);

			return dest;
		}

		public ColorCIELab ToLab (ColorCIEXYZ wp)
		{
			NativeMethods.CmsXYZ2Lab (ref wp, out var lab, ref this);

			return lab;
		}

		public static ColorCIEXYZ FromPtr (IntPtr ptr)
		{
			if (ptr == IntPtr.Zero)
				throw new ArgumentNullException (nameof (ptr), "lcms Color argument was null");

			return (ColorCIEXYZ)Marshal.PtrToStructure (ptr, typeof (ColorCIEXYZ));
		}

		public static ColorCIEXYZ D50 {
			get {
				IntPtr ptr = NativeMethods.CmsD50XYZ ();
				return FromPtr (ptr);
			}
		}

		public ColorCIELab ToLab (ColorCIExyY wp)
		{
			return ToLab (wp.ToXYZ ());
		}

		public override string ToString ()
		{
			return $"(x={x}, y={y}, z={z})";
		}
	}

	public struct ColorCIEXYZTriple
	{
		public ColorCIEXYZ Red;
		public ColorCIEXYZ Blue;
		public ColorCIEXYZ Green;

		public ColorCIEXYZTriple (ColorCIEXYZ red, ColorCIEXYZ green, ColorCIEXYZ blue)
		{
			Red = red;
			Blue = blue;
			Green = green;
		}
	}
}
