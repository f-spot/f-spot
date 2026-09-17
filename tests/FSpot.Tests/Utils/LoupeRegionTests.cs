// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Imaging;
using FSpot.Utils;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Utils.Tests
{
	[TestClass]
	public sealed class LoupeRegionTests
	{
		[TestMethod]
		public void Around_CentersATwoRadiusSquareOnInteriorPoints ()
		{
			var area = LoupeRegion.Around (1000, 1000, 500, 500, 128);
			AssertRect (372, 372, 256, 256, area);
		}

		[TestMethod]
		public void Around_ClampsToTheTopLeft ()
		{
			var area = LoupeRegion.Around (1000, 1000, 10, 10, 128);
			AssertRect (0, 0, 256, 256, area);
		}

		[TestMethod]
		public void Around_ClampsToTheBottomRight ()
		{
			var area = LoupeRegion.Around (1000, 1000, 990, 990, 128);
			AssertRect (744, 744, 256, 256, area);
		}

		[TestMethod]
		public void Around_WhenTheImageIsSmallerThanTheLens_IsTheWholeImage ()
		{
			var area = LoupeRegion.Around (100, 80, 50, 40, 128);
			AssertRect (0, 0, 100, 80, area);
		}

		static void AssertRect (int x, int y, int width, int height, PixelRect area)
		{
			Assert.AreEqual (x, area.X);
			Assert.AreEqual (y, area.Y);
			Assert.AreEqual (width, area.Width);
			Assert.AreEqual (height, area.Height);
		}
	}
}
