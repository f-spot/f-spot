//
// CompositeUtils.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using Gdk;

using Gtk;

namespace Hyena.Gui
{
	public static class CompositeUtils
	{
		[DllImport ("libgdk-win32-2.0-0.dll")]
		static extern IntPtr gdk_screen_get_rgba_visual (IntPtr screen);

		[DllImport ("libgtk-win32-2.0-0.dll")]
		static extern void gtk_widget_input_shape_combine_mask (IntPtr raw, IntPtr shape_mask,
			int offset_x, int offset_y);

		[DllImport ("libgdk-win32-2.0-0.dll")]
		static extern IntPtr gdk_screen_get_rgba_colormap (IntPtr screen);

		public static Colormap GetRgbaColormap (Screen screen)
		{
			try {
				IntPtr raw_ret = gdk_screen_get_rgba_colormap (screen.Handle);
				var ret = GLib.Object.GetObject (raw_ret) as Gdk.Colormap;
				return ret;
			} catch {
				var visual = Gdk.Visual.GetBestWithDepth (32);
				if (visual != null) {
					var cmap = new Gdk.Colormap (visual, false);
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
				var ret = GLib.Object.GetObject (raw_ret) as Gdk.Visual;
				return ret;
			} catch {
				var visual = Gdk.Visual.GetBestWithDepth (32);
				if (visual != null) {
					return visual;
				}
			}
			return null;
		}

		[DllImport ("libgdk-win32-2.0-0.dll")]
		static extern void gdk_property_change (IntPtr window, IntPtr property, IntPtr type,
			int format, int mode, uint[] data, int nelements);

		[DllImport ("libgdk-win32-2.0-0.dll")]
		static extern void gdk_property_change (IntPtr window, IntPtr property, IntPtr type,
			int format, int mode, byte[] data, int nelements);

		public static void ChangeProperty (Gdk.Window win, Atom property, Atom type, PropMode mode, uint[] data)
		{
			gdk_property_change (win.Handle, property.Handle, type.Handle, 32, (int)mode, data, data.Length * 4);
		}

		public static void ChangeProperty (Gdk.Window win, Atom property, Atom type, PropMode mode, byte[] data)
		{
			gdk_property_change (win.Handle, property.Handle, type.Handle, 8, (int)mode, data, data.Length);
		}

		[DllImport ("libgdk-win32-2.0-0.dll")]
		static extern bool gdk_x11_screen_supports_net_wm_hint (IntPtr screen, IntPtr property);

		public static bool SupportsHint (Screen screen, string name)
		{
			try {
				var atom = Atom.Intern (name, false);
				return gdk_x11_screen_supports_net_wm_hint (screen.Handle, atom.Handle);
			} catch {
				return false;
			}
		}

		[DllImport ("libgdk-win32-2.0-0.dll")]
		static extern bool gdk_screen_is_composited (IntPtr screen);

		public static bool IsComposited (Screen screen)
		{
			bool composited;
			try {
				composited = gdk_screen_is_composited (screen.Handle);
			} catch (EntryPointNotFoundException) {
				var atom = Atom.Intern (string.Format ("_NET_WM_CM_S{0}", screen.Number), false);
				composited = Gdk.Selection.OwnerGetForDisplay (screen.Display, atom) != null;
			}

			// FIXME check for WINDOW_OPACITY so that we support compositing on older composite manager
			// versions before they started supporting the real check given above
			if (!composited) {
				composited = CompositeUtils.SupportsHint (screen, "_NET_WM_WINDOW_OPACITY");
			}

			return composited;
		}

		public static void SetWinOpacity (Gtk.Window win, double opacity)
		{
			CompositeUtils.ChangeProperty (win.GdkWindow,
				Atom.Intern ("_NET_WM_WINDOW_OPACITY", false),
				Atom.Intern ("CARDINAL", false),
				PropMode.Replace,
				new uint[] { (uint)(0xffffffff * opacity) }
			);
		}

		public static void InputShapeCombineMask (Widget w, Pixmap shape_mask, int offset_x, int offset_y)
		{
			gtk_widget_input_shape_combine_mask (w.Handle, shape_mask == null ? IntPtr.Zero : shape_mask.Handle,
				offset_x, offset_y);
		}
	}
}
