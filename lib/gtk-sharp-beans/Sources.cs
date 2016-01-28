// GLib.SourceExtensions
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
using System.Runtime.InteropServices;
using GLib;

namespace GLibBeans {
	public class Sources {
		[DllImport("libglib-2.0-0.dll")]
		static extern void g_source_set_priority (IntPtr source, int priority);

		[DllImport("libglib-2.0-0.dll")]
		static extern IntPtr g_main_context_find_source_by_id (IntPtr context, uint source_id);

		public static void SetPriority (uint source_id, Priority priority)
		{
			g_source_set_priority (g_main_context_find_source_by_id (IntPtr.Zero, source_id), (int)priority);
		}
	}
}
