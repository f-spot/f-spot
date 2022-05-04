//
// ShadowMarginStyle.cs
//
// Author:
//       Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Aaron Bockover
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;

using Cairo;

namespace Hyena.Gui.Canvas
{
	public class ShadowMarginStyle : MarginStyle
	{
		int shadow_size;
		double shadow_opacity = 0.75;
		Brush fill;

		public ShadowMarginStyle ()
		{
		}

		public override void Apply (CanvasItem item, Context cr)
		{
			int steps = ShadowSize;
			double opacity_step = ShadowOpacity / ShadowSize;
			var color = new Color (0, 0, 0);

			double width = Math.Round (item.Allocation.Width);
			double height = Math.Round (item.Allocation.Height);

			if (Fill != null) {
				cr.Rectangle (shadow_size, shadow_size, width - ShadowSize * 2, height - ShadowSize * 2);
				Fill.Apply (cr);
				cr.Fill ();
			}

			cr.LineWidth = 1.0;

			for (int i = 0; i < steps; i++) {
				CairoExtensions.RoundedRectangle (cr,
					i + 0.5,
					i + 0.5,
					(width - 2 * i) - 1,
					(height - 2 * i) - 1,
					steps - i);

				color.A = opacity_step * (i + 1);
				cr.SetSourceColor (color);
				cr.Stroke ();
			}
		}

		public double ShadowOpacity {
			get { return shadow_opacity; }
			set { shadow_opacity = value; }
		}

		public int ShadowSize {
			get { return shadow_size; }
			set { shadow_size = value; }
		}

		public Brush Fill {
			get { return fill; }
			set { fill = value; }
		}
	}
}
