// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Imaging;
using FSpot.Utils;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using TagLib.Image;

namespace FSpot.Utils.Tests
{
	[TestClass]
	public sealed class ImageOrientationsRectTests
	{
		[TestMethod]
		public void TransformRect_TopLeft_IsUnchanged ()
		{
			var area = ImageOrientations.TransformRect (10, 8, new PixelRect (1, 2, 3, 4), ImageOrientation.TopLeft);
			AssertRect (1, 2, 3, 4, area);
		}

		[TestMethod]
		public void TransformRect_TopRight_MirrorsY ()
		{
			var area = ImageOrientations.TransformRect (10, 8, new PixelRect (1, 1, 3, 2), ImageOrientation.TopRight);
			AssertRect (1, 5, 3, 2, area);
		}

		[TestMethod]
		public void TransformRect_BottomLeft_MirrorsX ()
		{
			var area = ImageOrientations.TransformRect (10, 8, new PixelRect (1, 2, 3, 4), ImageOrientation.BottomLeft);
			AssertRect (6, 2, 3, 4, area);
		}

		[TestMethod]
		public void TransformRect_RightBottom_Transposes ()
		{
			var area = ImageOrientations.TransformRect (10, 8, new PixelRect (1, 2, 3, 4), ImageOrientation.RightBottom);
			AssertRect (2, 1, 4, 3, area);
		}

		[TestMethod]
		public void TransformRect_LeftTop_MirrorsBothAxes ()
		{
			var area = ImageOrientations.TransformRect (10, 8, new PixelRect (1, 2, 3, 4), ImageOrientation.LeftTop);
			AssertRect (2, 6, 4, 3, area);
		}

		// WindowCoordsToImage and the paint blit must pass the pixbuf orientation.
		// ReverseTransformation(LeftTop) is RightBottom, and that transpose does not recover (2, 3).
		[TestMethod]
		public void TransformRect_LeftTop_RecoversTheSourcePointARightBottomTransposeDoesNot ()
		{
			var recovered = ImageOrientations.TransformRect (8, 10, new PixelRect (4, 7, 1, 1), ImageOrientation.LeftTop);
			Assert.AreEqual (2, recovered.X);
			Assert.AreEqual (3, recovered.Y);

			var transposed = ImageOrientations.TransformRect (8, 10, new PixelRect (4, 7, 1, 1), ImageOrientation.RightBottom);
			Assert.AreEqual (7, transposed.X);
			Assert.AreEqual (4, transposed.Y);
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
