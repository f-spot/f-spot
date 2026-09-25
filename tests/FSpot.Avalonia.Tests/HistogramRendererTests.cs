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

}
