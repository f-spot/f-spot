/*
 * Cms.Format.cs A very incomplete wrapper for lcms
 *
 * Author(s):
 *	Larry Ewing  <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

namespace Cms {
	public enum Format : uint {
		Rgb8  = 262169,
		Rgba8 = 262297,
		Rgba8Planar = 266393,
		Gbr8  = 263193,
		Rgb16 = 262170,
		Rgb16Planar = 266266,
		Rgba16 = 262298,
		Rgba16se = 264436,
		Rgb16se = 264218,
		Lab8 = 655385,
	        Lab16 = 655386,
		Xyz16 = 589858,
		Yxy16 = 917530
	}
}
