// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Imaging;

namespace FSpot.Utils
{
	// GTK Loupe.SetSamplePoint: a 2*radius box centered on the pointer,
	// clamped to the image.
	public static class LoupeRegion
	{
		public const int DefaultRadius = 128;

		public static PixelRect Around (int imageWidth, int imageHeight, int x, int y, int radius = DefaultRadius)
		{
			if (imageWidth <= 0 || imageHeight <= 0 || radius <= 0)
				return default;

			int width = Math.Min (radius * 2, imageWidth);
			int height = Math.Min (radius * 2, imageHeight);
			int left = x - width / 2;
			int top = y - height / 2;
			if (left + width > imageWidth)
				left = imageWidth - width;
			if (top + height > imageHeight)
				top = imageHeight - height;
			if (left < 0)
				left = 0;
			if (top < 0)
				top = 0;
			return new PixelRect (left, top, width, height);
		}
	}
}
