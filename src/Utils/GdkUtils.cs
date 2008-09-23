using System;
using Gdk;
using System.Runtime.InteropServices;

namespace FSpot.Utils {
	public class GdkUtils {
		public static Pixbuf Deserialize (byte [] data)
		{
			Pixdata pixdata = new Pixdata ();
	
			pixdata.Deserialize ((uint) data.Length, data);
	
			return Pixbuf.FromPixdata (pixdata, true);
		}
	
		public static byte [] Serialize (Pixbuf pixbuf)
		{
			Pixdata pixdata = new Pixdata ();
	
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
			[DllImport("libgdk-2.0-0.dll")]
			public static extern uint gdk_x11_drawable_get_xid (IntPtr d);
	
			[DllImport("libgdk-2.0-0.dll")]
			public static extern IntPtr gdk_x11_display_get_xdisplay (IntPtr d);
	
			[DllImport("libgdk-2.0-0.dll")]
			public static extern IntPtr gdk_x11_visual_get_xvisual (IntPtr d);
	
			[DllImport("X11")]
			public static extern uint XVisualIDFromVisual(IntPtr visual);
	
			[DllImport("libgdk-2.0-0.dll")]
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
			} catch (System.Exception e){
				Log.Exception (e);
				return null;
			}
		}
	}
}
