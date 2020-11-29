//
// WindowOpacityFader.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Bling;

namespace FSpot.Gui
{
	public class WindowOpacityFader
	{
		readonly Gtk.Window win;
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
