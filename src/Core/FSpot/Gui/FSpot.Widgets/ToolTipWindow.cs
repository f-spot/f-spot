//
// ToolTipWindow.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Gtk;

namespace FSpot.Widgets
{
	public class ToolTipWindow : Gtk.Window
	{
		public ToolTipWindow () : base (Gtk.WindowType.Popup)
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
