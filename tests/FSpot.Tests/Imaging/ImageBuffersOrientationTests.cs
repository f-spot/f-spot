// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Imaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using TagLib.Image;

namespace FSpot.Imaging.Tests
{
	[TestClass]
	public sealed class ImageBuffersOrientationTests
	{
		// 2x2 unique reds: A B / C D
		static ImageBuffer Sample ()
			=> new ImageBuffer (2, 2, false, 6, new byte[] {
				1, 0, 0, 2, 0, 0,
				3, 0, 0, 4, 0, 0
			});

		[TestMethod]
		public void TransformOrientation_TopLeft_IsUnchanged ()
		{
			var output = ImageBuffers.TransformOrientation (Sample (), ImageOrientation.TopLeft);
			AssertLayout (output, 1, 2, 3, 4);
		}

		[TestMethod]
		public void TransformOrientation_TopRight_FlipsVertically ()
		{
			// Matches GTK Pixbuf.Flip (false)
			var output = ImageBuffers.TransformOrientation (Sample (), ImageOrientation.TopRight);
			AssertLayout (output, 3, 4, 1, 2);
		}

		[TestMethod]
		public void TransformOrientation_BottomRight_Rotates180 ()
		{
			var output = ImageBuffers.TransformOrientation (Sample (), ImageOrientation.BottomRight);
			AssertLayout (output, 4, 3, 2, 1);
		}

		[TestMethod]
		public void TransformOrientation_BottomLeft_FlipsHorizontally ()
		{
			var output = ImageBuffers.TransformOrientation (Sample (), ImageOrientation.BottomLeft);
			AssertLayout (output, 2, 1, 4, 3);
		}

		[TestMethod]
		public void TransformOrientation_RightTop_Rotates90Clockwise ()
		{
			var output = ImageBuffers.TransformOrientation (Sample (), ImageOrientation.RightTop);
			Assert.AreEqual (2, output.Width);
			Assert.AreEqual (2, output.Height);
			AssertLayout (output, 3, 1, 4, 2);
		}

		[TestMethod]
		public void TransformOrientation_LeftBottom_Rotates90Counterclockwise ()
		{
			var output = ImageBuffers.TransformOrientation (Sample (), ImageOrientation.LeftBottom);
			AssertLayout (output, 2, 4, 1, 3);
		}

		[TestMethod]
		public void TransformOrientation_LeftTop_Rotates90ClockwiseThenFlipsVertically ()
		{
			var output = ImageBuffers.TransformOrientation (Sample (), ImageOrientation.LeftTop);
			AssertLayout (output, 4, 2, 3, 1);
		}

		[TestMethod]
		public void TransformOrientation_RightBottom_Rotates90CounterclockwiseThenFlipsVertically ()
		{
			var output = ImageBuffers.TransformOrientation (Sample (), ImageOrientation.RightBottom);
			AssertLayout (output, 1, 3, 2, 4);
		}

		static void AssertLayout (ImageBuffer buffer, byte a, byte b, byte c, byte d)
		{
			Assert.AreEqual (a, buffer.Pixels[0]);
			Assert.AreEqual (b, buffer.Pixels[3]);
			Assert.AreEqual (c, buffer.Pixels[buffer.Rowstride]);
			Assert.AreEqual (d, buffer.Pixels[buffer.Rowstride + 3]);
		}
	}
}
