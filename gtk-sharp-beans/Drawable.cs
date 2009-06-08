// Gdk.Drawable.cs
//
// Author(s):
//      Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (c) 2009 Novell, Inc.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of version 2 of the Lesser GNU General 
// Public License as published by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this program; if not, write to the
// Free Software Foundation, Inc., 59 Temple Place - Suite 330,
// Boston, MA 02111-1307, USA.

using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Gdk {
	public static class DrawableExtensions {
		[DllImport("libgdk-win32-2.0-0.dll")]
                static unsafe extern void gdk_draw_rgb_image_dithalign(IntPtr raw, IntPtr gc, int x, int y, int width, int height, int dith, byte* rgb_buf, int rowstride, int xdith, int ydith);
		 
		public unsafe static void DrawRgbImageDithalign(this Drawable drawable, Gdk.GC gc, int x, int y, int width, int height, Gdk.RgbDither dith, byte* rgb_buf, int rowstride, int xdith, int ydith)
		{
			gdk_draw_rgb_image_dithalign(drawable.Handle, gc == null ? IntPtr.Zero : gc.Handle, x, y, width, height, (int) dith, rgb_buf, rowstride, xdith, ydith);
		}
	}
}

