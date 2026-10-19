// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.Printing;

using SkiaSharp;

namespace FSpot.Avalonia;

// JPEG at quality 95, the same encode GTK's zip export uses after JpegFilter.
// Photos that already fit are not enlarged. A file Skia cannot read returns null
// so the archive keeps the original bytes.
static class ZipJpeg
{
	public static byte[]? Encode (string path, int longerSide)
	{
		using SKBitmap? bitmap = PrintImageLoader.Load (path, longerSide);
		if (bitmap is null)
			return null;
		using SKImage image = SKImage.FromBitmap (bitmap);
		using SKData data = image.Encode (SKEncodedImageFormat.Jpeg, 95);
		return data?.ToArray ();
	}
}
