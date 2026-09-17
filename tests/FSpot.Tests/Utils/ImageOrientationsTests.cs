// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

using TagLib.Image;

namespace FSpot.Utils.Tests
{
	[TestClass]
	public sealed class ImageOrientationsTests
	{
		[TestMethod]
		[DataRow (ImageOrientation.None, ImageOrientation.LeftBottom)]
		[DataRow (ImageOrientation.TopLeft, ImageOrientation.LeftBottom)]
		[DataRow (ImageOrientation.TopRight, ImageOrientation.LeftTop)]
		[DataRow (ImageOrientation.BottomRight, ImageOrientation.RightTop)]
		[DataRow (ImageOrientation.BottomLeft, ImageOrientation.RightBottom)]
		[DataRow (ImageOrientation.LeftTop, ImageOrientation.BottomLeft)]
		[DataRow (ImageOrientation.RightTop, ImageOrientation.TopLeft)]
		[DataRow (ImageOrientation.RightBottom, ImageOrientation.TopRight)]
		[DataRow (ImageOrientation.LeftBottom, ImageOrientation.BottomRight)]
		public void Rotate270_FollowsTheExifQuarterTurnTable (ImageOrientation start, ImageOrientation expected)
		{
			Assert.AreEqual (expected, ImageOrientations.Rotate270 (start));
		}

		[TestMethod]
		[DataRow (ImageOrientation.None, ImageOrientation.RightTop)]
		[DataRow (ImageOrientation.TopLeft, ImageOrientation.RightTop)]
		[DataRow (ImageOrientation.TopRight, ImageOrientation.RightBottom)]
		[DataRow (ImageOrientation.BottomRight, ImageOrientation.LeftBottom)]
		[DataRow (ImageOrientation.BottomLeft, ImageOrientation.LeftTop)]
		[DataRow (ImageOrientation.LeftTop, ImageOrientation.TopRight)]
		[DataRow (ImageOrientation.RightTop, ImageOrientation.BottomRight)]
		[DataRow (ImageOrientation.RightBottom, ImageOrientation.BottomLeft)]
		[DataRow (ImageOrientation.LeftBottom, ImageOrientation.TopLeft)]
		public void Rotate90_FollowsTheExifQuarterTurnTable (ImageOrientation start, ImageOrientation expected)
		{
			Assert.AreEqual (expected, ImageOrientations.Rotate90 (start));
		}

		[TestMethod]
		public void FourRotate270s_ReturnToTheNormalizedStart ()
		{
			Assert.AreEqual (ImageOrientation.TopLeft,
				ImageOrientations.Rotate270 (ImageOrientations.Rotate270 (
					ImageOrientations.Rotate270 (ImageOrientations.Rotate270 (ImageOrientation.None)))));
			Assert.AreEqual (ImageOrientation.TopLeft,
				ImageOrientations.Rotate270 (ImageOrientations.Rotate270 (
					ImageOrientations.Rotate270 (ImageOrientations.Rotate270 (ImageOrientation.TopLeft)))));
		}
	}
}
