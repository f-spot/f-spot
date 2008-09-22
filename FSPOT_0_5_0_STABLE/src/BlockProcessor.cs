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

#if ENABLE_NUNIT
using NUnit.Framework;
#endif

namespace FSpot {
	public class BlockProcessor {
		Rectangle rect;
		Rectangle area;
		
		public BlockProcessor (Rectangle area, int step)
		{
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
			return !region.IsEmpty;
		}

	}

#if ENABLE_NUNIT
	[TestFixture]
	public class BlockProcessorTests 
	{
		[Test]
		public void Contained ()
		{
			BlockProcessor proc = new BlockProcessor (new Rectangle (0, 0, 10, 10), 1000);
			Rectangle step;

			Assert.IsTrue (proc.Step (out step));
			Assert.AreEqual (step, new Rectangle (0, 0, 10, 10));
			Assert.IsFalse (proc.Step (out step));
		}

		[Test]
		public void Step ()
		{
			BlockProcessor proc = new BlockProcessor (new Rectangle (10, 100, 25, 15), 20);
			Rectangle step;

			Assert.AreEqual (proc.Step (out step), true);
			Assert.AreEqual (step, new Rectangle (10, 100, 20, 15));
			Assert.AreEqual (proc.Step (out step), true);
			Assert.AreEqual (step, new Rectangle (30, 100, 5, 15));
			Assert.AreEqual (proc.Step (out step), false);
		}
	}
#endif
}
