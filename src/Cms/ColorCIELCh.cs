/*
 * Cms.ColorCIELCh.cs A very incomplete wrapper for lcms
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
	public struct ColorCIELCh {
		public double L;
		public double C;
		public double h;
		
		public ColorCIELab ToLab ()
		{
			ColorCIELab lab;
			NativeMethods.CmsLCh2Lab (out lab, ref this);

			return lab;
		}
	}
}
