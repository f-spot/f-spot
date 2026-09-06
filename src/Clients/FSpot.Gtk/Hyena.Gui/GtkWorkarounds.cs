//
// GtkWorkarounds.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Hyena.Gui
{
	public static class GtkWorkarounds
	{
		static bool toggle_ref_supported;
		static MethodInfo g_object_ref;
		static MethodInfo gdk_window_destroy;
		static object[] invoke_args;

		static GtkWorkarounds ()
		{
			if (!(toggle_ref_supported = Assembly.GetAssembly (typeof (GLib.Object)).GetType (
				"GLib.ToggleRef") != null)) {
				return;
			}

			// Find the P/Invoke signatures we need so we can avoid a dllmap
			g_object_ref = typeof (GLib.Object).GetMethod ("g_object_ref",
				BindingFlags.NonPublic | BindingFlags.Static);

			gdk_window_destroy = typeof (Gdk.Window).GetMethod ("gdk_window_destroy",
				BindingFlags.NonPublic | BindingFlags.Static);
		}

		public static void WindowDestroy (Gdk.Window window)
		{
			// There is a bug in GDK, and subsequently in Gdk# 2.8.5 through 2.12.1
			// where the managed Gdk.Window.Destroy function does not obtain a
			// normal reference (non-toggle) on the GdkWindow before calling
			// _destroy on it, which the native function apparently expects.

			// https://bugzilla.novell.com/show_bug.cgi?id=382186
			// http://anonsvn.mono-project.com/viewcvs/trunk/gtk-sharp/gdk/Window.custom?rev=101734&r1=42529&r2=101734

			if (window == null) {
				return;
			}

			if (!toggle_ref_supported) {
				window.Destroy ();
				return;
			}

			// If this ever happens I will move out west and start farming...
			if (g_object_ref == null || gdk_window_destroy == null) {
				window.Destroy ();
				return;
			}

			if (invoke_args == null) {
				invoke_args = new object[1];
			}

			invoke_args[0] = window.Handle;
			g_object_ref.Invoke (null, invoke_args);
			gdk_window_destroy.Invoke (null, invoke_args);

			window.Dispose ();
		}
	}
}
