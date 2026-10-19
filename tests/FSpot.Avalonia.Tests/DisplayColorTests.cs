// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.Imaging;
using FSpot.Cms;
using FSpot.Imaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class DisplayColorTests
{
	[TestInitialize]
	public void RequireLcms ()
	{
		if (!LcmsProbe.IsAvailable ())
			Assert.Inconclusive ("liblcms2 is not available on this machine");
	}

	[TestMethod]
	public void Apply_LeavesTheBufferWhenNoProfileIsChosen ()
	{
		var buffer = Rgba (10, 20, 30, 200);

		var shown = DisplayColor.Apply (buffer, destination: null);

		Assert.AreSame (buffer, shown);
		Assert.AreEqual (10, shown.Pixels[0]);
		Assert.AreEqual (200, shown.Pixels[3]);
	}

	[TestMethod]
	public void Apply_ConvertsAStraightPixelAndKeepsItsAlpha ()
	{
		using var destination = Profile.CreateAlternateRgb ();
		var buffer = Rgba (10, 20, 30, 200);

		var shown = DisplayColor.Apply (buffer, destination);

		Assert.AreNotSame (buffer, shown);
		Assert.AreEqual (10, buffer.Pixels[0]);
		Assert.AreEqual (20, buffer.Pixels[1]);
		Assert.AreEqual (30, buffer.Pixels[2]);
		Assert.AreEqual (200, buffer.Pixels[3]);
		Assert.AreEqual (200, shown.Pixels[3]);
		Assert.IsTrue (shown.Pixels[0] != 10 || shown.Pixels[1] != 20 || shown.Pixels[2] != 30,
			"the pixel should have been converted, not left as the source");
	}

	[TestMethod]
	public void Apply_ConvertsThePixelAndLeavesPaddingOnTheRow ()
	{
		using var destination = Profile.CreateAlternateRgb ();
		var pixels = new byte[] { 10, 20, 30, 0, 0 };
		var buffer = new ImageBuffer (1, 1, false, 5, pixels);

		var shown = DisplayColor.Apply (buffer, destination);

		Assert.AreEqual (10, buffer.Pixels[0]);
		Assert.AreEqual (0, shown.Pixels[3]);
		Assert.AreEqual (0, shown.Pixels[4]);
		Assert.IsTrue (shown.Pixels[0] != 10 || shown.Pixels[1] != 20 || shown.Pixels[2] != 30);
	}

	[TestMethod]
	public void Resolve_UsesTheSystemProfileOnlyForItsSentinel ()
	{
		using var destination = Profile.CreateAlternateRgb ();

		Assert.IsNull (DisplayColor.Resolve (null, _ => destination, destination));
		Assert.IsNull (DisplayColor.Resolve ("", _ => destination, destination));
		Assert.AreSame (destination, DisplayColor.Resolve (SystemColorProfile.Sentinel, _ => null, destination));
		Assert.IsNull (DisplayColor.Resolve ("missing", _ => null, destination));
		Assert.AreSame (destination, DisplayColor.Resolve ("Adobe RGB", name => name == "Adobe RGB" ? destination : null, null));
	}

	[TestMethod]
	public void UnpremultiplyRgba_RestoresStraightRedFromAPremulFramebuffer ()
	{
		var rgba = new byte[] { 128, 0, 0, 128 };

		StraightAlpha.UnpremultiplyRgba (rgba);

		Assert.AreEqual (255, (int)rgba[0]);
		Assert.AreEqual (0, (int)rgba[1]);
		Assert.AreEqual (0, (int)rgba[2]);
		Assert.AreEqual (128, (int)rgba[3]);
	}

	[TestMethod]
	public void Premultiply_StoresStraightRedAsPremulAndLeavesAnOpaquePixel ()
	{
		Assert.AreEqual (128, (int)StraightAlpha.Premultiply (255, 128));
		Assert.AreEqual (0, (int)StraightAlpha.Premultiply (0, 128));
		Assert.AreEqual (10, (int)StraightAlpha.Premultiply (10, 255));
		Assert.AreEqual (0, (int)StraightAlpha.Premultiply (10, 0));
	}

	static ImageBuffer Rgba (byte r, byte g, byte b, byte a)
		=> new (1, 1, true, 4, new[] { r, g, b, a });
}
