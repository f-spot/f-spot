//
// CmsFacadeTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Cms.UnitTest
{
	// These reach a real liblcms2 through the FSpot.Cms surface the rest of F-Spot
	// uses, so they exercise the binding layer itself rather than the maths.
	[TestClass]
	public class CmsFacadeTests
	{
		[TestInitialize]
		public void RequireLcms ()
		{
			LcmsNative.Require ();
		}

		// ColorManagement keys its profile dictionary on this string. The old LPWStr
		// marshalling read it as "s", collapsing every profile onto one letter.
		[TestMethod]
		public void StandardRgbProfileReportsItsFullDescription ()
		{
			var srgb = Profile.CreateStandardRgb ();

			Assert.AreEqual ("sRGB built-in", srgb.ProductDescription);
			Assert.AreEqual (IccColorSpace.Rgb, srgb.ColorSpace);
			Assert.AreEqual (IccProfileClass.Display, srgb.DeviceClass);
		}

		[TestMethod]
		public void StandardRgbProfileIsSharedAndSurvivesDisposal ()
		{
			var first = Profile.CreateStandardRgb ();
			first.Dispose ();
			var second = Profile.CreateStandardRgb ();

			Assert.AreSame (first, second);
			Assert.AreEqual ("sRGB built-in", second.ProductDescription, "disposing the shared profile must not close it");
		}

		[TestMethod]
		public void AlternateRgbProfileIsAnRgbDisplayProfileWithADescription ()
		{
			using var adobe = Profile.CreateAlternateRgb ();

			Assert.AreEqual (IccColorSpace.Rgb, adobe.ColorSpace);
			Assert.AreEqual (IccProfileClass.Display, adobe.DeviceClass);
			Assert.IsNotEmpty (adobe.ProductDescription);
			Assert.AreNotEqual (Profile.CreateStandardRgb ().ProductDescription, adobe.ProductDescription,
				"the two built-in profiles must not collide in ColorManagement's dictionary");
		}

		// AddProfilesByExtension catches CmsException per file; anything else aborts the
		// directory walk.
		[TestMethod]
		public void GarbageBytesThrowCmsException ()
		{
			var garbage = new byte[512];
			for (int i = 0; i < garbage.Length; i++)
				garbage[i] = (byte)(i * 7);

			Assert.ThrowsExactly<CmsException> (() => new Profile (garbage));
		}

		[TestMethod]
		public void ProfileCanBeOpenedFromASliceOfALargerBuffer ()
		{
			byte[] icc = SaveStandardRgb ();
			var padded = new byte[icc.Length + 64];
			Array.Copy (icc, 0, padded, 32, icc.Length);

			using var profile = new Profile (padded, 32, icc.Length);

			Assert.AreEqual ("sRGB built-in", profile.ProductDescription);
		}

		// The old Transform(Profile[]) marshalled HandleRef[] and could never work; this is
		// the path behind every colour-managed operation in F-Spot.
		[TestMethod]
		public void MultiprofileTransformBetweenTwoStandardRgbProfilesIsTheIdentity ()
		{
			var srgb = Profile.CreateStandardRgb ();
			byte[] pixels = { 0, 0, 0, 255, 255, 255, 12, 200, 77, 128, 128, 128 };

			using var transform = new Transform (new[] { srgb, srgb }, Format.Rgb8, Format.Rgb8, Intent.Perceptual, 0);
			byte[] output = Apply (transform, pixels, 3, pixels.Length / 3);

			Assert.AreSequenceEqual (pixels, output);
		}

		[TestMethod]
		public void TransformConvertsInPlaceWhenInputAndOutputAlias ()
		{
			var srgb = Profile.CreateStandardRgb ();
			using var adobe = Profile.CreateAlternateRgb ();
			byte[] pixels = { 255, 0, 0, 0, 255, 0, 0, 0, 255, 200, 100, 50 };
			using var reference = new Transform (srgb, Format.Rgb8, adobe, Format.Rgb8, Intent.Perceptual, 0);
			byte[] expected = Apply (reference, pixels, 3, 4);

			using var transform = new Transform (new[] { srgb, adobe }, Format.Rgb8, Format.Rgb8, Intent.Perceptual, 0);
			IntPtr buffer = Marshal.AllocHGlobal (pixels.Length);
			try {
				Marshal.Copy (pixels, 0, buffer, pixels.Length);
				transform.Apply (buffer, buffer, 4);
				var actual = new byte[pixels.Length];
				Marshal.Copy (buffer, actual, 0, actual.Length);

				Assert.AreSequenceEqual (expected, actual);
			} finally {
				Marshal.FreeHGlobal (buffer);
			}

			Assert.AreNotEqual (pixels[0], expected[0], "sRGB red should not survive unchanged into AdobeRGB");
		}

		// Adjustment.Adjust closes the abstract profiles it generated as soon as the
		// transform exists -- one 20x20x20 CLUT per editor slider drag, otherwise left to
		// the finalizer. That is only safe because lcms copies the pipeline into the
		// transform, so this pins the assumption the disposal rests on.
		[TestMethod]
		public void TransformKeepsWorkingAfterItsSourceProfilesAreDisposed ()
		{
			var srgb = Profile.CreateStandardRgb ();
			var desaturate = Profile.CreateAbstract (20, 1.0, 0.0, 0.0, 0.0, -100.0, null,
				ColorCIExyY.D50, ColorCIExyY.D50);
			byte[] pixels = { 255, 0, 0, 0, 255, 0, 0, 0, 255, 200, 100, 50 };

			using var transform = new Transform (new[] { srgb, desaturate, srgb }, Format.Rgb8, Format.Rgb8,
				Intent.Perceptual, 0);
			byte[] before = Apply (transform, pixels, 3, pixels.Length / 3);
			desaturate.Dispose ();
			byte[] after = Apply (transform, pixels, 3, pixels.Length / 3);

			Assert.AreNotEqual (pixels[0], before[0], "the abstract profile should have changed the pixels");
			Assert.AreSequenceEqual (before, after);
		}

		[TestMethod]
		public void TransformWithMismatchedProfilesThrowsCmsExceptionInsteadOfCrashing ()
		{
			// A linearisation device link is RGB->RGB; asking for Lab input has no path.
			using var line = new ToneCurve (1.0);
			using var link = new Profile (IccColorSpace.Rgb, new[] { line, line, line });

			Assert.ThrowsExactly<CmsException> (() =>
				new Transform (new[] { link }, Format.Lab16, Format.Lab16, Intent.Perceptual, 0));
		}

		// AutoStretch builds a per-channel stretch as a linearisation device link.
		[TestMethod]
		public void LinearizationDeviceLinkAppliesItsTables ()
		{
			var invert = new ushort[256];
			for (int i = 0; i < invert.Length; i++)
				invert[i] = (ushort)(65535 - i * 257);
			using var inverting = new ToneCurve (invert);
			using var identity = new ToneCurve (1.0);
			using var link = new Profile (IccColorSpace.Rgb, new[] { inverting, identity, identity });

			Assert.AreEqual (IccProfileClass.Link, link.DeviceClass);

			using var transform = new Transform (new[] { link }, Format.Rgb8, Format.Rgb8, Intent.Perceptual, 0);
			byte[] output = Apply (transform, new byte[] { 0, 10, 20, 255, 10, 20 }, 3, 2);

			Assert.AreSequenceEqual (new byte[] { 255, 10, 20, 0, 10, 20 }, output);
		}

		[TestMethod]
		public void WhitePointConversionsRoundTripThroughXyz ()
		{
			var d50 = ColorCIExyY.D50;
			ColorCIEXYZ xyz = d50.ToXYZ ();
			ColorCIExyY back = xyz.ToxyY ();

			Assert.AreEqual (d50.x, back.x, 1e-9);
			Assert.AreEqual (d50.y, back.y, 1e-9);
			Assert.AreEqual (1.0, xyz.y, 1e-9, "a white point has Y = 1");

			// The white point of its own adaptation is L = 100, neutral.
			ColorCIELab lab = xyz.ToLab (xyz);
			Assert.AreEqual (100.0, lab.L, 1e-6);
			Assert.AreEqual (0.0, lab.a, 1e-6);
			Assert.AreEqual (0.0, lab.b, 1e-6);
			ColorCIEXYZ again = lab.ToXYZ (xyz);
			Assert.AreEqual (xyz.x, again.x, 1e-9);
		}

		internal static byte[] Apply (Transform transform, byte[] input, int bytesPerPixel, int pixels)
		{
			IntPtr source = Marshal.AllocHGlobal (input.Length);
			IntPtr destination = Marshal.AllocHGlobal (pixels * bytesPerPixel);
			try {
				Marshal.Copy (input, 0, source, input.Length);
				transform.Apply (source, destination, (uint)pixels);
				var output = new byte[pixels * bytesPerPixel];
				Marshal.Copy (destination, output, 0, output.Length);
				return output;
			} finally {
				Marshal.FreeHGlobal (source);
				Marshal.FreeHGlobal (destination);
			}
		}

		static byte[] SaveStandardRgb ()
		{
			using var srgb = lcmsNET.Profile.Create_sRGB ();
			srgb.Save (null, out uint needed);
			var bytes = new byte[needed + 1];
			Assert.IsTrue (srgb.Save (bytes, out needed));
			Array.Resize (ref bytes, (int)needed);
			return bytes;
		}
	}
}
