// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.Printing;
using FSpot.Cms;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SkiaSharp;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class PrintColorTests
{
	[TestInitialize]
	public void RequireLcms ()
	{
		if (!LcmsProbe.IsAvailable ())
			Assert.Inconclusive ("liblcms2 is not available on this machine");
	}

	[TestMethod]
	public void ApplyProfile_ColourManagesAStraightAlphaPixelAndKeepsItsAlpha ()
	{
		using var destination = Profile.CreateAlternateRgb ();
		using var bitmap = new SKBitmap (new SKImageInfo (1, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul));
		bitmap.SetPixel (0, 0, new SKColor (10, 20, 30, 200));

		PrintImageLoader.ApplyProfile (bitmap, destination);

		SKColor pixel = bitmap.GetPixel (0, 0);
		Assert.AreEqual (200, (int)pixel.Alpha);
		Assert.IsTrue (pixel.Red != 10 || pixel.Green != 20 || pixel.Blue != 30,
			"the pixel should have been converted, not left as the source");
	}
}
