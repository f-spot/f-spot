// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

using FSpot.Editors;
using FSpot.Imaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using TagLib.Image;

namespace FSpot.Tests.Editors
{
	[TestClass]
	public sealed class PhotoEditorOperationsTests
	{
		[TestMethod]
		public void CanProcess_EveryCatalogEditor ()
		{
			foreach (var editor in PhotoEditorCatalog.All)
				Assert.IsTrue (PhotoEditorOperations.CanProcess (editor), editor.Id);
		}

		[TestMethod]
		public void Flip_MirrorsLeftAndRightPixels ()
		{
			var input = Rgb (2, 1, 255, 0, 0, 0, 0, 255);

			var output = PhotoEditorOperations.Process (PhotoEditorCatalog.Flip, input, null, new PhotoEditorContext ());

			AssertPixel (output, 0, 0, 0, 0, 255);
			AssertPixel (output, 1, 0, 255, 0, 0);
		}

		[TestMethod]
		public void Crop_CopiesTheSelection ()
		{
			var input = Rgb (2, 2,
				10, 10, 10, 20, 20, 20,
				30, 30, 30, 40, 40, 40);

			var output = PhotoEditorOperations.Process (PhotoEditorCatalog.Crop, input, null, new PhotoEditorContext {
				Selection = new PixelRect (1, 1, 1, 1)
			});

			Assert.AreEqual (1, output.Width);
			Assert.AreEqual (1, output.Height);
			AssertPixel (output, 0, 0, 40, 40, 40);
		}

		[TestMethod]
		public void Crop_NegativeOrigin_TrimsInsteadOfShifting ()
		{
			var input = Rgb (4, 1, 9, 0, 0, 8, 0, 0, 7, 0, 0, 6, 0, 0);

			var output = PhotoEditorOperations.Process (PhotoEditorCatalog.Crop, input, null, new PhotoEditorContext {
				Selection = new PixelRect (-1, 0, 2, 1)
			});

			Assert.AreEqual (1, output.Width);
			Assert.AreEqual (1, output.Height);
			AssertPixel (output, 0, 0, 9, 0, 0);
		}

		[TestMethod]
		public void Crop_RightTopPhoto_KeepsTheDisplaySelection ()
		{
			// Load already applied the RightTop tag, so the buffer is upright.
			// Apply must crop the rectangle the user drew, which is what TopLeft means.
			var input = Rgb (4, 2,
				1, 0, 0, 2, 0, 0, 3, 0, 0, 4, 0, 0,
				5, 0, 0, 6, 0, 0, 7, 0, 0, 8, 0, 0);
			var display = new PixelRect (0, 0, 2, 2);

			var output = PhotoEditorOperations.Process (PhotoEditorCatalog.Crop, input, null, new PhotoEditorContext {
				Selection = display,
				Orientation = ImageOrientation.TopLeft,
			});

			Assert.AreEqual (2, output.Width);
			Assert.AreEqual (2, output.Height);
			AssertPixel (output, 0, 0, 1, 0, 0);
			AssertPixel (output, 1, 0, 2, 0, 0);
			AssertPixel (output, 0, 1, 5, 0, 0);
			AssertPixel (output, 1, 1, 6, 0, 0);

			var doubled = PhotoEditorOperations.Process (PhotoEditorCatalog.Crop, input, null, new PhotoEditorContext {
				Selection = display,
				Orientation = ImageOrientation.RightTop,
			});
			Assert.IsFalse (output.Pixels.SequenceEqual (doubled.Pixels));
		}

		[TestMethod]
		public void Blackout_FillsTheSelectionWithBlackAndKeepsTheRest ()
		{
			var input = Rgb (2, 1, 255, 255, 255, 128, 64, 32);

			var output = PhotoEditorOperations.Process (PhotoEditorCatalog.Blackout, input, null, new PhotoEditorContext {
				Selection = new PixelRect (0, 0, 1, 1)
			});

			AssertPixel (output, 0, 0, 0, 0, 0);
			AssertPixel (output, 1, 0, 128, 64, 32);
		}

		[TestMethod]
		public void Pixelate_NearestUpscaleMakesTheBlockUniform ()
		{
			var input = Rgb (2, 2,
				255, 0, 0, 0, 255, 0,
				0, 0, 255, 255, 255, 0);

			var output = PhotoEditorOperations.Process (PhotoEditorCatalog.Pixelate, input, null, new PhotoEditorContext {
				Selection = new PixelRect (0, 0, 2, 2)
			});

			byte[] first = Pixel (output, 0, 0);
			AssertPixel (output, 1, 0, first[0], first[1], first[2]);
			AssertPixel (output, 0, 1, first[0], first[1], first[2]);
			AssertPixel (output, 1, 1, first[0], first[1], first[2]);
		}

		[TestMethod]
		public void RedEye_RemovesARedPixelThatPassesTheGimpThreshold ()
		{
			var input = Rgb (1, 1, 255, 0, 0);

			var output = PhotoEditorOperations.Process (PhotoEditorCatalog.RedEye, input, null, new PhotoEditorContext {
				Selection = new PixelRect (0, 0, 1, 1),
				RedEyeThreshold = -15
			});

			Assert.IsLessThan (255, output.Pixels[0]);
			Assert.AreEqual (0, output.Pixels[1]);
			Assert.AreEqual (0, output.Pixels[2]);
		}

		[TestMethod]
		public void Resize_FitsTheLongEdge ()
		{
			var input = Rgb (4, 2, Enumerable.Repeat ((byte)90, 4 * 2 * 3).ToArray ());

			var output = PhotoEditorOperations.Process (PhotoEditorCatalog.Resize, input, null, new PhotoEditorContext {
				ResizeMaxEdge = 2
			});

			Assert.AreEqual (2, output.Width);
			Assert.AreEqual (1, output.Height);
		}

		[TestMethod]
		public void ConvertToBw_UsesChannelWeightsAndTheToneCurve ()
		{
			var input = Rgb (1, 1, 100, 0, 0);
			var curve = Enumerable.Range (0, 256).Select (i => (byte)Math.Min (255, i * 2)).ToArray ();

			var output = PhotoEditorOperations.Process (PhotoEditorCatalog.BW, input, null, new PhotoEditorContext {
				BwRed = 1,
				BwGreen = 0,
				BwBlue = 0,
				BwCurve = curve
			});

			AssertPixel (output, 0, 0, 200, 200, 200);
		}

		[TestMethod]
		public void Desaturate_CollapsesAPrimaryTowardGray ()
		{
			FSpot.Cms.Tests.LcmsNative.Require ();
			var input = Rgb (1, 1, 255, 0, 0);

			var output = PhotoEditorOperations.Process (PhotoEditorCatalog.Desaturate, input, null, new PhotoEditorContext ());

			int spread = Math.Max (output.Pixels[0], Math.Max (output.Pixels[1], output.Pixels[2]))
				- Math.Min (output.Pixels[0], Math.Min (output.Pixels[1], output.Pixels[2]));
			Assert.IsLessThan (40, spread);
			Assert.AreNotEqual (255, output.Pixels[0]);
		}

		[TestMethod]
		public void Tilt_ZeroDegrees_IsUnchanged ()
		{
			var input = Rgb (2, 1, 9, 8, 7, 6, 5, 4);
			var output = PhotoEditorOperations.Process (PhotoEditorCatalog.Tilt, input, null, new PhotoEditorContext ());
			Assert.AreSequenceEqual (input.Pixels, output.Pixels);
		}

		[TestMethod]
		public void Sharpen_IncreasesContrastAtAPeak ()
		{
			var input = Rgb (3, 1, 80, 80, 80, 160, 160, 160, 80, 80, 80);
			var output = PhotoEditorOperations.Process (PhotoEditorCatalog.Sharpen, input, null, new PhotoEditorContext {
				SharpenRadius = 1,
				SharpenAmount = 1,
				SharpenThreshold = 0
			});
			Assert.IsGreaterThan (160, output.Pixels[3]);
		}

		[TestMethod]
		public void SoftFocus_UniformColor_IsUnchanged ()
		{
			var input = Rgb (2, 2, Enumerable.Repeat ((byte)40, 2 * 2 * 3).ToArray ());
			var output = PhotoEditorOperations.Process (PhotoEditorCatalog.SoftFocus, input, null,
				new PhotoEditorContext { SoftFocusRadius = 0.5 });
			Assert.AreSequenceEqual (input.Pixels, output.Pixels);
		}

		static ImageBuffer Rgb (int width, int height, params byte[] pixels)
			=> new ImageBuffer (width, height, false, width * 3, pixels);

		static byte[] Pixel (ImageBuffer buffer, int x, int y)
		{
			int i = y * buffer.Rowstride + x * (buffer.HasAlpha ? 4 : 3);
			return new[] { buffer.Pixels[i], buffer.Pixels[i + 1], buffer.Pixels[i + 2] };
		}

		static void AssertPixel (ImageBuffer buffer, int x, int y, byte r, byte g, byte b)
		{
			byte[] pixel = Pixel (buffer, x, y);
			Assert.AreEqual (r, pixel[0], $"R at {x},{y}");
			Assert.AreEqual (g, pixel[1], $"G at {x},{y}");
			Assert.AreEqual (b, pixel[2], $"B at {x},{y}");
		}
	}
}
