//
// DragDropList.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Hyena.Gui
{
	public class DragDropList<T> : List<T>
	{
		public DragDropList () : base ()
		{
		}

		public DragDropList (T o) : base ()
		{
			Add (o);
		}

		public DragDropList (T o, Gtk.SelectionData selectionData, Gdk.Atom target) : base ()
		{
			Add (o);
			AssignToSelection (selectionData, target);
		}

		public void AssignToSelection (Gtk.SelectionData selectionData, Gdk.Atom target)
		{
			byte[] data = this;
			selectionData.Set (target, 8, data, data.Length);
		}

		public static implicit operator byte[] (DragDropList<T> transferrable)
		{
			var handle = (IntPtr)GCHandle.Alloc (transferrable);
			return System.Text.Encoding.ASCII.GetBytes (Convert.ToString (handle));
		}

		public static implicit operator DragDropList<T> (byte[] transferrable)
		{
			try {
				string str_handle = System.Text.Encoding.ASCII.GetString (transferrable);
				var handle_ptr = (IntPtr)Convert.ToInt64 (str_handle);
				var handle = (GCHandle)handle_ptr;
				var o = (DragDropList<T>)handle.Target;
				handle.Free ();
				return o;
			} catch {
				return null;
			}
		}

		public static implicit operator DragDropList<T> (Gtk.SelectionData transferrable)
		{
			return transferrable.Data;
		}
	}
}
