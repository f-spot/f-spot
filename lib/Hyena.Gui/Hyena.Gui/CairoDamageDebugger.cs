//
// CairoDamageDebugger.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2010 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Cairo;

namespace Hyena.Gui
{
	public static class CairoDamageDebugger
	{
		static Random rand = new Random ();

		public static void RenderDamage (this Context cr, Gdk.Rectangle damage)
		{
			RenderDamage (cr, damage.X, damage.Y, damage.Width, damage.Height);
		}

		public static void RenderDamage (this Context cr, Cairo.Rectangle damage)
		{
			RenderDamage (cr, damage.X, damage.Y, damage.Width, damage.Height);
		}

		public static void RenderDamage (this Context cr, double x, double y, double w, double h)
		{
			cr.Save ();
			cr.LineWidth = 1.0;
			cr.SetSourceColor (CairoExtensions.RgbToColor ((uint)rand.Next (0, 0xffffff)));
			cr.Rectangle (x + 0.5, y + 0.5, w - 1, h - 1);
			cr.Stroke ();
			cr.Restore ();
		}
	}
}
