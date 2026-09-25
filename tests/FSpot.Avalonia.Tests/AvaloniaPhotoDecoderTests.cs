// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using FSpot.Avalonia.Imaging;
using FSpot.Utils;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

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
	public void DecodeFile_AppliesTheExifOrientationTag ()
	{
		EnsureAvalonia ();
		string path = CopySample ();
		try {
			WriteOrientation (path, ImageOrientation.TopLeft);
			using var original = AvaloniaPhotoDecoder.DecodeFile (path, decodeToWidth: 200);
			byte[] topLeft = Pixel (original, 0, 0);

			WriteOrientation (path, ImageOrientation.BottomRight);
			using var rotated = AvaloniaPhotoDecoder.DecodeFile (path, decodeToWidth: 200);
			byte[] rotatedTopLeft = Pixel (rotated, 0, 0);

			Assert.AreEqual (original.PixelSize.Width, rotated.PixelSize.Width);
			Assert.AreEqual (original.PixelSize.Height, rotated.PixelSize.Height);
			Assert.AreNotSequenceEqual (topLeft, rotatedTopLeft);
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

	static byte[] Pixel (Bitmap bitmap, int x, int y)
	{
		var data = new byte[4];
		unsafe {
			fixed (byte* pointer = data)
				bitmap.CopyPixels (new global::Avalonia.PixelRect (x, y, 1, 1), (nint)pointer, data.Length, 4);
		}

		return data;
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
