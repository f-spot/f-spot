//
// Format.cs
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
	// Little-CMS pixel formats (the TYPE_* macros in lcms2.h); a test pins each value
	// to lcmsNET's constant of the same name.
	public enum Format : uint
	{
		Rgb8 = 262169,
		Rgba8 = 262297,
		Rgba8Planar = 266393,
		Rgb16 = 262170,
		Rgba16 = 262298,
		Lab16 = 655386
	}

	static class FormatExtensions
	{
		// Transform.Apply is handed a pixel count and raw memory; lcmsNET wants byte
		// spans, so the format has to say how wide a pixel is. Decoded the way lcms2.h
		// does: T_BYTES is the low three bits (0 meaning double), T_CHANNELS the next
		// four, T_EXTRA the three after that. Planar formats occupy the same total.
		public static int BytesPerPixel (this Format format)
		{
			uint value = (uint)format;
			int bytes = (int)(value & 7);
			if (bytes == 0)
				bytes = 8;
			int channels = (int)((value >> 3) & 15);
			int extra = (int)((value >> 7) & 7);
			return bytes * (channels + extra);
		}
	}
}
