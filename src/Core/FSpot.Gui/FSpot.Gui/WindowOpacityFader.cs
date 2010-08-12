/*
 * Fader.cs
 *
 * Copyright 2007 Novell Inc.
 *
 * Author
 *   Larry Ewing <lewing@novell.com>
 *
 * See COPYING for license information.
 *
 */

using System;
using Gtk;
using FSpot.Bling;

namespace FSpot.Gui
{
    public class WindowOpacityFader
    {
        Gtk.Window win;
        DoubleAnimation fadin;

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
