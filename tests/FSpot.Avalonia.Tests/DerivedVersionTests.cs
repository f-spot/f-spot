// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.Imaging;
using FSpot.Imaging;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SkiaSharp;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class DerivedVersionTests
{
	[UiTestMethod]
	public void SaveDerivedVersion_KeepsFormatPixelsAndMetadata ()
	{
		string directory = Path.Combine (Path.GetTempPath (), "fspot-derived-" + Guid.NewGuid ().ToString ("n"));
		Directory.CreateDirectory (directory);
		try {
			string sample = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
			string sourcePath = Path.Combine (directory, "source.jpg");
			File.Copy (sample, sourcePath);
			using (var tags = TagLib.File.Create (sourcePath) as TagLib.Image.File) {
				Assert.IsNotNull (tags);
				tags.ImageTag.Comment = "keep-me";
				tags.ImageTag.Orientation = TagLib.Image.ImageOrientation.RightTop;
				tags.Save ();
			}

			var buffer = new ImageBuffer (2, 2, false, 6, new byte[] {
				255, 0, 0, 255, 0, 0,
				255, 0, 0, 255, 0, 0
			});
			var backend = new AvaloniaImagingBackend ();
			var source = new SafeUri (sourcePath);

			string jpegPath = Path.Combine (directory, "edit.jpg");
			backend.SaveDerivedVersion (buffer, source, new SafeUri (jpegPath), 95);
			Assert.IsFalse (File.Exists (Path.Combine (directory, "edit.fspot-tmp.jpg")));
			byte[] jpeg = File.ReadAllBytes (jpegPath);
			Assert.AreEqual (0xFF, jpeg[0]);
			Assert.AreEqual (0xD8, jpeg[1]);
			using (var decoded = SKBitmap.Decode (jpeg)) {
				SKColor pixel = decoded.GetPixel (0, 0);
				Assert.IsGreaterThan (200, pixel.Red);
				Assert.IsLessThan (40, pixel.Green);
				Assert.IsLessThan (40, pixel.Blue);
			}
			using (var saved = TagLib.File.Create (jpegPath) as TagLib.Image.File) {
				Assert.IsNotNull (saved);
				Assert.AreEqual ("keep-me", saved.ImageTag.Comment);
				Assert.AreEqual (TagLib.Image.ImageOrientation.TopLeft, saved.ImageTag.Orientation);
			}

			string pngPath = Path.Combine (directory, "edit.png");
			backend.SaveDerivedVersion (buffer, source, new SafeUri (pngPath), 95);
			byte[] png = File.ReadAllBytes (pngPath);
			Assert.AreEqual (0x89, png[0]);
			Assert.AreEqual ((byte)'P', png[1]);
			Assert.AreEqual ((byte)'N', png[2]);
			Assert.AreEqual ((byte)'G', png[3]);
			using (var decoded = SKBitmap.Decode (png)) {
				SKColor pixel = decoded.GetPixel (0, 0);
				Assert.AreEqual ((byte)255, pixel.Red);
				Assert.AreEqual ((byte)0, pixel.Green);
				Assert.AreEqual ((byte)0, pixel.Blue);
			}

			string gifPath = Path.Combine (directory, "edit.gif");
			Assert.ThrowsExactly<NotImplementedException> (() =>
				backend.SaveDerivedVersion (buffer, source, new SafeUri (gifPath), 95));
			Assert.IsFalse (File.Exists (gifPath));
		} finally {
			Directory.Delete (directory, true);
		}
	}
}
