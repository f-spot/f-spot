//
// ThemeEngine.cs
//
// Author:
//     Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Aaron Bockover
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;

namespace Hyena.Gui.Theming
{
	public static class ThemeEngine
	{
		static Type theme_type;

		public static void SetCurrentTheme<T> () where T : Theme
		{
			theme_type = typeof (T);
		}

		public static Theme CreateTheme (Gtk.Widget widget)
		{
			return theme_type == null
				? new GtkTheme (widget)
				: (Theme)Activator.CreateInstance (theme_type, new object[] { widget });
		}
	}
}