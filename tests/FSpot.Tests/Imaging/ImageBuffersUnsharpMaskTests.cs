// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Imaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Imaging.Tests
{
	[TestClass]
	public sealed class ImageBuffersUnsharpMaskTests
	{
		[TestMethod]
		public void UnsharpMask_UniformColor_IsUnchanged ()
		{
			var pixels = new byte[3 * 3 * 3];
			for (int i = 0; i < pixels.Length; i++)
				pixels[i] = 90;
			var input = new ImageBuffer (3, 3, false, 9, pixels);

			var output = ImageBuffers.UnsharpMask (input, radius: 1, amount: 0.5, threshold: 0);

			Assert.AreSequenceEqual (input.Pixels, output.Pixels);
		}

		[TestMethod]
		public void UnsharpMask_IncreasesContrastAtAPeak ()
		{
			var input = new ImageBuffer (3, 1, false, 9, new byte[] {
				80, 80, 80, 160, 160, 160, 80, 80, 80
			});

			var output = ImageBuffers.UnsharpMask (input, radius: 1, amount: 1, threshold: 0);

			Assert.IsGreaterThan (160, output.Pixels[3]);
		}

		[TestMethod]
		public void UnsharpMask_ThresholdSkipsSmallDifferences ()
		{
			var input = new ImageBuffer (3, 1, false, 9, new byte[] {
				80, 80, 80, 160, 160, 160, 80, 80, 80
			});

			var output = ImageBuffers.UnsharpMask (input, radius: 1, amount: 1, threshold: 200);

			Assert.AreEqual (160, output.Pixels[3]);
		}

		[TestMethod]
		public void UnsharpMask_UsesAPeakedBlurSoTheShoulderStaysDarkerThanABox ()
		{
			// GTK's blur weights the center more than its neighbors. A flat box
			// of radius 1 turns 100 into 167 here; the peaked kernel turns it into 150.
			var input = new ImageBuffer (5, 1, false, 15, new byte[] {
				0, 0, 0, 0, 0, 0, 100, 100, 100, 0, 0, 0, 0, 0, 0
			});

			var output = ImageBuffers.UnsharpMask (input, radius: 1, amount: 1, threshold: 0);

			Assert.AreEqual (150, output.Pixels[6]);
			Assert.AreEqual (0, output.Pixels[3]);
		}
	}
}
