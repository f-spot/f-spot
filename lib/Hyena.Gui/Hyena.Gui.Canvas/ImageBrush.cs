//
// ImageBrush.cs
//
// Author:
//       Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Aaron Bockover
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;

using Cairo;
using Hyena.Gui;

namespace Hyena.Gui.Canvas
{
    public class ImageBrush : Brush
    {
        private ImageSurface surface;
        //private bool surface_owner;

        public ImageBrush ()
        {
        }

        public ImageBrush (string path) : this (new Gdk.Pixbuf (path), true)
        {
        }

        public ImageBrush (Gdk.Pixbuf pixbuf, bool disposePixbuf)
            : this (new PixbufImageSurface (pixbuf, disposePixbuf), true)
        {
        }

        public ImageBrush (ImageSurface surface, bool disposeSurface)
        {
            this.surface = surface;
            //this.surface_owner = disposeSurface;
        }

        protected ImageSurface Surface {
            get { return surface; }
            set { surface = value; }
        }

        public override bool IsValid {
            get { return surface != null; }
        }

        public override void Apply (Cairo.Context cr)
        {
            if (surface != null) {
                cr.SetSource (surface);
            }
        }

        public override double Width {
            get { return surface == null ? 0 : surface.Width; }
        }

        public override double Height {
            get { return surface == null ? 0 : surface.Height; }
        }
    }
}
