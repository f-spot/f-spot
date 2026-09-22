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
	public static WriteableBitmap? FromBitmap (Bitmap? source, double scale = 1, double dipWidth = 256, double dipHeight = 72)
	{
		if (source is null || source.PixelSize.Width <= 0 || source.PixelSize.Height <= 0)
			return null;

		int srcWidth = source.PixelSize.Width;
		int srcHeight = source.PixelSize.Height;
		int srcStride = srcWidth * 4;
		var bgra = new byte[srcStride * srcHeight];
		unsafe {
			fixed (byte* pointer = bgra)
				source.CopyPixels (new global::Avalonia.PixelRect (0, 0, srcWidth, srcHeight), (nint)pointer, bgra.Length, srcStride);
		}

		int width = global::FSpot.Avalonia.HiDpi.PixelSize (dipWidth, scale);
		int height = global::FSpot.Avalonia.HiDpi.PixelSize (dipHeight, scale);
		byte[] pixels = RgbHistogram.RenderBgra (RgbHistogram.CountBgra (bgra), width, height);
		double dpi = 96 * Math.Max (scale, 1);
		var bitmap = new WriteableBitmap (new PixelSize (width, height), new global::Avalonia.Vector (dpi, dpi),
			PixelFormat.Bgra8888, AlphaFormat.Premul);
		using var locked = bitmap.Lock ();
		int row = width * 4;
		for (int y = 0; y < height; y++)
			Marshal.Copy (pixels, y * row, locked.Address + y * locked.RowBytes, row);

		return bitmap;
	}
}
