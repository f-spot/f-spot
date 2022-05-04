//
// Prelight.cs
//
// Author:
//       Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Aaron Bockover
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using Hyena.Gui.Theming;

namespace Hyena.Gui.Canvas
{
	public static class Prelight
	{
		public static void Gradient (Cairo.Context cr, Theme theme, Rect rect, double opacity)
		{
			cr.Save ();
			cr.Translate (rect.X, rect.Y);

			var x = rect.Width / 2.0;
			var y = rect.Height / 2.0;
			var grad = new Cairo.RadialGradient (x, y, 0, x, y, rect.Width / 2.0);
			grad.AddColorStop (0, new Cairo.Color (0, 0, 0, 0.1 * opacity));
			grad.AddColorStop (1, new Cairo.Color (0, 0, 0, 0.35 * opacity));
			cr.SetSource (grad);
			CairoExtensions.RoundedRectangle (cr, rect.X, rect.Y, rect.Width, rect.Height, theme.Context.Radius);
			cr.Fill ();
			grad.Dispose ();

			cr.Restore ();
		}
	}
}
