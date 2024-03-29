//
// TooltipSetter.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

using Gtk;

namespace Hyena.Gui
{
	public static class TooltipSetter
	{
		static Type host_type;
		static MethodInfo host_set_tip_method;
		static PropertyInfo tooltip_text_property;
		static bool reflected;

		public static object CreateHost ()
		{
			if (tooltip_text_property != null) {
				return null;
			}

			Type type = reflected ? null : typeof (Widget);

			if (type != null) {
				tooltip_text_property = type.GetProperty ("TooltipText", BindingFlags.Instance | BindingFlags.Public);
				if (tooltip_text_property != null) {
					reflected = true;
					return null;
				}
			}

			if (host_set_tip_method == null && !reflected) {
				reflected = true;
				host_type = Type.GetType (string.Format ("Gtk.Tooltips, {0}", type.Assembly.FullName));
				if (type == null) {
					return null;
				}

				host_set_tip_method = host_type.GetMethod ("SetTip", BindingFlags.Instance |
					BindingFlags.Public | BindingFlags.InvokeMethod);
				if (host_set_tip_method == null) {
					return null;
				}
			}

			return host_set_tip_method != null ? Activator.CreateInstance (host_type) : null;
		}

		public static void Set (object host, Widget widget, string textTip)
		{
			if (tooltip_text_property != null) {
				tooltip_text_property.SetValue (widget, textTip, null);
			} else if (host != null && host_set_tip_method != null) {
				host_set_tip_method.Invoke (host, new object[] { widget, textTip, null });
			} else {
				throw new ApplicationException ("You must call TooltipSetter.CreateHost before calling TooltipSetter.Set");
			}
		}
	}
}
