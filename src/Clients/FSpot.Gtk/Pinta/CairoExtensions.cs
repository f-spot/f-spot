//
// CairoExtensions.cs
//
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
//
// Copyright (c) 2010 Jonathan Pobst
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// Some functions are from Paint.NET:

/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;

using Cairo;

using FSpot;

namespace Pinta.Core
{
	public static class CairoExtensions
	{
		public unsafe static Gdk.Pixbuf ToPixbuf (this Cairo.ImageSurface surfSource)
		{
			Cairo.ImageSurface surf = surfSource.Clone ();
			surf.Flush ();

			var dstPtr = (ColorBgra*)surf.DataPtr;
			int len = surf.Data.Length / 4;

			for (int i = 0; i < len; i++) {
				if (dstPtr->A != 0)
					*dstPtr = (ColorBgra.FromBgra (dstPtr->R, dstPtr->G, dstPtr->B, dstPtr->A));
				dstPtr++;
			}

			var pb = new Gdk.Pixbuf (surf.Data, true, 8, surf.Width, surf.Height, surf.Stride);
			(surf as IDisposable).Dispose ();
			return pb;
		}

		public static unsafe ColorBgra* GetPointAddressUnchecked (this ImageSurface surf, int x, int y)
		{
			var dstPtr = (ColorBgra*)surf.DataPtr;

			dstPtr += (x) + (y * surf.Width);

			return dstPtr;
		}

		// This isn't really an extension method, since it doesn't use
		// the passed in argument, but it's nice to have the same calling
		// convention as the uncached version.  If you can use this one
		// over the other, it is much faster in tight loops (like effects).
		public static unsafe ColorBgra GetPointUnchecked (this ImageSurface surf, ColorBgra* surfDataPtr, int surfWidth, int x, int y)
		{
			ColorBgra* dstPtr = surfDataPtr;

			dstPtr += (x) + (y * surfWidth);

			return *dstPtr;
		}
	}
}
