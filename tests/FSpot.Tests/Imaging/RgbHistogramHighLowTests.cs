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

			Assert.IsTrue (low >= 0 && low <= 255, $"low={low}");
			Assert.IsTrue (high >= 0 && high <= 255, $"high={high}");
		}
	}
}
