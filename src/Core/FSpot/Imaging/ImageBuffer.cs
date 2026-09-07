//
// ImageBuffer.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace FSpot.Imaging
{
	public sealed class ImageBuffer
	{
		public int Width { get; }
		public int Height { get; }
		public bool HasAlpha { get; }
		public int Rowstride { get; }
		public byte[] Pixels { get; }

		public ImageBuffer (int width, int height, bool hasAlpha, int rowstride, byte[] pixels)
		{
			if (width <= 0)
				throw new ArgumentOutOfRangeException (nameof (width));
			if (height <= 0)
				throw new ArgumentOutOfRangeException (nameof (height));
			if (rowstride <= 0)
				throw new ArgumentOutOfRangeException (nameof (rowstride));
			Width = width;
			Height = height;
			HasAlpha = hasAlpha;
			Rowstride = rowstride;
			Pixels = pixels ?? throw new ArgumentNullException (nameof (pixels));
		}
	}
}
