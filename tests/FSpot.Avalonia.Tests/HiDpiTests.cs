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
	public void Histogram_AtScale2_IsAtLeastTwiceThe1xBitmap ()
	{
		AvaloniaApp.Ensure ();
		using var source = new WriteableBitmap (new PixelSize (8, 8), new Vector (96, 96),
			PixelFormat.Bgra8888, AlphaFormat.Premul);
		var one = HistogramRenderer.FromBitmap (source, scale: 1);
		var two = HistogramRenderer.FromBitmap (source, scale: 2);
		Assert.IsNotNull (one);
		Assert.IsNotNull (two);
		Assert.IsGreaterThanOrEqualTo (one!.PixelSize.Width * 2, two!.PixelSize.Width);
		Assert.IsGreaterThanOrEqualTo (one.PixelSize.Height * 2, two.PixelSize.Height);
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
