// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using FSpot.Avalonia.Imaging;
using FSpot.Imaging;
using FSpot.Utils;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SkiaSharp;

using TagLib.Image;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class AvaloniaPhotoDecoderTests
{
	static void EnsureAvalonia ()
		=> AvaloniaApp.Ensure ();

	[UiTestMethod]
	public void ReadOrientation_ReturnsTheExifTagRotateWrites ()
	{
		string path = CopySample ();
		try {
			WriteOrientation (path, ImageOrientation.TopLeft);
			Assert.AreEqual (ImageOrientation.TopLeft, AvaloniaPhotoDecoder.ReadOrientation (path));

			WriteOrientation (path, ImageOrientation.RightTop);
			Assert.AreEqual (ImageOrientation.RightTop, AvaloniaPhotoDecoder.ReadOrientation (path));
		} finally {
			System.IO.File.Delete (path);
		}
	}

	[UiTestMethod]
	public void ApplyOrientation_RightTop_SwapsWidthAndHeight ()
	{
		EnsureAvalonia ();
		using var source = CreateUniqueBitmap ();
		using var rotated = AvaloniaImagingBackend.ApplyOrientation (source, ImageOrientation.RightTop);

		Assert.AreEqual (4, rotated.PixelSize.Width);
		Assert.AreEqual (8, rotated.PixelSize.Height);
	}

	[UiTestMethod]
	public void DecodeFile_FitsATallImageInsideTheBox ()
	{
		EnsureAvalonia ();
		string path = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ("N") + ".png");
		try {
			WriteSolidPng (path, 2, 2000);
			using var bitmap = AvaloniaPhotoDecoder.DecodeFile (path, decodeToWidth: 256);
			Assert.IsLessThanOrEqualTo (256, bitmap.PixelSize.Width);
			Assert.IsLessThanOrEqualTo (256, bitmap.PixelSize.Height);
			Assert.IsGreaterThan (0, bitmap.PixelSize.Width);
			Assert.IsGreaterThan (0, bitmap.PixelSize.Height);
		} finally {
			System.IO.File.Delete (path);
		}
	}

	[UiTestMethod]
	public void DecodeFile_RefusesAnImageOverThePixelCap ()
	{
		EnsureAvalonia ();
		string path = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ("N") + ".png");
		try {
			WritePngHeader (path, 20000, 20000);
			var missed = Assert.ThrowsExactly<InvalidDataException> (() => AvaloniaPhotoDecoder.DecodeFile (path));
			Assert.AreEqual (DecodeBudget.TooLargeMessage, missed.Message);
		} finally {
			System.IO.File.Delete (path);
		}
	}

	[UiTestMethod]
	public void DecodeFile_RefusesASmallImageWhenTheSharedBudgetSaysSo ()
	{
		EnsureAvalonia ();
		string path = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ("N") + ".png");
		try {
			WriteSolidPng (path, 4, 4);
			using var limit = DecodeBudget.LimitForTest (1);
			var missed = Assert.ThrowsExactly<InvalidDataException> (() => AvaloniaPhotoDecoder.DecodeFile (path));
			Assert.AreEqual (DecodeBudget.TooLargeMessage, missed.Message);
		} finally {
			System.IO.File.Delete (path);
		}
	}

	[UiTestMethod]
	public void DecodeFile_AppliesTheExifOrientationTag ()
	{
		EnsureAvalonia ();
		string path = CopySample ();
		try {
			WriteOrientation (path, ImageOrientation.TopLeft);
			using var original = AvaloniaPhotoDecoder.DecodeFile (path, decodeToWidth: 200);

			WriteOrientation (path, ImageOrientation.RightTop);
			using var rotated = AvaloniaPhotoDecoder.DecodeFile (path, decodeToWidth: 200);

			Assert.AreEqual (original.PixelSize.Height, rotated.PixelSize.Width);
			Assert.AreEqual (original.PixelSize.Width, rotated.PixelSize.Height);
		} finally {
			System.IO.File.Delete (path);
		}
	}

	static WriteableBitmap CreateUniqueBitmap ()
	{
		var bitmap = new WriteableBitmap (
			new PixelSize (8, 4),
			new global::Avalonia.Vector (96, 96),
			PixelFormat.Bgra8888,
			AlphaFormat.Unpremul);
		using var locked = bitmap.Lock ();
		unsafe {
			var dest = (byte*)locked.Address;
			int stride = locked.RowBytes;
			for (int i = 0; i < stride * 4; i++)
				dest[i] = 0;
			WriteBgra (dest, stride, 0, 0, 255, 0, 0);
			WriteBgra (dest, stride, 7, 0, 0, 255, 0);
			WriteBgra (dest, stride, 0, 3, 0, 0, 255);
			WriteBgra (dest, stride, 7, 3, 255, 255, 255);
		}

		return bitmap;
	}

	static unsafe void WriteBgra (byte* dest, int stride, int x, int y, byte r, byte g, byte b)
	{
		byte* pixel = dest + y * stride + x * 4;
		pixel[0] = b;
		pixel[1] = g;
		pixel[2] = r;
		pixel[3] = 255;
	}

	static void WriteSolidPng (string path, int width, int height)
	{
		var info = new SKImageInfo (width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
		using var bitmap = new SKBitmap (info);
		bitmap.Erase (SKColors.Red);
		using var image = SKImage.FromBitmap (bitmap);
		using var data = image.Encode (SKEncodedImageFormat.Png, 100);
		using var stream = System.IO.File.Create (path);
		data.SaveTo (stream);
	}

	static void WritePngHeader (string path, int width, int height)
	{
		using var stream = System.IO.File.Create (path);
		stream.Write (new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
		var ihdr = new byte[13];
		WriteInt (ihdr, 0, width);
		WriteInt (ihdr, 4, height);
		ihdr[8] = 8;
		ihdr[9] = 2;
		WriteChunk (stream, "IHDR"u8, ihdr);
		// Skia will not open a PNG that has no IDAT, so the header never reaches the cap.
		WriteChunk (stream, "IDAT"u8, Array.Empty<byte> ());
		WriteChunk (stream, "IEND"u8, Array.Empty<byte> ());
	}

	static void WriteChunk (Stream stream, ReadOnlySpan<byte> type, byte[] data)
	{
		Span<byte> length = stackalloc byte[4];
		WriteInt (length, 0, data.Length);
		stream.Write (length);
		stream.Write (type);
		stream.Write (data);
		var crcInput = new byte[type.Length + data.Length];
		type.CopyTo (crcInput);
		data.CopyTo (crcInput.AsSpan (type.Length));
		Span<byte> crc = stackalloc byte[4];
		WriteInt (crc, 0, (int)Crc32 (crcInput));
		stream.Write (crc);
	}

	static void WriteInt (Span<byte> buffer, int offset, int value)
	{
		buffer[offset] = (byte)(value >> 24);
		buffer[offset + 1] = (byte)(value >> 16);
		buffer[offset + 2] = (byte)(value >> 8);
		buffer[offset + 3] = (byte)value;
	}

	static uint Crc32 (byte[] data)
	{
		uint crc = 0xFFFFFFFF;
		foreach (byte value in data) {
			crc ^= value;
			for (int bit = 0; bit < 8; bit++)
				crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
		}

		return ~crc;
	}

	static string CopySample ()
	{
		string source = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
		string path = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ("N") + ".jpg");
		System.IO.File.Copy (source, path, true);
		return path;
	}

	static void WriteOrientation (string path, ImageOrientation orientation)
	{
		using var metadata = MetadataUtils.Parse (new SafeUri (path));
		Assert.IsNotNull (metadata);
		metadata.EnsureAvailableTags ();
		metadata.ImageTag.Orientation = orientation;
		metadata.Save ();
	}
}
