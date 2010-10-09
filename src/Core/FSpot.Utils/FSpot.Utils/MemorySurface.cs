//
// MemorySurface.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
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
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Runtime.InteropServices;

namespace FSpot
{
    public sealed class MemorySurface : Cairo.Surface
    {
        static class NativeMethods
        {
            [DllImport("libfspot")]
            public static extern IntPtr f_image_surface_create (Cairo.Format format, int width, int height);

            [DllImport("libfspot")]
            public static extern IntPtr f_image_surface_get_data (IntPtr surface);

            [DllImport("libfspot")]
            public static extern Cairo.Format f_image_surface_get_format (IntPtr surface);

            [DllImport("libfspot")]
            public static extern int f_image_surface_get_width (IntPtr surface);

            [DllImport("libfspot")]
            public static extern int f_image_surface_get_height (IntPtr surface);

            [DllImport("libfspot")]
            public static extern IntPtr f_pixbuf_to_cairo_surface (IntPtr handle);

            [DllImport("libfspot")]
            public static extern IntPtr f_pixbuf_from_cairo_surface (IntPtr handle);
        }

        public MemorySurface (Cairo.Format format, int width, int height)
            : this (NativeMethods.f_image_surface_create (format, width, height))
        {
        }

        public MemorySurface (IntPtr handle) : base(handle, true)
        {
            if (DataPtr == IntPtr.Zero)
                throw new ApplicationException ("Missing image data");
        }

        public IntPtr DataPtr {
            get { return NativeMethods.f_image_surface_get_data (Handle); }
        }

        public Cairo.Format Format {
            get { return NativeMethods.f_image_surface_get_format (Handle); }
        }

        public int Width {
            get { return NativeMethods.f_image_surface_get_width (Handle); }
        }

        public int Height {
            get { return NativeMethods.f_image_surface_get_height (Handle); }
        }

        public static MemorySurface CreateSurface (Gdk.Pixbuf pixbuf)
        {
            IntPtr surface = NativeMethods.f_pixbuf_to_cairo_surface (pixbuf.Handle);
            return new MemorySurface (surface);
        }

        public static Gdk.Pixbuf CreatePixbuf (MemorySurface mem)
        {
            IntPtr result = NativeMethods.f_pixbuf_from_cairo_surface (mem.Handle);
            return (Gdk.Pixbuf)GLib.Object.GetObject (result, true);
        }
    }
}
