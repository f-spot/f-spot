// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Imaging;
using FSpot.Utils;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Utils.Tests
{
	[TestClass]
	public sealed class UniformImageLayoutTests
	{
		[TestMethod]
		public void MapRect_ZeroImageSize_IsEmpty ()
		{
			var area = UniformImageLayout.MapRect (200, 100, 0, 0, 10, 10, 40, 40);
			Assert.IsTrue (area.IsEmpty);
		}

		[TestMethod]
		public void MapRect_WhenTheImageFillsTheView_IsIdentity ()
		{
			var area = UniformImageLayout.MapRect (100, 50, 100, 50, 10, 5, 40, 25);
			AssertRect (10, 5, 30, 20, area);
		}

		[TestMethod]
		public void MapRect_StripsLetterboxAndScales ()
		{
			// 200x100 view, 100x100 image → scale 1, origin x=50
			var area = UniformImageLayout.MapRect (200, 100, 100, 100, 50, 0, 150, 100);
			AssertRect (0, 0, 100, 100, area);
		}

		[TestMethod]
		public void MapRect_ClampsPointsOutsideTheImage ()
		{
			var area = UniformImageLayout.MapRect (200, 100, 100, 100, 0, 0, 200, 100);
			AssertRect (0, 0, 100, 100, area);
		}

		[TestMethod]
		public void MapPoint_MapsViewCoordinatesOntoTheImage ()
		{
			var point = UniformImageLayout.MapPoint (200, 100, 100, 50, 100, 50);
			Assert.AreEqual (50, point.X);
			Assert.AreEqual (25, point.Y);
		}

		[TestMethod]
		public void MapRect_NormalizesInvertedDrag ()
		{
			var area = UniformImageLayout.MapRect (100, 100, 100, 100, 80, 70, 20, 10);
			AssertRect (20, 10, 60, 60, area);
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
