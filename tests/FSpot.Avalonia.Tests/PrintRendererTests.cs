// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

using FSpot.Avalonia.Printing;
using FSpot.Avalonia.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SkiaSharp;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class PrintRendererTests
{
	string directory = string.Empty;

	[TestInitialize]
	public void SetUp ()
	{
		directory = Path.Combine (Path.GetTempPath (), "fspot-print-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (directory);
	}

	[TestCleanup]
	public void TearDown ()
		=> Directory.Delete (directory, recursive: true);

	internal static string WriteJpeg (string directory, string name, int width, int height, SKColor color)
	{
		string path = Path.Combine (directory, name);
		using var bitmap = new SKBitmap (width, height);
		bitmap.Erase (color);
		using var image = SKImage.FromBitmap (bitmap);
		using var data = image.Encode (SKEncodedImageFormat.Jpeg, 90);
		using var stream = File.Create (path);
		data.SaveTo (stream);
		return path;
	}

	static PrintPhoto Photo (string path)
		=> new (path, Path.GetFileName (path), "1/1/2020", "12:00", "Tag", "Comment");

	[UiTestMethod]
	public void WritePdf_WritesOnePagePerSheet ()
	{
		var photos = Enumerable.Range (0, 5)
			.Select (i => Photo (WriteJpeg (directory, $"p{i}.jpg", 64, 48, SKColors.Red)))
			.ToList ();
		var renderer = new PrintRenderer (photos, new PrintOptions { PhotosPerPage = 2, CropMarks = true, PrintFileName = true }, PrintPageSetup.Letter);

		using var stream = new MemoryStream ();
		renderer.WritePdf (stream);

		Assert.AreEqual (3, renderer.PageCount);
		string pdf = Encoding.ASCII.GetString (stream.ToArray ());
		Assert.StartsWith ("%PDF", pdf);
		Assert.Contains ("/Count 3", pdf);
	}

	[UiTestMethod]
	public void RenderPreview_DrawsThePhotoOnAWhiteSheet ()
	{
		var photo = Photo (WriteJpeg (directory, "red.jpg", 300, 400, SKColors.Red));
		var renderer = new PrintRenderer ([photo], new PrintOptions { Fit = PrintFitMode.Zoom }, PrintPageSetup.Letter);

		using var preview = renderer.RenderPreview (0, 360);

		Assert.AreEqual (360, preview.Height);
		var centre = preview.GetPixel (preview.Width / 2, preview.Height / 2);
		Assert.IsGreaterThan (200, (int)centre.Red);
		Assert.IsLessThan (60, (int)centre.Green);
		var corner = preview.GetPixel (1, 1);
		Assert.AreEqual (SKColors.White, corner);
	}

	[UiTestMethod]
	public void RenderPreview_MissingFileStillRenders ()
	{
		var renderer = new PrintRenderer ([Photo (Path.Combine (directory, "gone.jpg"))], new PrintOptions (), PrintPageSetup.Letter);
		using var preview = renderer.RenderPreview (0, 200);
		Assert.AreEqual (200, preview.Height);
	}

	[UiTestMethod]
	public void Load_ShrinksToTheLongerEdge ()
	{
		string path = WriteJpeg (directory, "big.jpg", 1600, 800, SKColors.Blue);
		using var bitmap = PrintImageLoader.Load (path, 400);
		Assert.IsNotNull (bitmap);
		Assert.AreEqual (400, bitmap.Width);
		Assert.AreEqual (200, bitmap.Height);
	}

	[UiTestMethod]
	public void Orient_RightTopTurnsClockwise ()
	{
		var bitmap = new SKBitmap (4, 2);
		bitmap.Erase (SKColors.Black);
		bitmap.SetPixel (0, 0, SKColors.Red);

		using var upright = PrintImageLoader.Orient (bitmap, SKEncodedOrigin.RightTop);

		Assert.AreEqual (2, upright.Width);
		Assert.AreEqual (4, upright.Height);
		Assert.AreEqual (SKColors.Red, upright.GetPixel (1, 0));
	}

	[UiTestMethod]
	public void RotateCounterclockwise_MovesTopLeftToBottomLeft ()
	{
		using var bitmap = new SKBitmap (4, 2);
		bitmap.Erase (SKColors.Black);
		bitmap.SetPixel (0, 0, SKColors.Red);

		using var turned = PrintImageLoader.RotateCounterclockwise (bitmap);

		Assert.AreEqual (2, turned.Width);
		Assert.AreEqual (4, turned.Height);
		Assert.AreEqual (SKColors.Red, turned.GetPixel (0, 3));
	}
}
