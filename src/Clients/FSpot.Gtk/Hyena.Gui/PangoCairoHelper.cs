//
// CairoHelper.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Hyena.Gui
{
	public static class PangoCairoHelper
	{
		[DllImport ("libpangocairo-1.0-0.dll")]
		static extern void pango_cairo_show_layout (IntPtr cr, IntPtr layout);

		public static void ShowLayout (Cairo.Context cr, Pango.Layout layout)
		{
			pango_cairo_show_layout (cr == null ? IntPtr.Zero : cr.Handle,
				layout == null ? IntPtr.Zero : layout.Handle);
		}

		[DllImport ("libpangocairo-1.0-0.dll")]
		static extern IntPtr pango_cairo_create_layout (IntPtr cr);

		public static Pango.Layout CreateLayout (Cairo.Context cr)
		{
			IntPtr raw_ret = pango_cairo_create_layout (cr == null ? IntPtr.Zero : cr.Handle);
			return GLib.Object.GetObject (raw_ret) as Pango.Layout;
		}

		[DllImport ("libpangocairo-1.0-0.dll")]
		static extern void pango_cairo_layout_path (IntPtr cr, IntPtr layout);

		public static void LayoutPath (Cairo.Context cr, Pango.Layout layout,
			bool iUnderstandThePerformanceImplications)
		{
			pango_cairo_layout_path (cr == null ? IntPtr.Zero : cr.Handle,
				layout == null ? IntPtr.Zero : layout.Handle);
		}

		[DllImport ("libpangocairo-1.0-0.dll")]
		static extern void pango_cairo_context_set_resolution (IntPtr pango_context, double dpi);

		public static void ContextSetResolution (Pango.Context context, double dpi)
		{
			pango_cairo_context_set_resolution (context == null ? IntPtr.Zero : context.Handle, dpi);
		}

		[DllImport ("libpangocairo-1.0-0.dll")]
		static extern IntPtr pango_layout_get_context (IntPtr layout);

		public static Pango.Context LayoutGetContext (Pango.Layout layout)
		{
			IntPtr handle = pango_layout_get_context (layout.Handle);
			return handle.Equals (IntPtr.Zero) ? null : GLib.Object.GetObject (handle) as Pango.Context;
		}
	}
}
