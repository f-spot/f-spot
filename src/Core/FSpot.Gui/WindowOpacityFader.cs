//
// WindowOpacityFader.cs
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

using FSpot.Bling;

namespace FSpot.Gui
{
    public class WindowOpacityFader
    {
        Gtk.Window win;
        readonly DoubleAnimation fadin;

        public WindowOpacityFader (Gtk.Window win, double target, double msec)
        {
            this.win = win;
            win.Mapped += HandleMapped;
            win.Unmapped += HandleUnmapped;
            fadin = new DoubleAnimation (0.0, target, TimeSpan.FromMilliseconds (msec), opacity => {
                CompositeUtils.SetWinOpacity (win, opacity);
            });
        }

        [GLib.ConnectBefore]
        public void HandleMapped (object sender, EventArgs args)
        {
            bool composited = CompositeUtils.SupportsHint (win.Screen, "_NET_WM_WINDOW_OPACITY");
            if (!composited) {
                return;
            }
            
            CompositeUtils.SetWinOpacity (win, 0.0);
            fadin.Start ();
        }

        public void HandleUnmapped (object sender, EventArgs args)
        {
            fadin.Stop ();
        }
    }
}
