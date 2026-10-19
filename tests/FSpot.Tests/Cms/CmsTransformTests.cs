// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Cms.Tests
{
	// Display transforms stay at flags 0 (or CopyAlpha). A matrix RGB profile is
	// byte-identical with and without black-point compensation, so the print bit is
	// pinned here and then checked against a real CMYK conversion.
	[TestClass]
	public class CmsTransformTests
	{
		[TestMethod]
		public void For_RequestsBlackPointCompensationOnlyForPrint ()
		{
			uint compensation = (uint)lcmsNET.CmsFlags.BlackPointCompensation;

			Assert.AreEqual (0u, CmsTransforms.For (print: false, copyAlpha: false));
			Assert.AreEqual (CmsTransforms.CopyAlpha, CmsTransforms.For (print: false, copyAlpha: true));
			Assert.AreEqual (compensation, CmsTransforms.For (print: true, copyAlpha: false));
			Assert.AreEqual (CmsTransforms.CopyAlpha | compensation, CmsTransforms.For (print: true, copyAlpha: true));
		}

		[TestMethod]
		public void BlackPointCompensationChangesANeutralPixelInCmyk ()
		{
			const string path = "/System/Library/ColorSync/Profiles/Generic CMYK Profile.icc";
			if (!File.Exists (path))
				Assert.Inconclusive ("Generic CMYK Profile.icc is not on this machine");

			LcmsNative.Require ();
			using var printer = new Profile (path);
			byte[] plain = ToCmyk (printer, 0);
			byte[] compensated = ToCmyk (printer, CmsTransforms.For (print: true, copyAlpha: false));

			Assert.HasCount (4, plain);
			Assert.HasCount (4, compensated);
			Assert.IsFalse (plain.SequenceEqual (compensated),
				"black-point compensation should change a near-black CMYK sample");
		}

		static byte[] ToCmyk (Profile printer, uint flags)
		{
			var input = new byte[] { 1, 1, 1 };
			var output = new byte[4];
			using var srgb = Profile.CreateStandardRgb ();
			using var transform = lcmsNET.Transform.Create (
				new[] { srgb.Native, printer.Native },
				lcmsNET.Cms.TYPE_RGB_8, lcmsNET.Cms.TYPE_CMYK_8,
				lcmsNET.Intent.Perceptual, (lcmsNET.CmsFlags)flags);
			transform.DoTransform (input, output, 1);
			return output;
		}
	}
}
