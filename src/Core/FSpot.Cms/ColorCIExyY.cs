//
// ColorCIExyY.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

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
				var reader = new StreamReader (stream, System.Text.Encoding.ASCII);
				string line = null;
				for (int i = 0; i <= temp - 1000; i++) {
					line = reader.ReadLine ();
				}
				
				//System.Console.WriteLine (line);
				string [] subs = line.Split ('\t');
				int ptemp = int.Parse (subs [0]);
				if (ptemp != temp)
					throw new CmsException (string.Format ("{0} != {1}", ptemp, temp));
				
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
			return ToXYZ ().ToLab (wp);
		}

		public ColorCIELab ToLab (ColorCIEXYZ wp)
		{
			return ToXYZ ().ToLab (wp);
		}

		public override string ToString ()
		{
			return string.Format ("(x={0}, y={1}, Y={2})", x, y, Y);
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
