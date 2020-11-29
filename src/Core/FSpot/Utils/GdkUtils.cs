//
// GdkUtils.cs
//
// Author:
//   Anton Keks <anton@azib.net>
//   Stephane Delcroix <sdelcroix@novell.com>
//   Larry Ewing <lewing@src.gnome.org>
//
// Copyright (C) 2006-2008 Novell, Inc.
// Copyright (C) 2008 Anton Keks
// Copyright (C) 2008 Stephane Delcroix
// Copyright (C) 2006-2007 Larry Ewing
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using Gdk;

using Hyena;

namespace FSpot.Utils
{
	public class GdkUtils
	{
		public static Pixbuf Deserialize (byte[] data)
		{
			var pixdata = new Pixdata ();

			pixdata.Deserialize ((uint)data.Length, data);

			return Pixbuf.FromPixdata (pixdata, true);
		}

		public static byte[] Serialize (Pixbuf pixbuf)
		{
			var pixdata = new Pixdata ();

#if true   //We should use_rle, but bgo#553374 prevents this
			pixdata.FromPixbuf (pixbuf, false);
			return pixdata.Serialize ();
#else
			IntPtr raw_pixdata = pixdata.FromPixbuf (pixbuf, true); 
			byte [] data = pixdata.Serialize ();
			GLib.Marshaller.Free (raw_pixdata);
				
			return data;
#endif
		}

		public class NativeMethods
		{
			const string LIBGTK = "libgtk-win32-2.0-0.dll";

			[DllImport (LIBGTK, CallingConvention = CallingConvention.Cdecl)]
			public static extern uint gdk_x11_drawable_get_xid (IntPtr d);

			[DllImport (LIBGTK, CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr gdk_x11_display_get_xdisplay (IntPtr d);

			[DllImport (LIBGTK, CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr gdk_x11_visual_get_xvisual (IntPtr d);

			// FIXME: get rid of this? (Make this cross platform)
			[DllImport ("X11", CallingConvention = CallingConvention.Cdecl)]
			public static extern uint XVisualIDFromVisual (IntPtr visual);

			[DllImport (LIBGTK, CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr gdk_x11_screen_lookup_visual (IntPtr screen, uint xvisualid);

			[DllImport (LIBGTK, CallingConvention = CallingConvention.Cdecl)]
			public static extern bool gdk_screen_is_composited (IntPtr screen);

			[DllImport (LIBGTK, CallingConvention = CallingConvention.Cdecl)]
			public static extern bool gdk_x11_screen_supports_net_wm_hint (IntPtr screen, IntPtr property);

			[DllImport (LIBGTK, CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr gdk_screen_get_rgba_colormap (IntPtr screen);

			[DllImport (LIBGTK, CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr gdk_screen_get_rgba_visual (IntPtr screen);

			[DllImport ("libgtk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern void gtk_widget_input_shape_combine_mask (IntPtr raw, IntPtr shape_mask, int offset_x, int offset_y);

			[DllImport (LIBGTK, CallingConvention = CallingConvention.Cdecl)]
			public static extern void gdk_property_change (IntPtr window, IntPtr property, IntPtr type, int format, int mode, uint[] data, int nelements);

			[DllImport (LIBGTK, CallingConvention = CallingConvention.Cdecl)]
			public static extern void gdk_property_change (IntPtr window, IntPtr property, IntPtr type, int format, int mode, byte[] data, int nelements);
		}

		public static uint GetXid (Drawable d)
		{
			return NativeMethods.gdk_x11_drawable_get_xid (d.Handle);
		}

		public static uint GetXVisualId (Visual visual)
		{
			return NativeMethods.XVisualIDFromVisual (GetXVisual (visual));
		}

		public static IntPtr GetXDisplay (Display display)
		{
			return NativeMethods.gdk_x11_display_get_xdisplay (display.Handle);
		}

		public static IntPtr GetXVisual (Visual v)
		{
			return NativeMethods.gdk_x11_visual_get_xvisual (v.Handle);
		}

		public static Visual LookupVisual (Screen screen, uint visualid)
		{
			return (Gdk.Visual)GLib.Object.GetObject (NativeMethods.gdk_x11_screen_lookup_visual (screen.Handle, visualid));
		}

		public static Cursor CreateEmptyCursor (Display display)
		{
			try {
				using var empty = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, true, 8, 1, 1);
				empty.Fill (0x00000000);
				return new Gdk.Cursor (display, empty, 0, 0);
			} catch (Exception e) {
				Log.Exception (e);
				return null;
			}
		}
	}
}
