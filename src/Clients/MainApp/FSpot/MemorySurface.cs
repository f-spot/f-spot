/*
 * MemorySurface.cs
 *
 * Copyright 2007 Novell Inc.
 *
 * Author
 * 	Larry Ewing <lewing@novell.com>
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * See COPYING for license information.
 *
 */

using System;
using System.Runtime.InteropServices;

namespace FSpot {
	public sealed class MemorySurface : Cairo.Surface {
		static class NativeMethods
		{
			[DllImport ("libfspot")]
			public static extern IntPtr f_image_surface_create (Cairo.Format format, int width, int height);

			[DllImport ("libfspot")]
			public static extern IntPtr f_image_surface_get_data (IntPtr surface);

			[DllImport ("libfspot")]
			public static extern Cairo.Format f_image_surface_get_format (IntPtr surface);

			[DllImport ("libfspot")]
			public static extern int f_image_surface_get_width (IntPtr surface);

			[DllImport ("libfspot")]
			public static extern int f_image_surface_get_height (IntPtr surface);

			[DllImport("libfspot")]
			public static extern IntPtr f_pixbuf_to_cairo_surface (IntPtr handle);

			[DllImport("libfspot")]
			public static extern IntPtr f_pixbuf_from_cairo_surface (IntPtr handle);
		}

		public MemorySurface (Cairo.Format format, int width, int height)
			: this (NativeMethods.f_image_surface_create (format, width, height))
		{
		}

		public MemorySurface (IntPtr handle) : base (handle, true)
		{
			if (DataPtr == IntPtr.Zero)
				throw new ApplicationException ("Missing image data");
		}

		public IntPtr DataPtr {
			get { return NativeMethods.f_image_surface_get_data (Handle); }
		}

		public Cairo.Format Format {
			get { return NativeMethods.f_image_surface_get_format (Handle); }
		}

		public int Width {
			get { return NativeMethods.f_image_surface_get_width (Handle); }
		}

		public int Height {
			get { return NativeMethods.f_image_surface_get_height (Handle); }
		}

		public static MemorySurface CreateSurface (Gdk.Pixbuf pixbuf)
		{
			IntPtr surface = NativeMethods.f_pixbuf_to_cairo_surface (pixbuf.Handle);
			return new MemorySurface (surface);
		}

		public static Gdk.Pixbuf CreatePixbuf (MemorySurface mem)
		{
			IntPtr result = NativeMethods.f_pixbuf_from_cairo_surface (mem.Handle);
			return (Gdk.Pixbuf) GLib.Object.GetObject (result, true);
		}
	}
}
