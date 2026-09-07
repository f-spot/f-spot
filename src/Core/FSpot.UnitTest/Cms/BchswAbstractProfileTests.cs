//
// BchswAbstractProfileTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

using lcmsNET;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Cms.UnitTest
{
	// Golden tests for the C# port of f_cmsCreateBCHSWabstractProfile. The expected
	// values in BchswGoldenData were captured from the original C code linked against
	// liblcms2 2.19.1 (see the generator notes in that file). A wrong-colour regression
	// in the port -- a transposed CLUT, a dropped stage, an unmarshalled sampler --
	// would otherwise be invisible: lcms happily builds and applies a garbage profile.
	//
	// On the machine the goldens were captured on the match is bit-exact. The
	// tolerances below (one 8-bit step, two 16-bit steps) leave room for a libm that
	// rounds pow() differently in the last place, and are still a hundred times tighter
	// than any regression this guards against.
	[TestClass]
	public class BchswAbstractProfileTests
	{
		const int Rgb8Tolerance = 1;
		const int TableTolerance = 2;

		[TestInitialize]
		public void RequireLcms ()
		{
			LcmsNative.Require ();
		}

		public static IEnumerable<object[]> Cases ()
		{
			return BchswGoldenData.Cases.Select (c => new object[] { c });
		}

		public static string CaseName (System.Reflection.MethodInfo method, object[] data)
		{
			return $"{method.Name} ({((BchswGoldenData.Case)data[0]).Name})";
		}

		[TestMethod]
		[DynamicData (nameof (Cases), DynamicDataDisplayName = nameof (CaseName))]
		public void ClutTableMatchesTheCImplementation (BchswGoldenData.Case golden)
		{
			ushort[] table = BchswAbstractProfile.FillTable (golden.GridPoints, Adjustments (golden));

			Assert.HasCount (golden.TableLength, table);

			var mismatches = new List<string> ();
			for (int i = 0; i < golden.TableSamples.Length; i++) {
				int index = i * golden.TableSampleStride;
				int actual = table[index];
				int expected = golden.TableSamples[i];
				if (Math.Abs (actual - expected) > TableTolerance)
					mismatches.Add ($"[{index}] expected {expected} got {actual}");
			}

			Assert.IsEmpty (mismatches, $"{mismatches.Count} of {golden.TableSamples.Length} sampled nodes differ: "
				+ string.Join ("; ", mismatches.Take (8)));
		}

		// The strided comparison above never reaches a* or b*: on the five 20-grid cases
		// the stride is 120, a multiple of 3, so every sampled index is congruent to 0 mod
		// 3 and only L* is compared -- while Hue and Saturation move chroma, not lightness.
		// Hashing the whole table pins all 24 000 entries and makes the stride irrelevant.
		[TestMethod]
		[DynamicData (nameof (Cases), DynamicDataDisplayName = nameof (CaseName))]
		public void ClutTableHashMatchesTheCImplementation (BchswGoldenData.Case golden)
		{
			ushort[] table = BchswAbstractProfile.FillTable (golden.GridPoints, Adjustments (golden));

			Assert.AreEqual (golden.TableSha256, Sha256 (table),
				"the table differs from the C implementation somewhere the strided sample does not look");
		}

		// The goldens serialise the table as little-endian ushorts.
		static string Sha256 (ushort[] table)
		{
			var bytes = new byte[table.Length * sizeof (ushort)];
			Buffer.BlockCopy (table, 0, bytes, 0, bytes.Length);

			return Convert.ToHexString (SHA256.HashData (bytes)).ToLowerInvariant ();
		}

		[TestMethod]
		[DynamicData (nameof (Cases), DynamicDataDisplayName = nameof (CaseName))]
		public void TransformThroughTheAbstractProfileMatchesTheCImplementation (BchswGoldenData.Case golden)
		{
			byte[] expected = FromHex (golden.Rgb8OutHex);
			byte[] input = BchswGoldenData.Rgb8Probes;
			Assert.HasCount (input.Length, expected);

			byte[] actual = new byte[input.Length];
			using (var srgb = lcmsNET.Profile.Create_sRGB ())
			using (var abstractProfile = CreateProfile (golden))
			using (var transform = lcmsNET.Transform.Create (new[] { srgb, abstractProfile, srgb },
				lcmsNET.Cms.TYPE_RGB_8, lcmsNET.Cms.TYPE_RGB_8, lcmsNET.Intent.Perceptual, (CmsFlags)golden.Flags)) {
				transform.DoTransform (input, actual, input.Length / 3);
			}

			var mismatches = new List<string> ();
			for (int i = 0; i < expected.Length; i++)
				if (Math.Abs (actual[i] - expected[i]) > Rgb8Tolerance)
					mismatches.Add ($"pixel {i / 3} channel {i % 3}: expected {expected[i]} got {actual[i]}");

			Assert.IsEmpty (mismatches, $"{mismatches.Count} of {expected.Length} channel values differ: "
				+ string.Join ("; ", mismatches.Take (8)));
		}

		// The same comparison through the surface F-Spot's editors use: Profile.CreateAbstract
		// with the default tables, FSpot.Cms.Transform, and Apply on unmanaged memory.
		[TestMethod]
		[DynamicData (nameof (Cases), DynamicDataDisplayName = nameof (CaseName))]
		public void ProfileCreateAbstractMatchesTheCImplementation (BchswGoldenData.Case golden)
		{
			byte[] expected = FromHex (golden.Rgb8OutHex);
			var srgb = Profile.CreateStandardRgb ();
			using var abstractProfile = Profile.CreateAbstract (golden.GridPoints, golden.Exposure, golden.Bright,
				golden.Contrast, golden.Hue, golden.Saturation, null,
				new ColorCIExyY (golden.SrcX, golden.SrcY, 1.0), new ColorCIExyY (golden.DestX, golden.DestY, 1.0));
			using var transform = new Transform (new[] { srgb, abstractProfile, srgb }, Format.Rgb8, Format.Rgb8,
				Intent.Perceptual, golden.Flags);

			byte[] actual = CmsFacadeTests.Apply (transform, BchswGoldenData.Rgb8Probes, 3, BchswGoldenData.Rgb8Probes.Length / 3);

			var mismatches = new List<string> ();
			for (int i = 0; i < expected.Length; i++)
				if (Math.Abs (actual[i] - expected[i]) > Rgb8Tolerance)
					mismatches.Add ($"pixel {i / 3} channel {i % 3}: expected {expected[i]} got {actual[i]}");

			Assert.IsEmpty (mismatches, $"{mismatches.Count} of {expected.Length} channel values differ: "
				+ string.Join ("; ", mismatches.Take (8)));
		}

		// Guards against the failure mode of the unpatched lcmsNET sampler: a profile that
		// builds fine and maps every colour to the same value.
		[TestMethod]
		public void DesaturationProfileStillDistinguishesLuminance ()
		{
			var golden = BchswGoldenData.Cases.Single (c => c.Name == "Desaturate");
			byte[] output = new byte[BchswGoldenData.Rgb8Probes.Length];
			using (var srgb = lcmsNET.Profile.Create_sRGB ())
			using (var abstractProfile = CreateProfile (golden))
			using (var transform = lcmsNET.Transform.Create (new[] { srgb, abstractProfile, srgb },
				lcmsNET.Cms.TYPE_RGB_8, lcmsNET.Cms.TYPE_RGB_8, lcmsNET.Intent.Perceptual, CmsFlags.None)) {
				transform.DoTransform (BchswGoldenData.Rgb8Probes, output, output.Length / 3);
			}

			int distinctReds = output.Where ((_, i) => i % 3 == 0).Distinct ().Count ();
			Assert.IsGreaterThan (20, distinctReds, "a CLUT that took effect maps different luminances to different values");

			// Saturation -100 only removes 100 units of chroma, so vivid inputs keep some;
			// the neutral axis, though, must come back neutral and in luminance order.
			int previous = -1;
			for (int level = 0; level < 5; level++) {
				int pixel = level * 25 + level * 5 + level;
				int r = output[pixel * 3], g = output[pixel * 3 + 1], b = output[pixel * 3 + 2];
				Assert.IsLessThanOrEqualTo (2, Math.Abs (r - g), $"grey input {level} came out tinted");
				Assert.IsLessThanOrEqualTo (2, Math.Abs (g - b), $"grey input {level} came out tinted");
				Assert.IsGreaterThan (previous, r, $"grey input {level} is not brighter than the one before");
				previous = r;
			}
		}

		[TestMethod]
		public void AbstractProfileDescribesItself ()
		{
			var golden = BchswGoldenData.Cases[0];
			using var profile = CreateProfile (golden);

			// The C implementation wrote these as raw char*, which lcms2 rejects, so the
			// description read back empty; the port writes them properly.
			Assert.AreEqual ("f-spot BCHSW abstract profile", profile.GetProfileInfo (InfoType.Description, "en", "US"));
			Assert.AreEqual (ProfileClassSignature.Abstract, profile.DeviceClass);
			Assert.AreEqual (ColorSpaceSignature.LabData, profile.ColorSpace);
			Assert.AreEqual (ColorSpaceSignature.LabData, profile.PCS);
		}

		[TestMethod]
		public void RejectsTheWrongNumberOfCurves ()
		{
			using var line = lcmsNET.ToneCurve.BuildGamma (null, 1.0);

			Assert.ThrowsExactly<ArgumentException> (() => BchswAbstractProfile.Create (9, 1, 0, 0, 0, 0,
				CIExyY.D50, CIExyY.D50, new[] { line, line }));
			Assert.ThrowsExactly<ArgumentOutOfRangeException> (() => BchswAbstractProfile.Create (1, 1, 0, 0, 0, 0,
				CIExyY.D50, CIExyY.D50, null));
		}

		static lcmsNET.Profile CreateProfile (BchswGoldenData.Case golden)
		{
			// Brightness enters as a gamma curve on L, exactly as Profile.CreateAbstract
			// builds it; the CLUT's own brightness term is always zero.
			using var gamma = lcmsNET.ToneCurve.BuildGamma (null, Math.Pow (10, -golden.Bright / 100));
			using var line = lcmsNET.ToneCurve.BuildGamma (null, 1.0);
			return BchswAbstractProfile.Create (golden.GridPoints, golden.Exposure, 0.0, golden.Contrast, golden.Hue,
				golden.Saturation, WhitePoint (golden.SrcX, golden.SrcY), WhitePoint (golden.DestX, golden.DestY),
				new[] { gamma, line, line });
		}

		static BchswAbstractProfile.Adjustments Adjustments (BchswGoldenData.Case golden)
		{
			return new BchswAbstractProfile.Adjustments {
				Exposure = golden.Exposure,
				Brightness = 0.0,
				Contrast = golden.Contrast,
				Hue = golden.Hue,
				Saturation = golden.Saturation,
				SourceWhitePoint = Colorimetric.xyY2XYZ (WhitePoint (golden.SrcX, golden.SrcY)),
				DestinationWhitePoint = Colorimetric.xyY2XYZ (WhitePoint (golden.DestX, golden.DestY)),
			};
		}

		static CIExyY WhitePoint (double x, double y)
		{
			return new CIExyY { x = x, y = y, Y = 1.0 };
		}

		static byte[] FromHex (string hex)
		{
			var bytes = new byte[hex.Length / 2];
			for (int i = 0; i < bytes.Length; i++)
				bytes[i] = Convert.ToByte (hex.Substring (i * 2, 2), 16);
			return bytes;
		}
	}
}
