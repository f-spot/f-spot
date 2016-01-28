// Gtk.Widget.cs
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

namespace Gtk {
	public static class WidgetExtensions {
		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_widget_get_snapshot(IntPtr raw, IntPtr clip_rect);

		public static Gdk.Pixmap GetSnapshot(this Widget widget, Gdk.Rectangle clip_rect) {
			IntPtr native_clip_rect = GLib.Marshaller.StructureToPtrAlloc (clip_rect);
			IntPtr raw_ret = gtk_widget_get_snapshot(widget.Handle, native_clip_rect);
			Gdk.Pixmap ret = GLib.Object.GetObject(raw_ret) as Gdk.Pixmap;
			clip_rect = Gdk.Rectangle.New (native_clip_rect);
			Marshal.FreeHGlobal (native_clip_rect);
			return ret;
		}
	}
}
