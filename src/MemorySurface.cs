/* 
 * MemorySurface.cs
 *
 * Copyright 2007 Novell Inc.
 *
 * Author
 *   Larry Ewing <lewing@novell.com>
 *
 * See COPYING for license information.
 *
 */

using System;
using System.Runtime.InteropServices;

namespace FSpot {
	// FIXME this class is a hack to have get_data functionality
	// on cairo 1.0.x
	public sealed class MemorySurface : Cairo.Surface {
		[DllImport ("libfspot")]
		static extern IntPtr f_image_surface_create (Cairo.Format format, int width, int height);
		
		[DllImport ("libfspot")]
		static extern IntPtr f_image_surface_get_data (IntPtr surface);

		[DllImport ("libfspot")]
		static extern Cairo.Format f_image_surface_get_format (IntPtr surface);

		[DllImport ("libfspot")]
		static extern int f_image_surface_get_width (IntPtr surface);

		[DllImport ("libfspot")]
		static extern int f_image_surface_get_height (IntPtr surface);

		public MemorySurface (Cairo.Format format, int width, int height)
			: this (f_image_surface_create (format, width, height))
		{
		}

		public MemorySurface (IntPtr handle) : base (handle, true)
		{
			if (DataPtr == IntPtr.Zero)
				throw new ApplicationException ("Missing image data");
		}

		public IntPtr DataPtr {
			get {
				return f_image_surface_get_data (Handle);
			}
		}

		public Cairo.Format Format {
			get {
				return f_image_surface_get_format (Handle);
			}
		}
		
		public int Width {
			get { return f_image_surface_get_width (Handle); }
		}

		public int Height {
			get { return f_image_surface_get_height (Handle); }
		}
	}
}


