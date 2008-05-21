/*
 * Cms.IccColorSpace.cs A very incomplete wrapper for lcms
 *
 * Author(s):
 *	Larry Ewing  <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

namespace Cms {
	public enum IccColorSpace : uint {
		XYZ                        = 0x58595A20,  /* 'XYZ ' */
		Lab                        = 0x4C616220,  /* 'Lab ' */
		Luv                        = 0x4C757620,  /* 'Luv ' */
		YCbCr                      = 0x59436272,  /* 'YCbr' */
		Yxy                        = 0x59787920,  /* 'Yxy ' */
		Rgb                        = 0x52474220,  /* 'RGB ' */
		Gray                       = 0x47524159,  /* 'GRAY' */
		Hsv                        = 0x48535620,  /* 'HSV ' */
		Hls                        = 0x484C5320,  /* 'HLS ' */
		Cmyk                       = 0x434D594B,  /* 'CMYK' */
		Cmy                        = 0x434D5920,  /* 'CMY ' */
		Color2                     = 0x32434C52,  /* '2CLR' */
		Color3                     = 0x33434C52,  /* '3CLR' */
		Color4                     = 0x34434C52,  /* '4CLR' */
		Color5                     = 0x35434C52,  /* '5CLR' */
		Color6                     = 0x36434C52,  /* '6CLR' */
		Color7                     = 0x37434C52,  /* '7CLR' */
		Color8                     = 0x38434C52,  /* '8CLR' */
		Color9                     = 0x39434C52,  /* '9CLR' */
		Color10                    = 0x41434C52,  /* 'ACLR' */
		Color11                    = 0x42434C52,  /* 'BCLR' */
		Color12                    = 0x43434C52,  /* 'CCLR' */
		Color13                    = 0x44434C52,  /* 'DCLR' */
		Color14                    = 0x45434C52,  /* 'ECLR' */
		Color15                    = 0x46434C52,  /* 'FCLR' */
	}
}
