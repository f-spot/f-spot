//
// DragDropList.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Gtk;

namespace Hyena.Gui
{
    public class DragDropList<T> : List<T>
    {
        public DragDropList() : base()
        {
        }

        public DragDropList(T o) : base()
        {
            Add(o);
        }

        public DragDropList(T o, Gtk.SelectionData selectionData, Gdk.Atom target) : base()
        {
            Add(o);
            AssignToSelection(selectionData, target);
        }

        public void AssignToSelection(Gtk.SelectionData selectionData, Gdk.Atom target)
        {
            byte [] data = this;
            selectionData.Set(target, 8, data, data.Length);
        }

        public static implicit operator byte [](DragDropList<T> transferrable)
        {
            IntPtr handle = (IntPtr)GCHandle.Alloc(transferrable);
            return System.Text.Encoding.ASCII.GetBytes(Convert.ToString(handle));
        }

        public static implicit operator DragDropList<T>(byte [] transferrable)
        {
            try {
                string str_handle = System.Text.Encoding.ASCII.GetString(transferrable);
                IntPtr handle_ptr = (IntPtr)Convert.ToInt64(str_handle);
                GCHandle handle = (GCHandle)handle_ptr;
                DragDropList<T> o = (DragDropList<T>)handle.Target;
                handle.Free();
                return o;
            } catch {
                return null;
            }
        }

        public static implicit operator DragDropList<T>(Gtk.SelectionData transferrable)
        {
            return transferrable.Data;
        }
    }
}
