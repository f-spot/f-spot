//
// HistogramRenderer.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using FSpot.Imaging;

namespace FSpot.Avalonia.Imaging;

static class HistogramRenderer
{
	// GTK Histogram.Generate draws one column per code at 256×128. The view scales that
	// picture once. Enlarging it here and again in the Image blends every column with its neighbor.
	public const int SourceEdge = 256;
	public const int ChartWidth = 256;
	public const int ChartHeight = 128;

	public static Bitmap FitInside (Bitmap source, int edge)
	{
		int srcWidth = source.PixelSize.Width;
		int srcHeight = source.PixelSize.Height;
		if (srcWidth <= edge && srcHeight <= edge)
			return source;

		// DecodeToWidth(256) leaves a portrait taller than GTK's Load(256, 256) box.
		double scale = Math.Min ((double)edge / srcWidth, (double)edge / srcHeight);
		int width = Math.Max (1, (int)Math.Round (srcWidth * scale));
		int height = Math.Max (1, (int)Math.Round (srcHeight * scale));
		byte[] scaled = ScaleBgra (Copy (source), srcWidth, srcHeight, width, height);
		return BitmapFrom (scaled, width, height, 96);
	}

	public static WriteableBitmap? FromBitmap (Bitmap? source, double scale = 1, double dipWidth = ChartWidth, double dipHeight = ChartHeight,
		byte backgroundRed = 0, byte backgroundGreen = 0, byte backgroundBlue = 0, byte backgroundAlpha = 255)
	{
		if (source is null || source.PixelSize.Width <= 0 || source.PixelSize.Height <= 0)
			return null;

		byte[] chart = RgbHistogram.RenderBgra (RgbHistogram.CountBgra (Copy (source)), ChartWidth, ChartHeight,
			backgroundRed, backgroundGreen, backgroundBlue, backgroundAlpha);
		int width = global::FSpot.Avalonia.HiDpi.PixelSize (dipWidth, scale);
		int height = global::FSpot.Avalonia.HiDpi.PixelSize (dipHeight, scale);
		// A smaller chart (the popup at half size) is the one scale. A larger one would be
		// scaled again by the sidebar Image, so keep the 256×128 picture at 96 dpi.
		bool enlarged = width > ChartWidth || height > ChartHeight;
		if (enlarged) {
			width = ChartWidth;
			height = ChartHeight;
		}

		byte[] pixels = ScaleBgra (chart, ChartWidth, ChartHeight, width, height);
		double dpi = enlarged ? 96 : 96 * Math.Max (scale, 1);
		return BitmapFrom (pixels, width, height, dpi);
	}

	static byte[] Copy (Bitmap source)
	{
		int width = source.PixelSize.Width;
		int height = source.PixelSize.Height;
		int stride = width * 4;
		var pixels = new byte[stride * height];
		unsafe {
			fixed (byte* pointer = pixels)
				source.CopyPixels (new global::Avalonia.PixelRect (0, 0, width, height), (nint)pointer, pixels.Length, stride);
		}

		// CountBgra reads byte 0 as blue. A decoded photo is Rgba8888.
		if (source.Format == PixelFormat.Rgba8888) {
			for (int i = 0; i < pixels.Length; i += 4)
				(pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);
		}

		return pixels;
	}

	static WriteableBitmap BitmapFrom (byte[] pixels, int width, int height, double dpi)
	{
		var bitmap = new WriteableBitmap (new PixelSize (width, height), new global::Avalonia.Vector (dpi, dpi),
			PixelFormat.Bgra8888, AlphaFormat.Premul);
		using var locked = bitmap.Lock ();
		int row = width * 4;
		for (int y = 0; y < height; y++)
			Marshal.Copy (pixels, y * row, locked.Address + y * locked.RowBytes, row);
		return bitmap;
	}

	static byte[] ScaleBgra (byte[] source, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
	{
		if (srcWidth == dstWidth && srcHeight == dstHeight)
			return source;

		var destination = new byte[dstWidth * dstHeight * 4];
		for (int y = 0; y < dstHeight; y++) {
			Map ((y + 0.5) * srcHeight / dstHeight - 0.5, srcHeight, out int y0, out int y1, out double fy);
			for (int x = 0; x < dstWidth; x++) {
				Map ((x + 0.5) * srcWidth / dstWidth - 0.5, srcWidth, out int x0, out int x1, out double fx);
				int i = (y * dstWidth + x) * 4;
				for (int channel = 0; channel < 4; channel++) {
					int blended = Blend (
						source[(y0 * srcWidth + x0) * 4 + channel],
						source[(y0 * srcWidth + x1) * 4 + channel],
						source[(y1 * srcWidth + x0) * 4 + channel],
						source[(y1 * srcWidth + x1) * 4 + channel],
						fx, fy);
					destination[i + channel] = (byte)blended;
				}
			}
		}

		return destination;
	}

	static void Map (double sample, int limit, out int start, out int next, out double fraction)
	{
		start = (int)Math.Floor (sample);
		fraction = sample - start;
		if (start < 0) {
			start = 0;
			fraction = 0;
		}

		next = start + 1;
		if (next >= limit) {
			next = start;
			fraction = 0;
		}
	}

	static int Blend (int topLeft, int topRight, int bottomLeft, int bottomRight, double fx, double fy)
	{
		double top = (1 - fx) * topLeft + fx * topRight;
		double bottom = (1 - fx) * bottomLeft + fx * bottomRight;
		double value = (1 - fy) * top + fy * bottom;
		int rounded = (int)Math.Round (value, MidpointRounding.AwayFromZero);
		if (rounded < 0)
			return 0;
		return rounded > 255 ? 255 : rounded;
	}
}
