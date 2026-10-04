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
			byte[] wide = RgbHistogram.RenderBgra (counts, 512, 8);
			Assert.HasCount (512 * 8 * 4, wide);

			Assert.HasCount (256 * 4 * 4, pixels);
			AssertBottomPixel (pixels, 256, 4, 0, 3, blue: 255, green: 0, red: 0);
			AssertBottomPixel (pixels, 256, 4, 0, 1, blue: 255, green: 0, red: 255);
			AssertBottomPixel (pixels, 256, 4, 0, 0, blue: 255, green: 255, red: 255);
			AssertBottomPixel (pixels, 256, 4, 1, 0, blue: 0, green: 0, red: 0);
		}

		[TestMethod]
		public void CountRgb_CountsEveryRowOncePastPadding ()
		{
			var pixels = new byte[] {
				9, 0, 0, 0xEE,
				4, 0, 0, 0xEE,
			};
			var buffer = new ImageBuffer (1, 2, false, 4, pixels);

			var counts = RgbHistogram.CountRgb (buffer);

			Assert.AreEqual (1, counts[9, RgbHistogram.Red]);
			Assert.AreEqual (1, counts[4, RgbHistogram.Red]);
		}

		[TestMethod]
		public void RenderBgra_NarrowChartKeepsEndpointAndOddCodes ()
		{
			var counts = new int[256, 3];
			counts[1, RgbHistogram.Red] = 50;
			counts[255, RgbHistogram.Red] = 50;

			byte[] pixels = RgbHistogram.RenderBgra (counts, 128, 8);

			AssertBottomPixel (pixels, 128, 8, 0, 0, blue: 0, green: 0, red: 255);
			AssertBottomPixel (pixels, 128, 8, 127, 0, blue: 0, green: 0, red: 255);
		}

		[TestMethod]
		public void RenderBgra_Width192KeepsBinsTheSampleWouldSkip ()
		{
			var counts = new int[256, 3];
			counts[3, RgbHistogram.Red] = 100;

			byte[] pixels = RgbHistogram.RenderBgra (counts, 192, 4);

			AssertBottomPixel (pixels, 192, 4, 2, 0, blue: 0, green: 0, red: 255);
		}

		[TestMethod]
		public void RenderBgra_ScalesToTheSummedColumnNotTheTallestBin ()
		{
			var counts = new int[256, 3];
			counts[0, RgbHistogram.Red] = 100;
			counts[1, RgbHistogram.Red] = 50;
			counts[254, RgbHistogram.Red] = 100;

			byte[] pixels = RgbHistogram.RenderBgra (counts, 128, 15);

			AssertBottomPixel (pixels, 128, 15, 0, 14, blue: 0, green: 0, red: 255);
			AssertBottomPixel (pixels, 128, 15, 127, 14, blue: 0, green: 0, red: 0);
			AssertBottomPixel (pixels, 128, 15, 127, 9, blue: 0, green: 0, red: 255);
		}

		[TestMethod]
		public void RenderBgra_PaintsTheBackgroundOnlyAboveTheTallestChannel ()
		{
			var counts = new int[256, 3];
			counts[0, RgbHistogram.Red] = 1;
			counts[1, RgbHistogram.Red] = 3;

			byte[] pixels = RgbHistogram.RenderBgra (counts, 256, 4, backgroundRed: 10, backgroundGreen: 20, backgroundBlue: 30);

			AssertBottomPixel (pixels, 256, 4, 0, 3, blue: 30, green: 20, red: 10);
			AssertBottomPixel (pixels, 256, 4, 0, 0, blue: 0, green: 0, red: 255);
		}

		static void AssertBottomPixel (byte[] pixels, int width, int height, int x, int fromBottom,
			byte blue, byte green, byte red)
		{
			int y = height - 1 - fromBottom;
			int i = (y * width + x) * 4;
			Assert.AreEqual (blue, pixels[i], $"blue x={x} fromBottom={fromBottom}");
			Assert.AreEqual (green, pixels[i + 1], $"green x={x} fromBottom={fromBottom}");
			Assert.AreEqual (red, pixels[i + 2], $"red x={x} fromBottom={fromBottom}");
			Assert.AreEqual (255, pixels[i + 3], $"alpha x={x} fromBottom={fromBottom}");
		}
	}
}
