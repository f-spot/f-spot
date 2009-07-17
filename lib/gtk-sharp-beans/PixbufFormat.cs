// Gdk.PixbufFormat.cs
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

namespace Gdk {
	public static class PixbufFormatExtensions {
		[DllImport("libgdk_pixbuf-2.0-0.dll")]
		static extern void gdk_pixbuf_format_set_disabled(IntPtr raw, bool disabled);

		public static void SetDisabled (this PixbufFormat format, bool value) { 
			gdk_pixbuf_format_set_disabled(format.Handle, value);
		}
	}
}	
