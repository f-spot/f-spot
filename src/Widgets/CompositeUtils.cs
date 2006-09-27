using System;
using System.Runtime.InteropServices;
using Gdk;
using Gtk;

namespace FSpot.Widgets {
	public class CompositeUtils {
		[DllImport("libgdk-2.0-0.dll")]
	        static extern bool gdk_screen_is_composited (IntPtr screen);
		
		[DllImport("libgdk-2.0-0.dll")]
		static extern IntPtr gdk_screen_get_rgba_colormap (IntPtr screen);

		[DllImport("libgdk-2.0-0.dll")]
		static extern IntPtr gdk_screen_get_rgba_visual (IntPtr screen);


		[DllImport ("libgtk-win32-2.0-0.dll")]
		static extern void gtk_widget_input_shape_combine_mask (IntPtr raw, IntPtr shape_mask, int offset_x, int offset_y);



		public static Colormap GetRgbaColormap (Screen screen)
		{
			try {
				IntPtr raw_ret = gdk_screen_get_rgba_colormap (screen.Handle);
				Gdk.Colormap ret = GLib.Object.GetObject(raw_ret) as Gdk.Colormap;
				return ret;
			} catch {
				Gdk.Visual visual = Gdk.Visual.GetBestWithDepth (32);
				if (visual != null) {
					Gdk.Colormap cmap = new Gdk.Colormap (visual, false);
					System.Console.WriteLine ("fallback");
					return cmap;
				}
			}
			return null;
		}

		public static bool SetRgbaColormap (Widget w)
		{
			Gdk.Colormap cmap = GetRgbaColormap (w.Screen);

			if (cmap != null) {
				w.Colormap = cmap;
				return true;
			}

			return false;
		}


		public static Visual GetRgbaVisual (Screen screen)
		{
			try {
				IntPtr raw_ret = gdk_screen_get_rgba_visual (screen.Handle);
				Gdk.Visual ret = GLib.Object.GetObject(raw_ret) as Gdk.Visual;
				return ret;
			} catch {
				Gdk.Visual visual = Gdk.Visual.GetBestWithDepth (32);
				if (visual != null) {
					return visual;
				}
			}
			return null;
		}

		public static bool IsComposited (Screen screen) {
#if false
				try {
					return gdk_screen_is_composited (Screen.Handle);
				} catch {
					//System.Console.WriteLine ("unable to query composite manager");
				}
				return false;
#else
				return true;

#endif
		}

		public static void InputShapeCombineMask (Widget w, Pixmap shape_mask, int offset_x, int offset_y)
		{
			gtk_widget_input_shape_combine_mask (w.Handle, shape_mask == null ? IntPtr.Zero : shape_mask.Handle, offset_x, offset_y);
		}

		[DllImport("libgdk-2.0-0.dll")]
		static extern uint gdk_x11_drawable_get_xid (IntPtr d);

		[DllImport("libgdk-2.0-0.dll")]
		static extern IntPtr gdk_x11_display_get_xdisplay (IntPtr d);

		[DllImport("libXcomposite.dll")]
		static extern void XCompositeRedirectWindow (IntPtr display, uint window, CompositeRedirect update);


		public enum CompositeRedirect {
			Automatic = 0,
			Manual = 1
		};

		public static void RedirectDrawable (Drawable d)
		{
			uint xid = gdk_x11_drawable_get_xid (d.Handle);
			Console.WriteLine ("xid = {0} d.handle = {1}, d.Display.Handle = {2}", xid, d.Handle, d.Display.Handle);
			XCompositeRedirectWindow (gdk_x11_display_get_xdisplay (d.Display.Handle), xid, CompositeRedirect.Manual);
		}
	}
}
