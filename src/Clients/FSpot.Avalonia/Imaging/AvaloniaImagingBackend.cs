//
// AvaloniaImagingBackend.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using FSpot.Imaging;
using FSpot.Utils;

using Hyena;

using TagLib.Image;

namespace FSpot.Avalonia.Imaging;

sealed class AvaloniaImagingBackend : IImagingBackend
{
	public ImageBuffer Decode (Stream stream, ImageOrientation orientation)
	{
		using var bitmap = AvaloniaPhotoDecoder.Decode (stream, svgBackgroundArgb: 0xFFFFFFFF);
		return ImageBuffers.TransformOrientation (ToRgbBuffer (bitmap), orientation);
	}

	public ImageBuffer ScaleToMaxSize (ImageBuffer source, int maxWidth, int maxHeight, bool upscale = true)
	{
		double scale = Math.Min ((double)maxWidth / source.Width, (double)maxHeight / source.Height);
		if (!upscale)
			scale = Math.Min (scale, 1.0);
		int width = Math.Max (1, (int)(source.Width * scale));
		int height = Math.Max (1, (int)(source.Height * scale));
		return Scale (source, width, height);
	}

	public ImageBuffer Scale (ImageBuffer source, int width, int height)
	{
		int srcChannels = source.HasAlpha ? 4 : 3;
		int dstStride = width * srcChannels;
		var pixels = new byte[dstStride * height];
		for (int y = 0; y < height; y++) {
			int srcY = Math.Min (source.Height - 1, y * source.Height / height);
			int srcRow = srcY * source.Rowstride;
			int dstRow = y * dstStride;
			for (int x = 0; x < width; x++) {
				int srcX = Math.Min (source.Width - 1, x * source.Width / width);
				int src = srcRow + srcX * srcChannels;
				int dst = dstRow + x * srcChannels;
				for (int c = 0; c < srcChannels; c++)
					pixels[dst + c] = source.Pixels[src + c];
			}
		}

		return new ImageBuffer (width, height, source.HasAlpha, dstStride, pixels);
	}

	public void SaveDerivedVersion (ImageBuffer buffer, SafeUri source, SafeUri dest, uint jpegQuality)
		=> SavePng (buffer, dest.AbsolutePath, Array.Empty<string> (), Array.Empty<string> ());

	public ImageBuffer? LoadThemeIcon (string name, int size)
		=> null;

	public byte[] SerializeIcon (ImageBuffer icon)
	{
		using var stream = new MemoryStream ();
		using (var bitmap = ToBgraBitmap (icon))
			bitmap.Save (stream, PngBitmapEncoderOptions.Default);
		return stream.ToArray ();
	}

	public ImageBuffer DeserializeIcon (byte[] data)
	{
		using var stream = new MemoryStream (data);
		return Decode (stream, ImageOrientation.TopLeft);
	}

	public ThumbnailPng LoadPng (Stream stream)
		=> new ThumbnailPng { Image = Decode (stream, ImageOrientation.TopLeft) };

	public void SavePng (ImageBuffer image, string path, string[] optionKeys, string[] optionValues)
	{
		using var bitmap = ToBgraBitmap (image);
		bitmap.Save (path, PngBitmapEncoderOptions.Default);
	}

	internal static Bitmap ApplyOrientation (Bitmap bitmap, ImageOrientation orientation)
	{
		if (orientation is ImageOrientation.None or ImageOrientation.TopLeft)
			return bitmap;

		using (bitmap)
			return ToBgraBitmap (ImageBuffers.TransformOrientation (ToRgbBuffer (bitmap), orientation));
	}

	static ImageBuffer ToRgbBuffer (Bitmap bitmap)
	{
		int width = bitmap.PixelSize.Width;
		int height = bitmap.PixelSize.Height;
		int srcStride = width * 4;
		var bgra = new byte[srcStride * height];
		unsafe {
			fixed (byte* pointer = bgra)
				bitmap.CopyPixels (new global::Avalonia.PixelRect (0, 0, width, height), (nint)pointer, bgra.Length, srcStride);
		}

		int dstStride = width * 3;
		var rgb = new byte[dstStride * height];
		for (int y = 0; y < height; y++) {
			int srcRow = y * srcStride;
			int dstRow = y * dstStride;
			for (int x = 0; x < width; x++) {
				int src = srcRow + x * 4;
				int dst = dstRow + x * 3;
				rgb[dst] = bgra[src + 2];
				rgb[dst + 1] = bgra[src + 1];
				rgb[dst + 2] = bgra[src];
			}
		}

		return new ImageBuffer (width, height, false, dstStride, rgb);
	}

	static WriteableBitmap ToBgraBitmap (ImageBuffer buffer)
	{
		var bitmap = new WriteableBitmap (
			new PixelSize (buffer.Width, buffer.Height),
			new global::Avalonia.Vector (96, 96),
			PixelFormat.Bgra8888,
			AlphaFormat.Premul);
		int channels = buffer.HasAlpha ? 4 : 3;
		using var locked = bitmap.Lock ();
		unsafe {
			var dest = (byte*)locked.Address;
			int destStride = locked.RowBytes;
			for (int y = 0; y < buffer.Height; y++) {
				int srcRow = y * buffer.Rowstride;
				byte* dstRow = dest + y * destStride;
				for (int x = 0; x < buffer.Width; x++) {
					int src = srcRow + x * channels;
					int dst = x * 4;
					dstRow[dst] = buffer.Pixels[src + 2];
					dstRow[dst + 1] = buffer.Pixels[src + 1];
					dstRow[dst + 2] = buffer.Pixels[src];
					dstRow[dst + 3] = buffer.HasAlpha ? buffer.Pixels[src + 3] : (byte)255;
				}
			}
		}

		return bitmap;
	}
}
