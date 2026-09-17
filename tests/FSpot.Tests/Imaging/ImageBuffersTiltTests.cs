// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Imaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Imaging.Tests
{
	[TestClass]
	public sealed class ImageBuffersTiltTests
	{
		[TestMethod]
		public void TiltCoverScale_ZeroTilt_IsOne ()
		{
			Assert.AreEqual (1.0, ImageBuffers.TiltCoverScale (100, 80, 0), 1e-9);
		}

		[TestMethod]
		public void TiltCoverScale_FortyFiveOnASquare_IsSqrtTwo ()
		{
			double scale = ImageBuffers.TiltCoverScale (100, 100, System.Math.PI / 4);
			Assert.AreEqual (System.Math.Sqrt (2), scale, 1e-6);
		}

		[TestMethod]
		public void TiltFill_ZeroDegrees_IsUnchanged ()
		{
			var input = new ImageBuffer (2, 2, false, 6, new byte[] {
				1, 2, 3, 4, 5, 6,
				7, 8, 9, 10, 11, 12
			});

			var output = ImageBuffers.TiltFill (input, 0);

			Assert.AreEqual (input.Width, output.Width);
			Assert.AreEqual (input.Height, output.Height);
			Assert.AreSequenceEqual (input.Pixels, output.Pixels);
		}

		[TestMethod]
		public void TiltFill_KeepsTheCenterPixelOfAnOddSquare ()
		{
			// 3x3 unique reds; center is 5
			var input = new ImageBuffer (3, 3, false, 9, new byte[] {
				1, 0, 0, 2, 0, 0, 3, 0, 0,
				4, 0, 0, 5, 0, 0, 6, 0, 0,
				7, 0, 0, 8, 0, 0, 9, 0, 0
			});

			var output = ImageBuffers.TiltFill (input, 20);

			Assert.AreEqual (3, output.Width);
			Assert.AreEqual (3, output.Height);
			Assert.AreEqual (5, output.Pixels[output.Rowstride + 3]);
		}
	}
}
