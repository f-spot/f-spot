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
		public void TransformRect_TopRight_MirrorsX ()
		{
			var area = ImageOrientations.TransformRect (10, 8, new PixelRect (1, 2, 3, 4), ImageOrientation.TopRight);
			AssertRect (6, 2, 3, 4, area);
		}

		[TestMethod]
		public void TransformRect_LeftTop_SwapsAxes ()
		{
			var area = ImageOrientations.TransformRect (10, 8, new PixelRect (1, 2, 3, 4), ImageOrientation.LeftTop);
			AssertRect (2, 1, 4, 3, area);
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
