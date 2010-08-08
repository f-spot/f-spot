/*
 * Cms.Intent.cs A very incomplete wrapper for lcms
 *
 * Author(s):
 *	Larry Ewing  <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

namespace Cms {
	public enum Intent {
		Perceptual           = 0,
		RelativeColorimetric = 1,
		Saturation           = 2,
		AbsoluteColorimetric = 3
	}
}
