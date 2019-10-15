//
// ToolTipWindow.cs
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

using Gtk;

namespace FSpot.Widgets
{
    public class ToolTipWindow : Gtk.Window
    {
        public ToolTipWindow () : base(Gtk.WindowType.Popup)
        {
            Name = "gtk-tooltips";
            AppPaintable = true;
            BorderWidth = 4;
        }

        protected override bool OnExposeEvent (Gdk.EventExpose args)
        {
            Gtk.Style.PaintFlatBox (Style, GdkWindow, State, ShadowType.Out, args.Area,
                            this, "tooltip", Allocation.X, Allocation.Y, Allocation.Width,
                            Allocation.Height);
            
            return base.OnExposeEvent (args);
        }
    }
}
