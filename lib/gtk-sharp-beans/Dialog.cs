// Gtk.Dialog.cs
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
	public static class DialogExtensions {
		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_dialog_get_action_area (IntPtr raw);

		public static HButtonBox GetActionArea (this Dialog dialog) {
			IntPtr raw_ret = gtk_dialog_get_action_area (dialog.Handle);
			Gtk.HButtonBox ret = GLib.Object.GetObject (raw_ret) as Gtk.HButtonBox;
			return ret;
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_dialog_get_content_area (IntPtr raw);

		public static Widget GetContentArea (this Dialog dialog) { 
			IntPtr raw_ret = gtk_dialog_get_content_area (dialog.Handle);
			Gtk.Widget ret = GLib.Object.GetObject (raw_ret) as Gtk.Widget;
			return ret;
		}
	}
}
