using System;
using Gdk;
using System.Runtime.InteropServices;

namespace FSpot.Widgets {
	public class GdkUtils {

		[DllImport("libgdk-2.0-0.dll")]
		static extern uint gdk_x11_drawable_get_xid (IntPtr d);

		[DllImport("libgdk-2.0-0.dll")]
		static extern IntPtr gdk_x11_display_get_xdisplay (IntPtr d);


		[DllImport("libgdk-2.0-0.dll")]
		static extern IntPtr gdk_x11_visual_get_xvisual (IntPtr d);

		[DllImport("X11")]
		internal static extern uint XVisualIDFromVisual(IntPtr visual);

		public static uint GetXid (Drawable d)
		{
			return gdk_x11_drawable_get_xid (d.Handle);
		}

		public static uint GetXVisualId (Visual visual)
		{
			return XVisualIDFromVisual (GetXVisual (visual));
		}
		
		public static IntPtr GetXDisplay (Display display)
		{
			return gdk_x11_display_get_xdisplay (display.Handle);
		}

		public static IntPtr GetXVisual (Visual v)
		{
			return gdk_x11_visual_get_xvisual (v.Handle);
		}
	}
}
