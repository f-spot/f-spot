/*
 * Cms.ColorCIEXYZ.cs A very incomplete wrapper for lcms
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
	public struct ColorCIEXYZ {
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

		public static ColorCIEXYZ D50 {
			get {
				IntPtr ptr = NativeMethods.CmsD50XYZ ();
				return (ColorCIEXYZ) Marshal.PtrToStructure (ptr, typeof (ColorCIEXYZ));
			}
		}

		public ColorCIELab ToLab (ColorCIExyY wp)
		{
			return ToLab (wp.ToXYZ ());
		}

		public override string ToString ()
		{
			return String.Format ("(x={0}, y={1}, z={2})", x, y, z);
		}
	}

	public struct ColorCIEXYZTriple {
		public ColorCIEXYZ Red;
		public ColorCIEXYZ Blue;
		public ColorCIEXYZ Green;

		ColorCIEXYZTriple (ColorCIEXYZ red, ColorCIEXYZ green, ColorCIEXYZ blue)
		{
			Red = red;
			Blue = blue;
			Green = green;
		}
	}
}
