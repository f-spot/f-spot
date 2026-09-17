// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Imaging.Tests
{
	[TestClass]
	public class RgbHistogramTests
	{
		[TestMethod]
		public void CountBgra_Empty_IsAllZeros ()
		{
			var counts = RgbHistogram.CountBgra (Array.Empty<byte> ());

			Assert.AreEqual (256, counts.GetLength (0));
			Assert.AreEqual (3, counts.GetLength (1));
			Assert.AreEqual (0, counts[0, RgbHistogram.Red]);
			Assert.AreEqual (0, counts[255, RgbHistogram.Blue]);
		}

		[TestMethod]
		public void CountBgra_IgnoresATrailingPartialPixel ()
		{
			var counts = RgbHistogram.CountBgra (new byte[] { 10, 20, 30 });

			Assert.AreEqual (0, counts[30, RgbHistogram.Red]);
		}

		[TestMethod]
		public void CountBgra_ReadsBgraChannels ()
		{
			// B, G, R, A
			var bgra = new byte[] {
				1, 2, 3, 255,
				1, 2, 3, 128
			};

			var counts = RgbHistogram.CountBgra (bgra);

			Assert.AreEqual (2, counts[3, RgbHistogram.Red]);
			Assert.AreEqual (2, counts[2, RgbHistogram.Green]);
			Assert.AreEqual (2, counts[1, RgbHistogram.Blue]);
		}

		[TestMethod]
		public void RenderBgra_NullCounts_Throws ()
		{
			Assert.ThrowsExactly<ArgumentNullException> (() => RgbHistogram.RenderBgra (null, 8));
		}

		[TestMethod]
		[DataRow (0)]
		[DataRow (-1)]
		public void RenderBgra_NonPositiveHeight_Throws (int height)
		{
			Assert.ThrowsExactly<ArgumentOutOfRangeException> (
				() => RgbHistogram.RenderBgra (new int[256, 3], height));
		}

		[TestMethod]
		public void RenderBgra_FillsFromTheBottomAndOverlapsChannels ()
		{
			var counts = new int[256, 3];
			counts[0, RgbHistogram.Red] = 2;
			counts[0, RgbHistogram.Green] = 1;
			counts[0, RgbHistogram.Blue] = 4;

			byte[] pixels = RgbHistogram.RenderBgra (counts, 4);

			Assert.HasCount (256 * 4 * 4, pixels);
			AssertBottomPixel (pixels, 4, 0, 3, blue: 255, green: 0, red: 0);
			AssertBottomPixel (pixels, 4, 0, 1, blue: 255, green: 0, red: 255);
			AssertBottomPixel (pixels, 4, 0, 0, blue: 255, green: 255, red: 255);
			AssertBottomPixel (pixels, 4, 1, 0, blue: 0, green: 0, red: 0);
		}

		static void AssertBottomPixel (byte[] pixels, int height, int x, int fromBottom,
			byte blue, byte green, byte red)
		{
			int y = height - 1 - fromBottom;
			int i = (y * 256 + x) * 4;
			Assert.AreEqual (blue, pixels[i]);
			Assert.AreEqual (green, pixels[i + 1]);
			Assert.AreEqual (red, pixels[i + 2]);
			Assert.AreEqual (255, pixels[i + 3]);
		}
	}
}
