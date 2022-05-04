//
// ShadingTestWindow.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Gtk;

namespace Hyena.Gui
{
	public class ShadingTestWindow : Window
	{
		int steps = 16;

		public ShadingTestWindow () : base ("Shading Test")
		{
			SetSizeRequest (512, 512);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window);

			double step_width = Allocation.Width / (double)steps;
			double step_height = Allocation.Height / (double)steps;
			double h = 1.0;
			double s = 0.0;

			for (int xi = 0, i = 0; xi < steps; xi++) {
				for (int yi = 0; yi < steps; yi++, i++) {
					double bg_b = (double)(i / 255.0);
					double fg_b = 1.0 - bg_b;

					double x = Allocation.X + xi * step_width;
					double y = Allocation.Y + yi * step_height;

					cr.Rectangle (x, y, step_width, step_height);
					cr.SetSourceColor (CairoExtensions.ColorFromHsb (h, s, bg_b));
					cr.Fill ();

					var layout = new Pango.Layout (PangoContext);
					layout.SetText (((int)(bg_b * 255.0)).ToString ());
					layout.GetPixelSize (out var tw, out var th);

					cr.Translate (0.5, 0.5);
					cr.MoveTo (x + (step_width - tw) / 2.0, y + (step_height - th) / 2.0);
					cr.SetSourceColor (CairoExtensions.ColorFromHsb (h, s, fg_b));
					PangoCairoHelper.ShowLayout (cr, layout);
					cr.Translate (-0.5, -0.5);
				}
			}

			CairoExtensions.DisposeContext (cr);
			return true;
		}

	}
}
