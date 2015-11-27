//
// CellContext.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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

using Hyena.Gui.Theming;

namespace Hyena.Data.Gui
{
    public class CellContext
    {
        public CellContext ()
        {
            Opaque = true;
        }

        public Cairo.Context Context { get; set; }
        public Pango.Layout Layout { get; set; }
        public Pango.FontDescription FontDescription { get; set; }
        public Gtk.Widget Widget { get; set; }
        public Gtk.StateType State { get; set; }
        public Gdk.Drawable Drawable { get; set; }
        public Theme Theme { get; set; }
        public Gdk.Rectangle Area { get; set; }
        public Gdk.Rectangle Clip { get; set; }
        public bool TextAsForeground { get; set; }
        public bool Opaque { get; set; }
        public int ViewRowIndex { get; set; }
        public int ViewColumnIndex { get; set; }
        public int ModelRowIndex { get; set; }
        public bool IsRtl { get; set; }
    }
}
