//
// IccColorSpace.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

namespace FSpot.Cms
{
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
