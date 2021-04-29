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
	public enum Format : uint
	{
		Rgb8 = 262169,
		Rgba8 = 262297,
		Rgba8Planar = 266393,
		Bgr8 = 263193,
		Rgb16 = 262170,
		Rgb16Planar = 266266,
		Rgba16 = 262298,
		Rgba16se = 264346,
		Rgb16se = 264218,
		Lab8 = 655385,
		Lab16 = 655386,
		Xyz16 = 589850,
		Yxy16 = 917530
	}
}
