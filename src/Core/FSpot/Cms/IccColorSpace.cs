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
		// Only Rgb is ever named. Profile.ColorSpace casts whatever signature lcms
		// reports, so an unlisted space still compares unequal to Rgb, which is all
		// any caller asks. Add members back when something needs to name one.
		Rgb = 0x52474220,  /* 'RGB ' */
	}
}
