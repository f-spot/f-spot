//
// GtkUtilities.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2007-2010 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Gtk;

namespace Hyena.Gui
{
	public delegate void WidgetAction<T> (T widget) where T : class;

	public static class GtkUtilities
	{
		static Gdk.ModifierType[] important_modifiers = new Gdk.ModifierType[] {
			Gdk.ModifierType.ControlMask,
			Gdk.ModifierType.ShiftMask
		};

		public static bool NoImportantModifiersAreSet ()
		{
			return NoImportantModifiersAreSet (important_modifiers);
		}

		public static bool NoImportantModifiersAreSet (params Gdk.ModifierType[] modifiers)
		{
			Gdk.ModifierType state;

			if (Global.CurrentEvent is Gdk.EventKey) {
				state = ((Gdk.EventKey)Global.CurrentEvent).State;
			} else if (Global.CurrentEvent is Gdk.EventButton) {
				state = ((Gdk.EventButton)Global.CurrentEvent).State;
			} else {
				return false;
			}

			foreach (Gdk.ModifierType modifier in modifiers) {
				if ((state & modifier) == modifier) {
					return false;
				}
			}

			return true;
		}

		public static FileFilter GetFileFilter (string name, System.Collections.Generic.IEnumerable<string> extensions)
		{
			var filter = new FileFilter ();
			filter.Name = name;
			foreach (string extension in extensions) {
				filter.AddPattern (string.Format ("*.{0}", extension.ToLower ()));
				filter.AddPattern (string.Format ("*.{0}", extension.ToUpper ()));
			}
			return filter;
		}

		public static void SetChooserShortcuts (Gtk.FileChooserDialog chooser, params string[] shortcuts)
		{
			foreach (string shortcut in shortcuts) {
				if (shortcut != null) {
					try {
						chooser.AddShortcutFolder (shortcut);
					} catch { }
				}
			}
		}

		public static Gdk.Color ColorBlend (Gdk.Color a, Gdk.Color b)
		{
			// at some point, might be nice to allow any blend?
			double blend = 0.5;

			if (blend < 0.0 || blend > 1.0) {
				throw new ApplicationException ("blend < 0.0 || blend > 1.0");
			}

			double blendRatio = 1.0 - blend;

			int aR = a.Red >> 8;
			int aG = a.Green >> 8;
			int aB = a.Blue >> 8;

			int bR = b.Red >> 8;
			int bG = b.Green >> 8;
			int bB = b.Blue >> 8;

			double mR = aR + bR;
			double mG = aG + bG;
			double mB = aB + bB;

			double blR = mR * blendRatio;
			double blG = mG * blendRatio;
			double blB = mB * blendRatio;

			var color = new Gdk.Color ((byte)blR, (byte)blG, (byte)blB);
			Gdk.Colormap.System.AllocColor (ref color, true, true);
			return color;
		}

		public static void AdaptGtkRcStyle (Widget adaptee, Type adapter)
		{
			var type = (GLib.GType)adapter;
			string path = $"*.{type}";
			AdaptGtkRcStyle (adaptee, type, path, path);
		}

		public static void AdaptGtkRcStyle (Widget adaptee, GLib.GType adapter, string widgetPath, string classPath)
		{
			Style style = Gtk.Rc.GetStyleByPaths (adaptee.Settings, widgetPath, classPath, adapter);
			if (style == null) {
				return;
			}

			foreach (StateType state in Enum.GetValues (typeof (StateType))) {
				adaptee.ModifyBase (state, style.Base (state));
				adaptee.ModifyBg (state, style.Background (state));
				adaptee.ModifyFg (state, style.Foreground (state));
				adaptee.ModifyText (state, style.Text (state));
			}
		}

		public static T StyleGetProperty<T> (Widget widget, string property, T default_value)
		{
			object result = null;
			try {
				result = widget.StyleGetProperty (property);
			} catch { }
			return result != null && result.GetType () == typeof (T) ? (T)result : default_value;
		}

		public static void ForeachWidget<T> (Container container, WidgetAction<T> action) where T : class
		{
			if (container == null) {
				return;
			}

			foreach (Widget child in container.Children) {
				var widget = child as T;
				if (widget != null) {
					action (widget);
				} else {
					var child_container = child as Container;
					if (child_container != null) {
						ForeachWidget<T> (child_container, action);
					}
				}
			}
		}

		public static bool ShowUri (string uri)
		{
			return ShowUri (null, uri);
		}

		public static bool ShowUri (Gdk.Screen screen, string uri)
		{
			return ShowUri (screen, uri, Gtk.Global.CurrentEventTime);
		}

		[System.Runtime.InteropServices.DllImport ("libgtk-win32-2.0-0.dll")]
		static unsafe extern bool gtk_show_uri (IntPtr screen, IntPtr uri, uint timestamp, out IntPtr error);

		public static bool ShowUri (Gdk.Screen screen, string uri, uint timestamp)
		{
			var native_uri = GLib.Marshaller.StringToPtrGStrdup (uri);
			var native_error = IntPtr.Zero;

			try {
				return gtk_show_uri (screen == null ? IntPtr.Zero : screen.Handle,
					native_uri, timestamp, out native_error);
			} finally {
				GLib.Marshaller.Free (native_uri);
				if (native_error != IntPtr.Zero) {
					throw new GLib.GException (native_error);
				}
			}
		}
	}
}
