using System;
using System.Runtime.InteropServices;
using Gdk;
using Gtk;

namespace FSpot.Widgets {
	public class CompositeUtils {
		[DllImport("libgdk-2.0-0.dll")]
	        static extern bool gdk_screen_is_composited (IntPtr screen);
		
		[DllImport("libgdk-2.0-0.dll")]
		static extern bool gdk_x11_screen_supports_net_wm_hint (IntPtr screen,
									IntPtr property);

		[DllImport("libgdk-2.0-0.dll")]
		static extern IntPtr gdk_x11_get_xatom_by_name_for_display (IntPtr display, string name);

		[DllImport("libgdk-2.0-0.dll")]
		static extern IntPtr gdk_screen_get_rgba_colormap (IntPtr screen);

		[DllImport("libgdk-2.0-0.dll")]
		static extern IntPtr gdk_screen_get_rgba_visual (IntPtr screen);

		[DllImport ("libgtk-win32-2.0-0.dll")]
		static extern void gtk_widget_input_shape_combine_mask (IntPtr raw, IntPtr shape_mask, int offset_x, int offset_y);

		[DllImport("libgdk-2.0-0.dll")]
		static extern void gdk_property_change(IntPtr window, IntPtr property, IntPtr type, int format, int mode, uint [] data, int nelements);
		
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

		public static void  ChangeProperty (Gdk.Window win, Atom property, Atom type, PropMode mode, uint [] data)
		{
			gdk_property_change (win.Handle, property.Handle, type.Handle, 32, (int)mode,  data, data.Length * 4);
		}

		public static bool SupportsHint (Screen screen, string name)
		{
			try {
				Atom atom = Atom.Intern (name, false);
				return gdk_x11_screen_supports_net_wm_hint (screen.Handle, atom.Handle);
			} catch {
				
				return false;
			}
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

		[DllImport("libXcomposite.dll")]
		static extern void XCompositeRedirectWindow (IntPtr display, uint window, CompositeRedirect update);

		public enum CompositeRedirect {
			Automatic = 0,
			Manual = 1
		};

		public static void RedirectDrawable (Drawable d)
		{
			uint xid = GdkUtils.GetXid (d);
			Console.WriteLine ("xid = {0} d.handle = {1}, d.Display.Handle = {2}", xid, d.Handle, d.Display.Handle);
			XCompositeRedirectWindow (GdkUtils.GetXDisplay (d.Display), GdkUtils.GetXid (d), CompositeRedirect.Manual);
		}
	}
}
