/*
 * Filters/JpegFilter.cs
 *
 * Author(s)
 *   Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 *
 */

using Gdk;
using System;

namespace FSpot {
	public class BlockProcessor {
		Rectangle rect;
		Rectangle area;
		int step;
		
		public BlockProcessor (Rectangle area, int step)
		{
			this.step = step;
			this.area = area;
			rect = new Gdk.Rectangle (area.X, area.Y, 
						  Math.Min (step, area.Width),
						  Math.Min (step, area.Height));
		}

		public bool Step (out Rectangle region)
		{
			rect.Intersect (area, out region);
			rect.X += rect.Width;
			if (rect.X >= area.Right) {
				rect.X = area.X;
				rect.Y += rect.Height;
			}
			System.Console.WriteLine ("region = {0}", region);
			return !region.IsEmpty;
		}
	}
}
