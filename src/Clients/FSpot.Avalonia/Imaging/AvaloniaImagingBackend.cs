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

using SkiaSharp;

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
	{
		string extension = dest.GetExtension ().ToLowerInvariant ();
		int quality = jpegQuality > 100 ? 100 : (int)jpegQuality;
		DerivedVersionFile.Publish (source, dest, temp => {
			if (extension == ".png")
				Encode (buffer, temp, SKEncodedImageFormat.Png, 100);
			else
				Encode (buffer, temp, SKEncodedImageFormat.Jpeg, quality);
		});
	}

	public ImageBuffer? LoadThemeIcon (string name, int size)
		=> null;

	public byte[] SerializeIcon (ImageBuffer icon)
		=> GdkPixdata.Write (icon);

	public ImageBuffer DeserializeIcon (byte[] data)
	{
		if (GdkPixdata.TryRead (data, out ImageBuffer icon))
			return icon;
		using var stream = new MemoryStream (data);
		return Decode (stream, ImageOrientation.TopLeft);
	}

	public ThumbnailPng LoadPng (Stream stream)
		=> new ThumbnailPng { Image = Decode (stream, ImageOrientation.TopLeft) };

	public void SavePng (ImageBuffer image, string path, string[] optionKeys, string[] optionValues)
		=> Encode (image, path, SKEncodedImageFormat.Png, 100);

	// Avalonia's headless bitmap Save writes no bytes. Skia is already the
	// production encoder for mail and print, so file output uses it directly.
	static void Encode (ImageBuffer buffer, string path, SKEncodedImageFormat format, int quality)
	{
		using var stream = System.IO.File.Create (path);
		Encode (buffer, stream, format, quality);
	}

	static void Encode (ImageBuffer buffer, Stream stream, SKEncodedImageFormat format, int quality)
	{
		var info = new SKImageInfo (buffer.Width, buffer.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
		using var bitmap = new SKBitmap (info);
		int channels = buffer.HasAlpha ? 4 : 3;
		for (int y = 0; y < buffer.Height; y++) {
			int srcRow = y * buffer.Rowstride;
			for (int x = 0; x < buffer.Width; x++) {
				int src = srcRow + x * channels;
				byte alpha = buffer.HasAlpha ? buffer.Pixels[src + 3] : (byte)255;
				bitmap.SetPixel (x, y, new SKColor (buffer.Pixels[src], buffer.Pixels[src + 1], buffer.Pixels[src + 2], alpha));
			}
		}

		using var image = SKImage.FromBitmap (bitmap);
		using var data = image.Encode (format, quality) ?? throw new InvalidOperationException ("The image encoder produced no data.");
		data.SaveTo (stream);
	}

	internal static Bitmap ApplyOrientation (Bitmap bitmap, ImageOrientation orientation)
	{
		if (orientation is ImageOrientation.None or ImageOrientation.TopLeft)
			return bitmap;

		using (bitmap)
			return ToBgraBitmap (ImageBuffers.TransformOrientation (ToRgbBuffer (bitmap), orientation));
	}

	internal static ImageBuffer ToRgbBuffer (Bitmap bitmap)
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

	internal static WriteableBitmap ToBgraBitmap (ImageBuffer buffer)
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
					byte alpha = buffer.HasAlpha ? buffer.Pixels[src + 3] : (byte)255;
					dstRow[dst] = StraightAlpha.Premultiply (buffer.Pixels[src + 2], alpha);
					dstRow[dst + 1] = StraightAlpha.Premultiply (buffer.Pixels[src + 1], alpha);
					dstRow[dst + 2] = StraightAlpha.Premultiply (buffer.Pixels[src], alpha);
					dstRow[dst + 3] = alpha;
				}
			}
		}

		return bitmap;
	}
}
