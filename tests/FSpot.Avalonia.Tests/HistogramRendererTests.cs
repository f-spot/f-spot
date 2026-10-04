// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using FSpot.Avalonia.Imaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class HistogramRendererTests
{
	[UiTestMethod]
	public void FromBitmap_DecodedJpeg_IsNotNull ()
	{
		AvaloniaApp.Ensure ();
		string path = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
		using var source = AvaloniaPhotoDecoder.DecodeFile (path, decodeToWidth: 256);
		var histogram = HistogramRenderer.FromBitmap (source, scale: 1);
		Assert.IsNotNull (histogram, $"format={source.Format} alpha={source.AlphaFormat} size={source.PixelSize}");
		histogram!.Dispose ();
	}

	[UiTestMethod]
	public void FromBitmap_DecodedSvg_IsNotNull ()
	{
		AvaloniaApp.Ensure ();
		string svg = """
			<svg xmlns="http://www.w3.org/2000/svg" width="64" height="32" viewBox="0 0 64 32">
			  <rect width="64" height="32" fill="#ff0000"/>
			</svg>
			""";
		string path = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ("N") + ".svg");
		try {
			File.WriteAllText (path, svg);
			using var source = AvaloniaPhotoDecoder.DecodeFile (path, decodeToWidth: 256);
			var histogram = HistogramRenderer.FromBitmap (source, scale: 1);
			Assert.IsNotNull (histogram, $"format={source.Format} alpha={source.AlphaFormat} size={source.PixelSize}");
			histogram!.Dispose ();
		} finally {
			File.Delete (path);
		}
	}

	[UiTestMethod]
	public void FromBitmap_SidebarChartIsOneColumnPerCodeAnd128Tall ()
	{
		AvaloniaApp.Ensure ();
		using var source = Black (8, 8);
		var chart = HistogramRenderer.FromBitmap (source, scale: 1);
		Assert.IsNotNull (chart);
		Assert.AreEqual (256, chart.PixelSize.Width);
		Assert.AreEqual (128, chart.PixelSize.Height);
		// Every pixel is code 0, so only the first column is a bar. The empty
		// column is an opaque black plate, the same as the old sidebar chart.
		byte[] pixels = Pixels (chart);
		Assert.AreEqual (255, pixels[2]);
		Assert.AreEqual (255, pixels[3]);
		Assert.AreEqual (0, pixels[4]);
		Assert.AreEqual (0, pixels[5]);
		Assert.AreEqual (0, pixels[6]);
		Assert.AreEqual (255, pixels[7]);
		chart.Dispose ();
	}

	[UiTestMethod]
	public void FromBitmap_HalfScaleBlendsAdjacentColumns ()
	{
		AvaloniaApp.Ensure ();
		using var source = Black (8, 8);
		var chart = HistogramRenderer.FromBitmap (source, scale: 1, dipWidth: 128, dipHeight: 64);
		Assert.IsNotNull (chart);
		Assert.AreEqual (128, chart.PixelSize.Width);
		Assert.AreEqual (64, chart.PixelSize.Height);
		byte[] pixels = Pixels (chart);
		int bottom = (chart.PixelSize.Height - 1) * chart.PixelSize.Width * 4;
		// A summed pair of bins would stay pure white. Half of 256×128 blends the
		// bar with the opaque black neighbor, so the pixel stays opaque.
		Assert.AreEqual (128, pixels[bottom]);
		Assert.AreEqual (128, pixels[bottom + 1]);
		Assert.AreEqual (128, pixels[bottom + 2]);
		Assert.AreEqual (255, pixels[bottom + 3]);
		chart.Dispose ();
	}

	[UiTestMethod]
	public void FromBitmap_PopupPlateIsGray ()
	{
		AvaloniaApp.Ensure ();
		using var source = Black (8, 8);
		var chart = HistogramRenderer.FromBitmap (source, scale: 1,
			backgroundRed: 127, backgroundGreen: 127, backgroundBlue: 127, backgroundAlpha: 255);
		Assert.IsNotNull (chart);
		byte[] pixels = Pixels (chart);
		Assert.AreEqual (127, pixels[4]);
		Assert.AreEqual (127, pixels[5]);
		Assert.AreEqual (127, pixels[6]);
		Assert.AreEqual (255, pixels[7]);
		chart.Dispose ();
	}

	[UiTestMethod]
	public void FitInside_ShrinksAPortraitSoTheLongEdgeIs256 ()
	{
		AvaloniaApp.Ensure ();
		using var portrait = Black (100, 400);
		using var fitted = HistogramRenderer.FitInside (portrait, 256);
		Assert.AreEqual (64, fitted.PixelSize.Width);
		Assert.AreEqual (256, fitted.PixelSize.Height);

		using var small = Black (50, 50);
		var same = HistogramRenderer.FitInside (small, 256);
		Assert.AreSame (small, same);
	}

	static WriteableBitmap Black (int width, int height)
		=> new (new PixelSize (width, height), new Vector (96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);

	static byte[] Pixels (Bitmap bitmap)
	{
		int width = bitmap.PixelSize.Width;
		int height = bitmap.PixelSize.Height;
		var pixels = new byte[width * height * 4];
		unsafe {
			fixed (byte* pointer = pixels)
				bitmap.CopyPixels (new PixelRect (0, 0, width, height), (nint)pointer, pixels.Length, width * 4);
		}

		return pixels;
	}

}
