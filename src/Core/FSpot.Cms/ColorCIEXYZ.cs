//
// ColorCIEXYZ.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
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
			ColorCIExyY dest;
			NativeMethods.CmsXYZ2xyY (out dest, ref this);
			
			return dest;
		}

		public ColorCIELab ToLab (ColorCIEXYZ wp)
		{
			ColorCIELab lab;
			NativeMethods.CmsXYZ2Lab (ref wp, out lab, ref this);

			return lab;
		}

		public static ColorCIEXYZ FromPtr(IntPtr ptr)
		{
			if (ptr == IntPtr.Zero)
				throw new ArgumentNullException ("ptr", "lcms Color argument was null");
			return (ColorCIEXYZ) Marshal.PtrToStructure (ptr, typeof (ColorCIEXYZ));
		}

		public static ColorCIEXYZ D50 {
			get {
				IntPtr ptr = NativeMethods.CmsD50XYZ ();
				return FromPtr(ptr);
			}
		}

		public ColorCIELab ToLab (ColorCIExyY wp)
		{
			return ToLab (wp.ToXYZ ());
		}

		public override string ToString ()
		{
			return string.Format ("(x={0}, y={1}, z={2})", x, y, z);
		}
	}

	public struct ColorCIEXYZTriple {
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
