//
// PixbufImageSurface.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2008-2010 Novell, Inc.
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
using System.Runtime.InteropServices;

using Cairo;

namespace Hyena.Gui
{
    public class PixbufImageSurface : ImageSurface, IDisposable
    {
        private delegate void cairo_destroy_func_t (IntPtr userdata);

        private static bool is_le = BitConverter.IsLittleEndian;
        private static int user_data_key = 0;
        private static cairo_destroy_func_t destroy_func;

        private static void DestroyPixelData (IntPtr data)
        {
            Marshal.FreeHGlobal (data);
        }

        static PixbufImageSurface ()
        {
            destroy_func = new cairo_destroy_func_t (DestroyPixelData);
        }

        public static ImageSurface Create (Gdk.Pixbuf pixbuf)
        {
            return Create (pixbuf, false);
        }

        public static ImageSurface Create (Gdk.Pixbuf pixbuf, bool disposePixbuf)
        {
            if (pixbuf == null || pixbuf.Handle == IntPtr.Zero) {
                return null;
            }

            if (!PlatformDetection.IsWindows) {
                try {
                    return new PixbufImageSurface (pixbuf, disposePixbuf);
                } catch {
                    return null;
                }
            } else {
                // FIXME:
                // Windows has some trouble running the PixbufImageSurface, so as a
                // workaround a slower but working version of this factory method is
                // implemented. One day we can come back and optimize this by finding
                // out what's causing the PixbufImageSurface to result in access
                // violations when the object is disposed.
                ImageSurface target = new ImageSurface (Format.ARGB32, pixbuf.Width, pixbuf.Height);
                Context context = new Context (target);
                try {
                    Gdk.CairoHelper.SetSourcePixbuf (context, pixbuf, 0, 0);
                    context.Paint ();
                } finally {
                    ((IDisposable)context).Dispose ();
                    if (disposePixbuf) {
                        ((IDisposable)pixbuf).Dispose ();
                    }
                }

                return target;
            }
        }

        private IntPtr data;

        public PixbufImageSurface (Gdk.Pixbuf pixbuf) : this (pixbuf, false)
        {
        }

        public PixbufImageSurface (Gdk.Pixbuf pixbuf, bool disposePixbuf) : this (disposePixbuf ? pixbuf : null,
            pixbuf.Width, pixbuf.Height, pixbuf.NChannels, pixbuf.Rowstride, pixbuf.Pixels)
        {
        }

        // This ctor is to avoid multiple queries against the GdkPixbuf for width/height
        private PixbufImageSurface (Gdk.Pixbuf pixbuf, int width, int height, int channels, int rowstride, IntPtr pixels)
            : this (pixbuf, Marshal.AllocHGlobal (width * height * 4), width, height, channels, rowstride, pixels)
        {
        }

        private PixbufImageSurface (Gdk.Pixbuf pixbuf, IntPtr data, int width, int height, int channels, int rowstride, IntPtr pixels)
            : base (data, channels == 3 ? Format.Rgb24 : Format.Argb32, width, height, width * 4)
        {
            this.data = data;

            CreateSurface (width, height, channels, rowstride, pixels);
            SetDestroyFunc ();

            if (pixbuf != null && pixbuf.Handle != IntPtr.Zero) {
                pixbuf.Dispose ();
            }
        }

        private unsafe void CreateSurface (int width, int height, int channels, int gdk_rowstride, IntPtr pixels)
        {
            byte *gdk_pixels = (byte *)pixels;
            byte *cairo_pixels = (byte *)data;

            for (int i = height; i > 0; i--) {
                byte *p = gdk_pixels;
                byte *q = cairo_pixels;

                if (channels == 3) {
                    byte *end = p + 3 * width;
                    while (p < end) {
                        if (is_le) {
                            q[0] = p[2];
                            q[1] = p[1];
                            q[2] = p[0];
                        } else {
                            q[1] = p[0];
                            q[2] = p[1];
                            q[3] = p[2];
                        }

                        p += 3;
                        q += 4;
                    }
                } else {
                    byte *end = p + 4 * width;
                    while (p < end) {
                        if (is_le) {
                            q[0] = Mult (p[2], p[3]);
                            q[1] = Mult (p[1], p[3]);
                            q[2] = Mult (p[0], p[3]);
                            q[3] = p[3];
                        } else {
                            q[0] = p[3];
                            q[1] = Mult (p[0], p[3]);
                            q[2] = Mult (p[1], p[3]);
                            q[3] = Mult (p[2], p[3]);
                        }

                        p += 4;
                        q += 4;
                    }
                }

                gdk_pixels += gdk_rowstride;
                cairo_pixels += 4 * width;
            }
        }

        private static byte Mult (byte c, byte a)
        {
            int t = c * a + 0x7f;
            return (byte)(((t >> 8) + t) >> 8);
        }

        [DllImport ("libcairo-2.dll")]
        private static extern Cairo.Status cairo_surface_set_user_data (IntPtr surface,
            ref int key, IntPtr userdata, cairo_destroy_func_t destroy);

        private void SetDestroyFunc ()
        {
            try {
                Status status = cairo_surface_set_user_data (Handle, ref user_data_key, data, destroy_func);
                if (status != Status.Success) {
                    throw new ApplicationException (String.Format (
                        "cairo_surface_set_user_data returned {0}", status));
                }
            } catch (Exception e) {
                Console.Error.WriteLine ("WARNING: Image data will be leaked! ({0} bytes)", Width * Height * 4);
                Console.Error.WriteLine (e);
            }
        }
    }
}
