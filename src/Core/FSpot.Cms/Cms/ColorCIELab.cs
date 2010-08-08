/*
 * Cms.ColorCIELab.cs A very incomplete wrapper for lcms
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
	public struct ColorCIELab {
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
			return String.Format ("(L={0}, a={1}, b={2})", L, a, b);
		}
	}
}
