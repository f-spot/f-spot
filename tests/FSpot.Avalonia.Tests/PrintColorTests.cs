// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

using FSpot.Avalonia.Printing;
using FSpot.Avalonia.Services;
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

	[TestMethod]
	public void Load_ColourManagesATranslucentPng ()
	{
		string path = Path.Combine (Path.GetTempPath (), "fspot-print-alpha-" + Guid.NewGuid ().ToString ("N") + ".png");
		try {
			using (var encoded = new SKBitmap (new SKImageInfo (1, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul))) {
				encoded.SetPixel (0, 0, new SKColor (255, 0, 0, 128));
				using var image = SKImage.FromBitmap (encoded);
				using var data = image.Encode (SKEncodedImageFormat.Png, 100);
				using var stream = File.Create (path);
				data.SaveTo (stream);
			}

			using var destination = Profile.CreateAlternateRgb ();
			using var straight = new SKBitmap (new SKImageInfo (1, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul));
			straight.SetPixel (0, 0, new SKColor (255, 0, 0, 128));
			PrintImageLoader.ApplyProfile (straight, destination);
			SKColor expected = straight.GetPixel (0, 0);

			using SKBitmap? loaded = PrintImageLoader.Load (path, 16);
			Assert.IsNotNull (loaded);
			PrintImageLoader.ApplyProfile (loaded, destination);
			SKColor actual = loaded.GetPixel (0, 0);

			Assert.AreEqual ((int)expected.Alpha, (int)actual.Alpha);
			Assert.AreEqual ((int)expected.Red, (int)actual.Red);
			Assert.AreEqual ((int)expected.Green, (int)actual.Green);
			Assert.AreEqual ((int)expected.Blue, (int)actual.Blue);
		} finally {
			if (File.Exists (path))
				File.Delete (path);
		}
	}

	[TestMethod]
	public void ApplyProfile_UsesTheSourceProfileRatherThanAssumingSrgb ()
	{
		using var destination = Profile.CreateAlternateRgb ();
		using var embedded = Profile.CreateAlternateRgb ();
		using var asSrgb = Pixel (10, 20, 30, 200);
		using var asEmbedded = Pixel (10, 20, 30, 200);

		PrintImageLoader.ApplyProfile (asSrgb, null, destination);
		PrintImageLoader.ApplyProfile (asEmbedded, embedded, destination);

		SKColor assumed = asSrgb.GetPixel (0, 0);
		SKColor fromFile = asEmbedded.GetPixel (0, 0);
		Assert.AreEqual (200, (int)fromFile.Alpha);
		Assert.IsTrue (fromFile.Red != assumed.Red || fromFile.Green != assumed.Green || fromFile.Blue != assumed.Blue,
			"an embedded profile must change the print differently than treating the file as sRGB");
	}

	[TestMethod]
	public void RenderPreview_UsesAnEmbeddedProfileAsThePrintSource ()
	{
		string directory = Path.Combine (Path.GetTempPath (), "fspot-print-icc-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (directory);
		try {
			byte[] jpeg = Encode (new SKColor (220, 30, 40));
			string plain = Path.Combine (directory, "plain.jpg");
			string tagged = Path.Combine (directory, "tagged.jpg");
			File.WriteAllBytes (plain, jpeg);
			using var embedded = Profile.CreateAlternateRgb ();
			File.WriteAllBytes (tagged, InsertApp2 (jpeg, Save (embedded)));

			using var destination = Profile.CreateAlternateRgb ();
			SKColor without = Centre (plain, destination);
			SKColor with = Centre (tagged, destination);

			Assert.IsTrue (with.Red != without.Red || with.Green != without.Green || with.Blue != without.Blue,
				"printing must use the profile embedded in the file, not sRGB, as the source");
		} finally {
			if (Directory.Exists (directory))
				Directory.Delete (directory, true);
		}
	}

	static SKBitmap Pixel (byte red, byte green, byte blue, byte alpha)
	{
		var bitmap = new SKBitmap (new SKImageInfo (1, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul));
		bitmap.SetPixel (0, 0, new SKColor (red, green, blue, alpha));
		return bitmap;
	}

	static byte[] Encode (SKColor color)
	{
		using var bitmap = new SKBitmap (32, 32);
		bitmap.Erase (color);
		using var image = SKImage.FromBitmap (bitmap);
		using var data = image.Encode (SKEncodedImageFormat.Jpeg, 95);
		return data.ToArray ();
	}

	static byte[] Save (Profile profile)
	{
		profile.Native.Save (null, out uint size);
		var bytes = new byte[size + 1];
		Assert.IsTrue (profile.Native.Save (bytes, out _));
		int length = (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
		return bytes[..length];
	}

	static byte[] InsertApp2 (byte[] jpeg, byte[] profile)
	{
		var payload = new byte[14 + profile.Length];
		Encoding.ASCII.GetBytes ("ICC_PROFILE").CopyTo (payload, 0);
		payload[12] = 1;
		payload[13] = 1;
		profile.CopyTo (payload, 14);
		int markerLength = payload.Length + 2;
		var result = new byte[jpeg.Length + 4 + payload.Length];
		result[0] = jpeg[0];
		result[1] = jpeg[1];
		result[2] = 0xFF;
		result[3] = 0xE2;
		result[4] = (byte)(markerLength >> 8);
		result[5] = (byte)markerLength;
		payload.CopyTo (result, 6);
		Buffer.BlockCopy (jpeg, 2, result, 6 + payload.Length, jpeg.Length - 2);
		return result;
	}

	static SKColor Centre (string path, Profile destination)
	{
		var photo = new PrintPhoto (path, "name", "date", "time", "tags", "comment");
		var renderer = new PrintRenderer (new[] { photo }, new PrintOptions (), PrintPageSetup.Letter, destination);
		using var preview = renderer.RenderPreview (0, 240);
		return preview.GetPixel (preview.Width / 2, preview.Height / 2);
	}
}
