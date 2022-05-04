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

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

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

		public static ColorCIExyY WhitePointFromTemperatureCIE (int temp)
		{
			NativeMethods.CmsWhitePointFromTemp (temp, out var wp);
			return wp;
		}

		public static ColorCIExyY WhitePointFromTemperatureResource (int temp, string name)
		{
			ColorCIExyY wp;
			//const int line_size = 0x1e;

			using var stream = Assembly.GetExecutingAssembly ().GetManifestResourceStream (name);
			using var reader = new StreamReader (stream, System.Text.Encoding.ASCII);
			string line = null;
			for (int i = 0; i <= temp - 1000; i++) {
				line = reader.ReadLine ();
			}

			//System.Console.WriteLine (line);
			string[] subs = line.Split ('\t');
			int ptemp = int.Parse (subs[0]);
			if (ptemp != temp)
				throw new CmsException ($"{ptemp} != {temp}");

			double x = double.Parse (subs[1]);
			double y = double.Parse (subs[2]);
			wp = new ColorCIExyY (x, y, 1.0);
			return wp;
		}

		public ColorCIEXYZ ToXYZ ()
		{
			NativeMethods.CmsxyY2XYZ (out var dest, ref this);

			return dest;
		}

		public static ColorCIExyY D50 {
			get {
				IntPtr ptr = NativeMethods.CmsD50xyY ();
				return (ColorCIExyY)Marshal.PtrToStructure (ptr, typeof (ColorCIExyY));
			}
		}

		public ColorCIELab ToLab (ColorCIExyY wp)
		{
			return ToXYZ ().ToLab (wp);
		}

		public ColorCIELab ToLab (ColorCIEXYZ wp)
		{
			return ToXYZ ().ToLab (wp);
		}

		public override string ToString ()
		{
			return $"(x={x}, y={y}, Y={Y})";
		}
	}

	public struct ColorCIExyYTriple
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
	}
}
