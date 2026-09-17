// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Numerics;

using FSpot.Cms;
using FSpot.ColorAdjustment;
using FSpot.Imaging;
using FSpot.Utils;

namespace FSpot.Editors
{
	public static class PhotoEditorOperations
	{
		public static bool CanProcess (PhotoEditorDescriptor editor)
			=> editor != null && editor.HasCoreOperation;

		public static ImageBuffer Process (PhotoEditorDescriptor editor, ImageBuffer input, Profile inputProfile, PhotoEditorContext context)
		{
			if (editor == null)
				throw new ArgumentNullException (nameof (editor));
			if (input == null)
				throw new ArgumentNullException (nameof (input));
			context = context ?? new PhotoEditorContext ();

			if (editor.Id == PhotoEditorCatalog.Flip.Id)
				return ImageBuffers.FlipHorizontal (input);
			if (editor.Id == PhotoEditorCatalog.Crop.Id)
				return Crop (input, context);
			if (editor.Id == PhotoEditorCatalog.Blackout.Id)
				return Blackout (input, context);
			if (editor.Id == PhotoEditorCatalog.Pixelate.Id)
				return Pixelate (input, context);
			if (editor.Id == PhotoEditorCatalog.RedEye.Id)
				return RedEye (input, context);
			if (editor.Id == PhotoEditorCatalog.Resize.Id)
				return Resize (input, context);
			if (editor.Id == PhotoEditorCatalog.BW.Id)
				return ConvertToBw (input, context);
			if (editor.Id == PhotoEditorCatalog.Desaturate.Id)
				return new Desaturate (input, inputProfile).Adjust ();
			if (editor.Id == PhotoEditorCatalog.Sepia.Id)
				return new SepiaTone (input, inputProfile).Adjust ();
			if (editor.Id == PhotoEditorCatalog.AutoStretch.Id)
				return new AutoStretch (input, inputProfile).Adjust ();
			if (editor.Id == PhotoEditorCatalog.Color.Id)
				return AdjustColors (input, inputProfile, context);
			if (editor.Id == PhotoEditorCatalog.Tilt.Id)
				return ImageBuffers.TiltFill (input, context.TiltDegrees);
			if (editor.Id == PhotoEditorCatalog.SoftFocus.Id)
				return ImageBuffers.SoftFocus (input, context.SoftFocusRadius);
			if (editor.Id == PhotoEditorCatalog.Sharpen.Id)
				return ImageBuffers.UnsharpMask (input, context.SharpenRadius, context.SharpenAmount, context.SharpenThreshold);

			throw new NotSupportedException (editor.Id);
		}

		static ImageBuffer Crop (ImageBuffer input, PhotoEditorContext context)
		{
			var selection = ImageBuffers.Clamp (input, OrientedSelection (input, context));
			if (selection.IsEmpty)
				throw new ArgumentException ("Crop requires a selection");
			return ImageBuffers.Crop (input, selection);
		}

		static ImageBuffer Blackout (ImageBuffer input, PhotoEditorContext context)
		{
			var output = ImageBuffers.Clone (input);
			ImageBuffers.Fill (output, OrientedSelection (input, context), 0, 0, 0);
			return output;
		}

		static ImageBuffer Pixelate (ImageBuffer input, PhotoEditorContext context)
		{
			var selection = ImageBuffers.Clamp (input, OrientedSelection (input, context));
			if (selection.IsEmpty)
				return ImageBuffers.Clone (input);

			var cropped = ImageBuffers.Crop (input, selection);
			int downWidth = Math.Max (1, selection.Width / 75);
			int downHeight = Math.Max (1, selection.Height / 75);
			var down = ImageBuffers.ScaleNearest (cropped, downWidth, downHeight);
			var up = ImageBuffers.ScaleNearest (down, selection.Width, selection.Height);
			var output = ImageBuffers.Clone (input);
			ImageBuffers.Blit (output, selection, up);
			return output;
		}

		static ImageBuffer RedEye (ImageBuffer input, PhotoEditorContext context)
		{
			var selection = OrientedSelection (input, context);
			selection = ImageBuffers.Clamp (input, selection);
			var output = ImageBuffers.Clone (input);
			if (selection.IsEmpty)
				return output;

			int channels = ImageBuffers.Channels (output);
			int threshold = context.RedEyeThreshold;
			const double RedFactor = 0.5133333;
			const double GreenFactor = 1;
			const double BlueFactor = 0.1933333;
			for (int y = 0; y < selection.Height; y++) {
				int row = (selection.Y + y) * output.Rowstride + selection.X * channels;
				for (int x = 0; x < selection.Width; x++) {
					int i = row + x * channels;
					int adjustedRed = (int)(output.Pixels[i] * RedFactor);
					int adjustedGreen = (int)(output.Pixels[i + 1] * GreenFactor);
					int adjustedBlue = (int)(output.Pixels[i + 2] * BlueFactor);
					if (adjustedRed >= adjustedGreen - threshold && adjustedRed >= adjustedBlue - threshold)
						output.Pixels[i] = (byte)((adjustedGreen + adjustedBlue) / (2.0 * RedFactor));
				}
			}

			return output;
		}

		static ImageBuffer Resize (ImageBuffer input, PhotoEditorContext context)
		{
			int max = Math.Max (input.Width, input.Height);
			if (context.ResizeMaxEdge <= 0 || context.ResizeMaxEdge >= max)
				return ImageBuffers.Clone (input);

			double ratio = context.ResizeMaxEdge / (double)max;
			int width = Math.Max (1, (int)(input.Width * ratio));
			int height = Math.Max (1, (int)(input.Height * ratio));
			if (ImagingServices.Backend != null)
				return ImagingServices.Required.Scale (input, width, height);
			return ImageBuffers.ScaleNearest (input, width, height);
		}

		static ImageBuffer ConvertToBw (ImageBuffer input, PhotoEditorContext context)
		{
			var output = ImageBuffers.Clone (input);
			var multiply = new Vector4 (context.BwRed, context.BwGreen, context.BwBlue, 0);
			float sum = multiply.X + multiply.Y + multiply.Z;
			if (sum <= 0)
				multiply = new Vector4 (80, 10, 10, 0);
			sum = multiply.X + multiply.Y + multiply.Z;
			multiply /= new Vector4 (sum);
			byte[] curve = context.BwCurve ?? IdentityCurve ();
			int channels = ImageBuffers.Channels (input);
			for (int y = 0; y < input.Height; y++) {
				int row = y * input.Rowstride;
				for (int x = 0; x < input.Width; x++) {
					int i = row + x * channels;
					float val = input.Pixels[i] * multiply.X
						+ input.Pixels[i + 1] * multiply.Y
						+ input.Pixels[i + 2] * multiply.Z;
					byte tone = curve[unchecked((byte)val)];
					output.Pixels[i] = tone;
					output.Pixels[i + 1] = tone;
					output.Pixels[i + 2] = tone;
				}
			}

			return output;
		}

		static ImageBuffer AdjustColors (ImageBuffer input, Profile inputProfile, PhotoEditorContext context)
		{
			var srcWp = ColorCIExyY.WhitePointFromTemperature (5000).ToXYZ ();
			var destWp = ColorCIExyY.WhitePointFromTemperature ((int)context.ColorTemperature).ToXYZ ();
			var destLab = destWp.ToLab (srcWp);
			destLab.a += context.ColorTint;
			destWp = destLab.ToXYZ (srcWp);
			return new FullColorAdjustment (input, inputProfile,
				context.ColorExposure, context.ColorBrightness, context.ColorContrast,
				context.ColorHue, context.ColorSaturation, srcWp, destWp).Adjust ();
		}

		static PixelRect OrientedSelection (ImageBuffer input, PhotoEditorContext context)
		{
			int displayWidth = (int)context.Orientation <= 4 ? input.Width : input.Height;
			int displayHeight = (int)context.Orientation <= 4 ? input.Height : input.Width;
			return ImageOrientations.TransformRect (displayWidth, displayHeight, context.Selection, context.Orientation);
		}

		static byte[] IdentityCurve ()
		{
			var curve = new byte[256];
			for (int i = 0; i < curve.Length; i++)
				curve[i] = (byte)i;
			return curve;
		}
	}
}
