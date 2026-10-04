// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using FSpot.Avalonia;
using FSpot.Avalonia.Imaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class HiDpiTests
{
	[UiTestMethod]
	[DoNotParallelize]
	public void Histogram_AtScale2_StaysThe256By128Chart ()
	{
		AvaloniaApp.Ensure ();
		using var source = new WriteableBitmap (new PixelSize (8, 8), new Vector (96, 96),
			PixelFormat.Bgra8888, AlphaFormat.Premul);
		var one = HistogramRenderer.FromBitmap (source, scale: 1);
		var two = HistogramRenderer.FromBitmap (source, scale: 2);
		Assert.IsNotNull (one);
		Assert.IsNotNull (two);
		Assert.AreEqual (256, one!.PixelSize.Width);
		Assert.AreEqual (128, one.PixelSize.Height);
		// Enlarging 256 columns to 512 blends every code with its neighbor.
		// The view scales the 256×128 chart once.
		Assert.AreEqual (256, two!.PixelSize.Width);
		Assert.AreEqual (128, two.PixelSize.Height);
		Assert.AreEqual (96, two.Dpi.X, 0.1);
		byte[] pixels = new byte[4];
		unsafe {
			fixed (byte* pointer = pixels)
				two.CopyPixels (new PixelRect (1, 0, 1, 1), (nint)pointer, pixels.Length, 4);
		}
		Assert.AreEqual (0, pixels[0]);
		Assert.AreEqual (0, pixels[2]);
		Assert.AreEqual (255, pixels[3]);
	}

	[UiTestMethod]
	public void PixelSize_IsDipTimesScale ()
	{
		Assert.AreEqual (22, HiDpi.PixelSize (22, 1));
		Assert.AreEqual (44, HiDpi.PixelSize (22, 2));
		Assert.AreEqual (33, HiDpi.PixelSize (22, 1.5));
		Assert.AreEqual (1, HiDpi.PixelSize (0, 2));
		Assert.AreEqual (16, HiDpi.PixelSize (16, 0));
	}
}
