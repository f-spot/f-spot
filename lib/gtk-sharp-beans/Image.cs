// Gtk.Image.cs
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
	public class Image {
		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_image_new_from_gicon(IntPtr icon, int size);

		public static Gtk.Image NewFromIcon (GLib.Icon icon, Gtk.IconSize size)
		{
			return new Gtk.Image (gtk_image_new_from_gicon(icon == null ? IntPtr.Zero : icon.Handle, (int) size));
		}
	}
}
	
