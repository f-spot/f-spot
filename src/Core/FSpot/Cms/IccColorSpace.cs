//
// IccColorSpace.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Cms
{
	public enum IccColorSpace : uint
	{
		// Profile.ColorSpace casts whatever signature lcms reports, so an unlisted
		// space still compares unequal to these. Name one when a caller has to.
		Rgb = 0x52474220,  /* 'RGB ' */
		Cmyk = 0x434D594B, /* 'CMYK' */
	}
}
