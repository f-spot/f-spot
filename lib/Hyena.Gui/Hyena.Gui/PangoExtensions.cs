//
// PangoExtensions.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Runtime.InteropServices;

using Pango;

namespace Hyena.Gui
{
	public static class PangoExtensions
	{
		public static int MeasureTextHeight (this FontDescription description, Context context)
		{
			return MeasureTextHeight (description, context, context.Language);
		}

		public static int MeasureTextHeight (this FontDescription description, Context context, Language language)
		{
			using (var metrics = context.GetMetrics (description, language)) {
				return ((int)(metrics.Ascent + metrics.Descent) + 512) >> 10; // PANGO_PIXELS (d)
			}
		}

		[DllImport ("libpango-1.0-0.dll")]
		static extern int pango_layout_get_height (IntPtr raw);
		public static int GetHeight (this Pango.Layout layout)
		{
			int raw_ret = pango_layout_get_height (layout.Handle);
			int ret = raw_ret;
			return ret;
		}

		[DllImport ("libpango-1.0-0.dll")]
		static extern void pango_layout_set_height (IntPtr raw, int height);
		public static void SetHeight (this Pango.Layout layout, int height)
		{
			pango_layout_set_height (layout.Handle, height);
		}

		public static string FormatEscaped (this string format, params object[] args)
		{
			return string.Format (format, args.Select (a => a == null ? "" : GLib.Markup.EscapeText (a.ToString ())).ToArray ());
		}
	}
}
