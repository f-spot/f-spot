//
// FormatTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Cms.Tests
{
	// FSpot.Cms.Format carries the numeric values of Little-CMS's TYPE_* macros and is
	// passed to lcmsNET as a plain uint, so each value has to agree with lcmsNET's
	// constant of the same name. Pure arithmetic: no native library involved.
	[TestClass]
	public class FormatTests
	{
		public static IEnumerable<(Format Format, uint Expected, int BytesPerPixel)> Formats ()
		{
			yield return (Format.Rgb8, lcmsNET.Cms.TYPE_RGB_8, 3);
			yield return (Format.Rgba8, lcmsNET.Cms.TYPE_RGBA_8, 4);
			yield return (Format.Rgba8Planar, lcmsNET.Cms.TYPE_RGBA_8_PLANAR, 4);
			yield return (Format.Rgb16, lcmsNET.Cms.TYPE_RGB_16, 6);
			yield return (Format.Rgba16, lcmsNET.Cms.TYPE_RGBA_16, 8);
			yield return (Format.Lab16, lcmsNET.Cms.TYPE_Lab_16, 6);
		}

		[TestMethod]
		[DynamicData (nameof (Formats))]
		public void FormatValueMatchesTheLcmsNetConstant (Format format, uint expected, int bytesPerPixel)
		{
			Assert.AreEqual (expected, (uint)format, format.ToString ());
		}

		[TestMethod]
		[DynamicData (nameof (Formats))]
		public void BytesPerPixelDecodesTheFormat (Format format, uint expected, int bytesPerPixel)
		{
			Assert.AreEqual (bytesPerPixel, format.BytesPerPixel (), format.ToString ());
		}

		[TestMethod]
		public void EveryFormatIsPinned ()
		{
			var pinned = new HashSet<Format> ();
			foreach (var (format, _, _) in Formats ())
				pinned.Add (format);

			foreach (Format format in System.Enum.GetValues (typeof (Format)))
				Assert.Contains (format, pinned, $"{format} has no pinned lcmsNET constant");
		}
	}
}
