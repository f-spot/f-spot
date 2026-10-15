// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Imaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Imaging.Tests
{
	[TestClass]
	public sealed class ImageBuffersSoftFocusTests
	{
		[TestMethod]
		public void ScaleAreaAverage_BlendsEachSourcePair ()
		{
			var pixels = new byte[4 * 3];
			pixels[3] = pixels[4] = pixels[5] = 100;
			pixels[9] = pixels[10] = pixels[11] = 100;
			var input = new ImageBuffer (4, 1, false, 12, pixels);

			var output = ImageBuffers.ScaleAreaAverage (input, 2, 1);

			Assert.AreEqual (50, output.Pixels[0]);
			Assert.AreEqual (50, output.Pixels[3]);
		}

		[TestMethod]
		public void SoftFocus_UniformColor_StaysUniform ()
		{
			var input = new ImageBuffer (4, 4, false, 12, Repeat (80, 4 * 4 * 3));

			var output = ImageBuffers.SoftFocus (input, 0.5);

			for (int i = 0; i < output.Pixels.Length; i++)
				Assert.AreEqual (80, output.Pixels[i]);
		}

		[TestMethod]
		public void SoftFocus_LargerRadiusKeepsMoreOfTheOriginalNearTheCenter ()
		{
			// GTK Amount=3 is a small blur; a 64x64 split makes the mask
			// radius change visible below the center on the black half.
			var pixels = new byte[64 * 64 * 3];
			for (int y = 0; y < 64; y++) {
				for (int x = 0; x < 32; x++) {
					int i = y * 192 + x * 3;
					pixels[i] = pixels[i + 1] = pixels[i + 2] = 255;
				}
			}

			var input = new ImageBuffer (64, 64, false, 192, pixels);
			var tight = ImageBuffers.SoftFocus (input, 0);
			var wide = ImageBuffers.SoftFocus (input, 1);

			int at = 48 * 192 + 32 * 3;
			Assert.IsLessThan (tight.Pixels[at], wide.Pixels[at]);
		}

		[TestMethod]
		public void SoftFocus_CenterPixelStaysSharpWhenTheMaskIsCenteredOnIt ()
		{
			const int size = 256;
			var pixels = new byte[size * size * 3];
			int center = 128 * size * 3 + 128 * 3;
			pixels[center] = pixels[center + 1] = pixels[center + 2] = 200;
			var input = new ImageBuffer (size, size, false, size * 3, pixels);

			var output = ImageBuffers.SoftFocus (input, 0);

			Assert.AreEqual (200, output.Pixels[center]);
			Assert.AreEqual (200, output.Pixels[center + 1]);
			Assert.AreEqual (200, output.Pixels[center + 2]);
		}

		static byte[] Repeat (byte value, int count)
		{
			var data = new byte[count];
			for (int i = 0; i < count; i++)
				data[i] = value;
			return data;
		}
	}
}
