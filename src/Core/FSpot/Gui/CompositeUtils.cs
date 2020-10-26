//
// CompositeUtils.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2006 Novell, Inc.
// Copyright (C) 2006 Larry Ewing
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using Gdk;
using Gtk;

using FSpot.Utils;

using Hyena;

namespace FSpot.Gui
{
	public class CompositeUtils
	{
		[DllImport ("libgdk-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern bool gdk_screen_is_composited (IntPtr screen);

		[DllImport ("libgdk-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern bool gdk_x11_screen_supports_net_wm_hint (IntPtr screen, IntPtr property);

		[DllImport ("libgdk-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gdk_screen_get_rgba_colormap (IntPtr screen);

		[DllImport ("libgdk-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gdk_screen_get_rgba_visual (IntPtr screen);

		[DllImport ("libgtk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void gtk_widget_input_shape_combine_mask (IntPtr raw, IntPtr shape_mask, int offset_x, int offset_y);

		[DllImport ("libgdk-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void gdk_property_change (IntPtr window, IntPtr property, IntPtr type, int format, int mode, uint[] data, int nelements);

		[DllImport ("libgdk-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void gdk_property_change (IntPtr window, IntPtr property, IntPtr type, int format, int mode, byte[] data, int nelements);

		public static Colormap GetRgbaColormap (Screen screen)
		{
			try {
				IntPtr raw_ret = gdk_screen_get_rgba_colormap (screen.Handle);
				var ret = GLib.Object.GetObject (raw_ret) as Colormap;
				return ret;
			} catch {
				var visual = Gdk.Visual.GetBestWithDepth (32);
				if (visual != null) {
					var cmap = new Colormap (visual, false);
					Log.Debug ("fallback");
					return cmap;
				}
			}
			return null;
		}

		public static void ChangeProperty (Gdk.Window win, Atom property, Atom type, PropMode mode, uint[] data)
		{
			gdk_property_change (win.Handle, property.Handle, type.Handle, 32, (int)mode, data, data.Length * 4);
		}

		public static void ChangeProperty (Gdk.Window win, Atom property, Atom type, PropMode mode, byte[] data)
		{
			gdk_property_change (win.Handle, property.Handle, type.Handle, 8, (int)mode, data, data.Length);
		}

		public static bool SupportsHint (Screen screen, string name)
		{
			try {
				var atom = Atom.Intern (name, false);
				return gdk_x11_screen_supports_net_wm_hint (screen.Handle, atom.Handle);
			} catch {

				return false;
			}
		}

		public static bool SetRgbaColormap (Widget w)
		{
			Colormap cmap = GetRgbaColormap (w.Screen);

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
				var ret = GLib.Object.GetObject (raw_ret) as Visual;
				return ret;
			} catch {
				var visual = Gdk.Visual.GetBestWithDepth (32);
				if (visual != null) {
					return visual;
				}
			}
			return null;
		}

		public static bool IsComposited (Screen screen)
		{
			bool composited;
			try {
				composited = gdk_screen_is_composited (screen.Handle);
			} catch (EntryPointNotFoundException) {
				Log.Debug ("query composite manager locally");
				var atom = Atom.Intern ($"_NET_WM_CM_S{screen.Number}", false);
				composited = Gdk.Selection.OwnerGetForDisplay (screen.Display, atom) != null;
			}

			// FIXME check for WINDOW_OPACITY so that we support compositing on older composite manager
			// versions before they started supporting the real check given above
			if (!composited)
				composited = CompositeUtils.SupportsHint (screen, "_NET_WM_WINDOW_OPACITY");

			return composited;
		}

		public static void InputShapeCombineMask (Widget w, Pixmap shapeMask, int offsetX, int offsetY)
		{
			gtk_widget_input_shape_combine_mask (w.Handle, shapeMask == null ? IntPtr.Zero : shapeMask.Handle, offsetX, offsetY);
		}

		[DllImport ("libXcomposite.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void XCompositeRedirectWindow (IntPtr display, uint window, CompositeRedirect update);

		public enum CompositeRedirect
		{
			Automatic = 0,
			Manual = 1
		};

		public static void RedirectDrawable (Drawable d)
		{
			uint xid = GdkUtils.GetXid (d);
			Log.Debug ($"xid = {xid} d.handle = {d.Handle}, d.Display.Handle = {d.Display.Handle}");
			XCompositeRedirectWindow (GdkUtils.GetXDisplay (d.Display), GdkUtils.GetXid (d), CompositeRedirect.Manual);
		}

		public static void SetWinOpacity (Gtk.Window win, double opacity)
		{
			CompositeUtils.ChangeProperty (win.GdkWindow,
							   Atom.Intern ("_NET_WM_WINDOW_OPACITY", false),
							   Atom.Intern ("CARDINAL", false),
							   PropMode.Replace,
							   new uint[] { (uint)(0xffffffff * opacity) });
		}

		public static Cms.Profile GetScreenProfile (Screen screen)
		{
			if (Gdk.Property.Get (screen.RootWindow,
						  Atom.Intern ("_ICC_PROFILE", false),
						  Atom.Intern ("CARDINAL", false),
						  0,
						  int.MaxValue,
						  0, // FIXME in gtk# should be a bool
						  out var atype,
						  out var aformat,
						  out var alength,
						  out var data)) {
				return new Cms.Profile (data);
			}

			return null;
		}

		public static void SetScreenProfile (Screen screen, Cms.Profile profile)
		{
			byte[] data = profile.Save ();
			ChangeProperty (screen.RootWindow,
					Atom.Intern ("_ICC_PROFILE", false),
					Atom.Intern ("CARDINAL", false),
					PropMode.Replace,
					data);
		}
	}
}
