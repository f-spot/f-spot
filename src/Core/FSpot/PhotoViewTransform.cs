// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Utils;

namespace FSpot
{
	public static class PhotoViewTransform
	{
		public static (int X, int Y) MapPoint (
			int viewWidth, int viewHeight, int imageWidth, int imageHeight,
			double zoom, double panX, double panY, double viewX, double viewY)
		{
			double scale = zoom <= 0 ? 1 : zoom;
			double originX = viewWidth / 2.0;
			double originY = viewHeight / 2.0;
			double x = originX + (viewX - panX - originX) / scale;
			double y = originY + (viewY - panY - originY) / scale;
			return UniformImageLayout.MapPoint (viewWidth, viewHeight, imageWidth, imageHeight, x, y);
		}

		// Inverse of MapPoint (without its clamp): where an image pixel is drawn.
		public static (double X, double Y) ImageToView (
			int viewWidth, int viewHeight, int imageWidth, int imageHeight,
			double zoom, double panX, double panY, double imageX, double imageY)
		{
			if (viewWidth <= 0 || viewHeight <= 0 || imageWidth <= 0 || imageHeight <= 0)
				return (0, 0);
			double fit = Math.Min ((double)viewWidth / imageWidth, (double)viewHeight / imageHeight);
			double originX = viewWidth / 2.0;
			double originY = viewHeight / 2.0;
			double x = (viewWidth - imageWidth * fit) / 2.0 + imageX * fit;
			double y = (viewHeight - imageHeight * fit) / 2.0 + imageY * fit;
			double scale = zoom <= 0 ? 1 : zoom;
			return (originX + (x - originX) * scale + panX, originY + (y - originY) * scale + panY);
		}

		public static Imaging.PixelRect MapRect (
			int viewWidth, int viewHeight, int imageWidth, int imageHeight,
			double zoom, double panX, double panY,
			double x1, double y1, double x2, double y2)
		{
			var a = MapPoint (viewWidth, viewHeight, imageWidth, imageHeight, zoom, panX, panY, x1, y1);
			var b = MapPoint (viewWidth, viewHeight, imageWidth, imageHeight, zoom, panX, panY, x2, y2);
			int left = Math.Min (a.X, b.X);
			int top = Math.Min (a.Y, b.Y);
			return new Imaging.PixelRect (left, top, Math.Abs (b.X - a.X), Math.Abs (b.Y - a.Y));
		}
	}
}
