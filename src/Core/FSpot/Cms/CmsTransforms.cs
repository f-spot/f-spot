// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Cms
{
	public static class CmsTransforms
	{
		// lcms2 cmsFLAGS_COPY_ALPHA (2.8+). lcmsNET 1.2.1 does not name it, and
		// Format.Rgba8 without it writes a zero alpha.
		public const uint CopyAlpha = 0x04000000;

		// Black-point compensation changes a CMYK print and leaves a matrix RGB
		// display transform byte-identical, so only print asks for it.
		public static uint For (bool print, bool copyAlpha)
		{
			uint flags = copyAlpha ? CopyAlpha : 0;
			if (print)
				flags |= (uint)lcmsNET.CmsFlags.BlackPointCompensation;
			return flags;
		}
	}
}
