// 
// CairoExtensions.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

// Some functions are from Paint.NET:

/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;

namespace Pinta.Core
{
	public static class CairoExtensions
	{
		public unsafe static Gdk.Pixbuf ToPixbuf (this Cairo.ImageSurface surfSource)
		{
			Cairo.ImageSurface surf = surfSource.Clone ();
			surf.Flush ();

			ColorBgra* dstPtr = (ColorBgra*)surf.DataPtr;
			int len = surf.Data.Length / 4;

			for (int i = 0; i < len; i++) {
				if (dstPtr->A != 0)
					*dstPtr = (ColorBgra.FromBgra (dstPtr->R, dstPtr->G, dstPtr->B, dstPtr->A));
				dstPtr++;
			}

			Gdk.Pixbuf pb = new Gdk.Pixbuf (surf.Data, true, 8, surf.Width, surf.Height, surf.Stride);
			(surf as IDisposable).Dispose ();
			return pb;
		}

		public static unsafe ColorBgra* GetPointAddressUnchecked (this ImageSurface surf, int x, int y)
		{
			ColorBgra* dstPtr = (ColorBgra*)surf.DataPtr;

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
