/*
 * Cms.ColorCIExyY.cs A very incomplete wrapper for lcms
 *
 * Author(s):
 *	Larry Ewing  <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.Serialization;

namespace Cms {
	public struct ColorCIExyY {
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
			double x, y;
			CctTable.GetXY (temp, out x, out y);
			return new ColorCIExyY (x, y, 1.0);
		}

		public static ColorCIExyY WhitePointFromTemperatureCIE (int temp)
		{
			ColorCIExyY wp;
			NativeMethods.CmsWhitePointFromTemp (temp, out wp);
			return wp;
		}

		public static ColorCIExyY WhitePointFromTemperatureResource (int temp, string name)
		{
			ColorCIExyY wp;
			//const int line_size = 0x1e;

			using (Stream stream = Assembly.GetExecutingAssembly ().GetManifestResourceStream (name)) {
				StreamReader reader = new StreamReader (stream, System.Text.Encoding.ASCII);
				string line = null;
				for (int i = 0; i <= temp - 1000; i++) {
					line = reader.ReadLine ();
				}
				
				//System.Console.WriteLine (line);
				string [] subs = line.Split ('\t');
				int ptemp = int.Parse (subs [0]);
				if (ptemp != temp)
					throw new System.Exception (String.Format ("{0} != {1}", ptemp, temp));
				
				double x = double.Parse (subs [1]);
				double y = double.Parse (subs [2]);
				wp = new ColorCIExyY (x, y, 1.0);
				return wp;
			}			
		}

		public ColorCIEXYZ ToXYZ ()
		{
			ColorCIEXYZ dest;
			NativeMethods.CmsxyY2XYZ (out dest, ref this);
			
			return dest;
		}

		public static ColorCIExyY D50 {
			get {
				IntPtr ptr = NativeMethods.CmsD50xyY ();
				return (ColorCIExyY) Marshal.PtrToStructure (ptr, typeof (ColorCIExyY));
			}
		}

		public ColorCIELab ToLab (ColorCIExyY wp)
		{
			return this.ToXYZ ().ToLab (wp);
		}

		public ColorCIELab ToLab (ColorCIEXYZ wp)
		{
			return this.ToXYZ ().ToLab (wp);
		}

		public override string ToString ()
		{
			return String.Format ("(x={0}, y={1}, Y={2})", x, y, Y);
		}
	}

	public struct ColorCIExyYTriple {
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
