// Gtk.Style.cs
//
// Author(s):
//      Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (c) 2009 Novell, Inc.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of version 2 of the Lesser GNU General 
// Public License as published by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this program; if not, write to the
// Free Software Foundation, Inc., 59 Temple Place - Suite 330,
// Boston, MA 02111-1307, USA.

using System;
using System.Collections;
using System.Runtime.InteropServices;
using Gtk;

namespace GtkBeans {
	public class Style {
               [DllImport("libgtk-win32-2.0-0.dll")]
                static extern void gtk_paint_flat_box(IntPtr style, IntPtr window, int state_type, int shadow_type, IntPtr area, IntPtr widget, IntPtr detail, int x, int y, int width, int height);

                public static void PaintFlatBox(Gtk.Style style, Gdk.Drawable window, Gtk.StateType state_type, Gtk.ShadowType shadow_type, Gdk.Rectangle? area, Gtk.Widget widget, string detail, int x, int y, int width, int height) {
                        IntPtr native_area = area == null ? IntPtr.Zero : GLib.Marshaller.StructureToPtrAlloc (area);
                        IntPtr native_detail = GLib.Marshaller.StringToPtrGStrdup (detail);
                        gtk_paint_flat_box(style == null ? IntPtr.Zero : style.Handle, window == null ? IntPtr.Zero : window.Handle, (int) state_type, (int) shadow_type, native_area, widget == null ? IntPtr.Zero : widget.Handle, native_detail, x, y, width, height);
                        if (area != null) {
				area = Gdk.Rectangle.New (native_area);
	                        Marshal.FreeHGlobal (native_area);
			}
                        GLib.Marshaller.Free (native_detail);
                }
	}
}
	
