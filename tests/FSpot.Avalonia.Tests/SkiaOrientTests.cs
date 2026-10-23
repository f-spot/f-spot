// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.Imaging;
using FSpot.Imaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SkiaSharp;

using TagLib.Image;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class SkiaOrientTests
{
	[TestMethod]
	public void Apply_MatchesTheSharedOrientationForEveryTurn ()
	{
		var pixels = new byte[2 * 3 * 3];
		for (int i = 0; i < 6; i++) {
			pixels[i * 3] = (byte)(10 * (i + 1));
			pixels[i * 3 + 1] = (byte)(i + 1);
			pixels[i * 3 + 2] = 2;
		}

		var source = new ImageBuffer (2, 3, false, 6, pixels);
		foreach (ImageOrientation orientation in Enum.GetValues<ImageOrientation> ()) {
			if (orientation == ImageOrientation.None)
				continue;
			var expected = ImageBuffers.TransformOrientation (source, orientation);
			using var bitmap = Paint (pixels);
			using var oriented = SkiaOrient.Apply (bitmap, orientation);
			Assert.AreEqual (expected.Width, oriented.Width, orientation.ToString ());
			Assert.AreEqual (expected.Height, oriented.Height, orientation.ToString ());
			Assert.AreEqual (expected.Pixels[0], oriented.GetPixel (0, 0).Red, orientation.ToString ());
			int last = (expected.Height - 1) * expected.Rowstride + (expected.Width - 1) * 3;
			Assert.AreEqual (expected.Pixels[last], oriented.GetPixel (expected.Width - 1, expected.Height - 1).Red, orientation.ToString ());
		}
	}

	static SKBitmap Paint (byte[] rgb)
	{
		var bitmap = new SKBitmap (new SKImageInfo (2, 3, SKColorType.Rgba8888, SKAlphaType.Opaque));
		for (int i = 0; i < 6; i++)
			bitmap.SetPixel (i % 2, i / 2, new SKColor (rgb[i * 3], rgb[i * 3 + 1], rgb[i * 3 + 2]));
		return bitmap;
	}
}
