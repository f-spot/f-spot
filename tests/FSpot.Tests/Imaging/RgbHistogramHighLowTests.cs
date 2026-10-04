// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Imaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Imaging.Tests
{
	[TestClass]
	public sealed class RgbHistogramHighLowTests
	{
		[TestMethod]
		public void CountRgb_ReadsPackedRgb ()
		{
			var buffer = new ImageBuffer (2, 1, false, 6, new byte[] { 1, 2, 3, 1, 2, 3 });

			var counts = RgbHistogram.CountRgb (buffer);

			Assert.AreEqual (2, counts[1, RgbHistogram.Red]);
			Assert.AreEqual (2, counts[2, RgbHistogram.Green]);
			Assert.AreEqual (2, counts[3, RgbHistogram.Blue]);
		}

		[TestMethod]
		public void GetHighLow_FindsThePercentileCutsOnASpike ()
		{
			var values = new int[256, 3];
			values[10, RgbHistogram.Red] = 1;
			values[200, RgbHistogram.Red] = 1;

			RgbHistogram.GetHighLow (values, RgbHistogram.Red, out int high, out int low);

			Assert.AreEqual (10, low);
			Assert.AreEqual (200, high);
		}

		[TestMethod]
		public void GetHighLow_FlatHistogramClipsEachEndIndependently ()
		{
			var values = new int[256, 3];
			for (int i = 0; i < 256; i++)
				values[i, RgbHistogram.Red] = 100;

			RgbHistogram.GetHighLow (values, RgbHistogram.Red, out int high, out int low);

			Assert.AreEqual (2, low);
			Assert.AreEqual (253, high);
		}

		[TestMethod]
		public void GetHighLow_EmptyGapDoesNotPullTheCutIntoTheBody ()
		{
			var values = new int[256, 3];
			values[0, RgbHistogram.Red] = 60;
			values[128, RgbHistogram.Red] = 9900;
			values[255, RgbHistogram.Red] = 40;

			RgbHistogram.GetHighLow (values, RgbHistogram.Red, out int high, out int low);

			Assert.AreEqual (1, low);
			Assert.AreEqual (254, high);
		}
	}
}
