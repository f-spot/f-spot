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

using Gdk;

using Hyena;

namespace FSpot.Utils
{
	public class GdkUtils
	{
		public static Pixbuf Deserialize (byte [] data)
		{
			var pixdata = new Pixdata ();
	
			pixdata.Deserialize ((uint) data.Length, data);
	
			return Pixbuf.FromPixdata (pixdata, true);
		}
	
		public static byte [] Serialize (Pixbuf pixbuf)
		{
			var pixdata = new Pixdata ();
	
#if true 	//We should use_rle, but bgo#553374 prevents this
			pixdata.FromPixbuf (pixbuf, false);
			return pixdata.Serialize ();
#else
			IntPtr raw_pixdata = pixdata.FromPixbuf (pixbuf, true); 
			byte [] data = pixdata.Serialize ();
			GLib.Marshaller.Free (raw_pixdata);
				
			return data;
#endif
		}

		class NativeMethods
		{
			[DllImport("libgdk-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern uint gdk_x11_drawable_get_xid (IntPtr d);
	
			[DllImport("libgdk-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr gdk_x11_display_get_xdisplay (IntPtr d);
	
			[DllImport("libgdk-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr gdk_x11_visual_get_xvisual (IntPtr d);

			// FIXME: get rid of this? (Make this cross platform)
			[DllImport("X11", CallingConvention = CallingConvention.Cdecl)]
			public static extern uint XVisualIDFromVisual(IntPtr visual);
	
			[DllImport("libgdk-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr gdk_x11_screen_lookup_visual (IntPtr screen,
									   uint   xvisualid);
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
			return (Gdk.Visual) GLib.Object.GetObject (NativeMethods.gdk_x11_screen_lookup_visual (screen.Handle, visualid));
		}
		
		public static Cursor CreateEmptyCursor (Display display) 
		{
			try {
				Gdk.Pixbuf empty = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, true, 8, 1, 1);
				empty.Fill (0x00000000);
				return new Gdk.Cursor (display, empty, 0, 0);
			} catch (Exception e){
				Log.Exception (e);
				return null;
			}
		}
	}
}
