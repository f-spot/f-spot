// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Imaging;

namespace FSpot.Utils
{
	// Map a rubber-band drawn on a Uniform-stretched, centered image view
	// into buffer pixels. Used by Avalonia editor selection; GTK's ImageView
	// already has its own mapping.
	public static class UniformImageLayout
	{
		public static PixelRect MapRect (
			int viewWidth, int viewHeight, int imageWidth, int imageHeight,
			double x1, double y1, double x2, double y2)
		{
			if (viewWidth <= 0 || viewHeight <= 0 || imageWidth <= 0 || imageHeight <= 0)
				return default;

			double scale = Math.Min ((double)viewWidth / imageWidth, (double)viewHeight / imageHeight);
			double drawnWidth = imageWidth * scale;
			double drawnHeight = imageHeight * scale;
			double originX = (viewWidth - drawnWidth) / 2.0;
			double originY = (viewHeight - drawnHeight) / 2.0;

			int left = ToImage (Math.Min (x1, x2), originX, scale, imageWidth);
			int right = ToImage (Math.Max (x1, x2), originX, scale, imageWidth);
			int top = ToImage (Math.Min (y1, y2), originY, scale, imageHeight);
			int bottom = ToImage (Math.Max (y1, y2), originY, scale, imageHeight);
			return new PixelRect (left, top, Math.Max (0, right - left), Math.Max (0, bottom - top));
		}

		public static (int X, int Y) MapPoint (
			int viewWidth, int viewHeight, int imageWidth, int imageHeight,
			double viewX, double viewY)
		{
			var area = MapRect (viewWidth, viewHeight, imageWidth, imageHeight, viewX, viewY, viewX, viewY);
			return (area.X, area.Y);
		}

		static int ToImage (double view, double origin, double scale, int imageSize)
		{
			int pixel = (int)Math.Round ((view - origin) / scale);
			if (pixel < 0)
				return 0;
			if (pixel > imageSize)
				return imageSize;
			return pixel;
		}
	}
}
