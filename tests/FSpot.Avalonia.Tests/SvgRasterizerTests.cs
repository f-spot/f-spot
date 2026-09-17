// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Text;

using FSpot.Avalonia.Imaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SkiaSharp;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class SvgRasterizerTests
{
	const string RedRectSvg = """
		<svg xmlns="http://www.w3.org/2000/svg" width="64" height="32" viewBox="0 0 64 32">
		  <rect width="64" height="32" fill="#ff0000"/>
		</svg>
		""";

	[TestMethod]
	public void IsSvg_TrueForSvgAndSvgzExtensions ()
	{
		using var stream = new MemoryStream (Encoding.UTF8.GetBytes (RedRectSvg));

		Assert.IsTrue (SvgRasterizer.IsSvg ("icon.svg", stream));
		Assert.IsTrue (SvgRasterizer.IsSvg ("icon.SVGZ", stream));
	}

	[TestMethod]
	public void IsSvg_FalseForJpegExtension ()
	{
		using var stream = new MemoryStream (Encoding.UTF8.GetBytes (RedRectSvg));

		Assert.IsFalse (SvgRasterizer.IsSvg ("photo.jpg", stream));
		Assert.IsFalse (SvgRasterizer.IsSvg ("photo.jpg", stream: null));
	}

	[TestMethod]
	public void IsSvg_SniffsSvgMarkupWhenThereIsNoPath ()
	{
		using var stream = new MemoryStream (Encoding.UTF8.GetBytes (RedRectSvg));

		Assert.IsTrue (SvgRasterizer.IsSvg (null, stream));
		Assert.AreEqual (0, stream.Position);
	}

	[TestMethod]
	public void IsSvg_FalseForJpegBytes ()
	{
		using var stream = new MemoryStream (new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 });

		Assert.IsFalse (SvgRasterizer.IsSvg (null, stream));
		Assert.AreEqual (0, stream.Position);
	}

	[TestMethod]
	public void RasterizePng_ScalesTheLongerEdgeToMaxEdgeAndKeepsAspect ()
	{
		using var stream = new MemoryStream (Encoding.UTF8.GetBytes (RedRectSvg));

		byte[] png = SvgRasterizer.RasterizePng (stream, 64);

		using var bitmap = SKBitmap.Decode (png);
		Assert.AreEqual (64, bitmap.Width);
		Assert.AreEqual (32, bitmap.Height);
		SKColor pixel = bitmap.GetPixel (32, 16);
		Assert.AreEqual (255, pixel.Red);
		Assert.AreEqual (0, pixel.Green);
		Assert.AreEqual (0, pixel.Blue);
	}

	[TestMethod]
	public void RasterizePng_ReadsSvgz ()
	{
		using var stream = new MemoryStream (Gzip (Encoding.UTF8.GetBytes (RedRectSvg)));

		byte[] png = SvgRasterizer.RasterizePng (stream, 64);

		using var bitmap = SKBitmap.Decode (png);
		Assert.AreEqual (64, bitmap.Width);
		Assert.AreEqual (32, bitmap.Height);
	}

	[TestMethod]
	public void RasterizePng_CompositesOnTheRequestedBackground ()
	{
		const string diamond = """
			<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 32 32">
			  <rect width="32" height="32" fill="none"/>
			</svg>
			""";
		using var stream = new MemoryStream (Encoding.UTF8.GetBytes (diamond));

		byte[] png = SvgRasterizer.RasterizePng (stream, 32, 0xFFFFFFFF);

		using var bitmap = SKBitmap.Decode (png);
		SKColor pixel = bitmap.GetPixel (16, 16);
		Assert.AreEqual (255, pixel.Red);
		Assert.AreEqual (255, pixel.Green);
		Assert.AreEqual (255, pixel.Blue);
		Assert.AreEqual (255, pixel.Alpha);
	}

	static byte[] Gzip (byte[] data)
	{
		using var output = new MemoryStream ();
		using (var gzip = new GZipStream (output, CompressionLevel.SmallestSize, leaveOpen: true))
			gzip.Write (data);
		return output.ToArray ();
	}
}
