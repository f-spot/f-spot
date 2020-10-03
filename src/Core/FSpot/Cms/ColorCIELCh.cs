//
// ColorCIELCh.cs
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
	public struct ColorCIELCh
	{
		public double L { get; }
		public double C { get; }
		public double h { get; }
		
		public ColorCIELab ToLab ()
		{
			NativeMethods.CmsLCh2Lab (out var lab, ref this);
			return lab;
		}
	}
}
