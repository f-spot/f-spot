// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.Imaging;
using FSpot.Cms;

using SkiaSharp;

namespace FSpot.Avalonia.Printing;

// Decodes a photo for print or mail with Skia: upright (EXIF orientation applied),
// no larger than asked for, as opaque RGBA the colour transform can work on.
static class PrintImageLoader
{
	// Decodes path so its longer edge is at most maxEdge (never upscaled); null when
	// Skia cannot read the file.
	public static SKBitmap? Load (string path, int maxEdge)
		=> DecodeGate.Run (() => LoadNow (path, maxEdge));

	static SKBitmap? LoadNow (string path, int maxEdge)
	{
		using var codec = SKCodec.Create (path);
		if (codec is null)
			return null;

		// The long edge is the box. A square box is min(edge/w, edge/h, 1), which
		// never upscales and is what thumbnails ask for; print and mail pass the long edge.
		int box = maxEdge;
		var bitmap = SkiaScaledDecoder.TryDecode (codec, box);
		if (bitmap is null)
			return null;
		return AsStraightAlpha (Orient (bitmap, codec.EncodedOrigin));
	}

	// Scales bitmap down (disposing it) when its longer edge exceeds maxEdge.
	public static SKBitmap Shrink (SKBitmap bitmap, int maxEdge)
	{
		int longest = Math.Max (bitmap.Width, bitmap.Height);
		if (maxEdge <= 0 || longest <= maxEdge)
			return bitmap;
		double scale = (double)maxEdge / longest;
		var size = new SKImageInfo (Math.Max (1, (int)Math.Round (bitmap.Width * scale)),
			Math.Max (1, (int)Math.Round (bitmap.Height * scale)), bitmap.ColorType, bitmap.AlphaType);
		var resized = bitmap.Resize (size, new SKSamplingOptions (SKCubicResampler.Mitchell));
		if (resized is null)
			return bitmap;
		bitmap.Dispose ();
		return resized;
	}

	public static bool SwapsAxes (SKEncodedOrigin origin)
		=> origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
			or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

	// The matrix that maps the stored pixels onto the upright image of width x height (as stored).
	public static SKMatrix OrientationMatrix (SKEncodedOrigin origin, int width, int height)
		=> origin switch {
			SKEncodedOrigin.TopRight => new SKMatrix (-1, 0, width, 0, 1, 0, 0, 0, 1),
			SKEncodedOrigin.BottomRight => new SKMatrix (-1, 0, width, 0, -1, height, 0, 0, 1),
			SKEncodedOrigin.BottomLeft => new SKMatrix (1, 0, 0, 0, -1, height, 0, 0, 1),
			SKEncodedOrigin.LeftTop => new SKMatrix (0, 1, 0, 1, 0, 0, 0, 0, 1),
			SKEncodedOrigin.RightTop => new SKMatrix (0, -1, height, 1, 0, 0, 0, 0, 1),
			SKEncodedOrigin.RightBottom => new SKMatrix (0, -1, height, -1, 0, width, 0, 0, 1),
			SKEncodedOrigin.LeftBottom => new SKMatrix (0, 1, 0, -1, 0, width, 0, 0, 1),
			_ => SKMatrix.Identity,
		};

	// Returns bitmap turned upright, disposing the original when a copy was needed.
	public static SKBitmap Orient (SKBitmap bitmap, SKEncodedOrigin origin)
	{
		if (origin is SKEncodedOrigin.TopLeft)
			return bitmap;
		bool swap = SwapsAxes (origin);
		var info = new SKImageInfo (swap ? bitmap.Height : bitmap.Width, swap ? bitmap.Width : bitmap.Height,
			bitmap.ColorType, bitmap.AlphaType);
		var upright = new SKBitmap (info);
		using (var canvas = new SKCanvas (upright)) {
			canvas.SetMatrix (OrientationMatrix (origin, bitmap.Width, bitmap.Height));
			DrawUnscaled (canvas, bitmap);
		}
		bitmap.Dispose ();
		return upright;
	}

	// Gdk.Pixbuf.RotateSimple (Counterclockwise), as PrintOperation turns a photo to fit its cell.
	public static SKBitmap RotateCounterclockwise (SKBitmap bitmap)
	{
		var info = new SKImageInfo (bitmap.Height, bitmap.Width, bitmap.ColorType, bitmap.AlphaType);
		var turned = new SKBitmap (info);
		using var canvas = new SKCanvas (turned);
		canvas.SetMatrix (new SKMatrix (0, 1, 0, -1, 0, bitmap.Width, 0, 0, 1));
		DrawUnscaled (canvas, bitmap);
		return turned;
	}

	static void DrawUnscaled (SKCanvas canvas, SKBitmap bitmap)
	{
		using var image = SKImage.FromBitmap (bitmap);
		canvas.DrawImage (image, 0, 0, new SKSamplingOptions (SKFilterMode.Nearest));
	}

	// sRGB to the print profile, in place.
	// The shared decoder stores partial alpha premultiplied. lcms needs straight RGB,
	// and ApplyProfile leaves a premul buffer alone.
	static SKBitmap AsStraightAlpha (SKBitmap bitmap)
	{
		if (bitmap.AlphaType != SKAlphaType.Premul)
			return bitmap;
		int width = bitmap.Width;
		int height = bitmap.Height;
		var straight = new SKBitmap (new SKImageInfo (width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
		if (bitmap.RowBytes < width * 4 || straight.RowBytes < width * 4) {
			straight.Dispose ();
			return bitmap;
		}

		unsafe {
			byte* src = (byte*)bitmap.GetPixels ();
			byte* dst = (byte*)straight.GetPixels ();
			int srcStride = bitmap.RowBytes;
			int dstStride = straight.RowBytes;
			for (int y = 0; y < height; y++) {
				byte* srcRow = src + y * srcStride;
				byte* dstRow = dst + y * dstStride;
				for (int x = 0; x < width; x++) {
					int i = x * 4;
					byte alpha = srcRow[i + 3];
					if (alpha is 0 or 255) {
						dstRow[i] = alpha == 0 ? (byte)0 : srcRow[i];
						dstRow[i + 1] = alpha == 0 ? (byte)0 : srcRow[i + 1];
						dstRow[i + 2] = alpha == 0 ? (byte)0 : srcRow[i + 2];
					} else {
						dstRow[i] = (byte)(srcRow[i] * 255 / alpha);
						dstRow[i + 1] = (byte)(srcRow[i + 1] * 255 / alpha);
						dstRow[i + 2] = (byte)(srcRow[i + 2] * 255 / alpha);
					}
					dstRow[i + 3] = alpha;
				}
			}
		}

		bitmap.Dispose ();
		straight.NotifyPixelsChanged ();
		return straight;
	}

	public static void ApplyProfile (SKBitmap bitmap, Profile destination)
		=> ApplyProfile (bitmap, source: null, destination);

	// source null means the file is sRGB. The caller owns source. The buffer is packed
	// straight RGBA, so the alpha byte is copied rather than skipped. Premultiplied
	// pixels are left alone: lcms would treat that RGB as straight. A profile lcms
	// cannot apply to RGBA (a CMYK printer profile) degrades to unmanaged colour, as
	// the GTK pixbuf path does.
	public static void ApplyProfile (SKBitmap bitmap, Profile? source, Profile destination)
	{
		if (bitmap.ColorType != SKColorType.Rgba8888)
			return;
		if (bitmap.AlphaType is not (SKAlphaType.Opaque or SKAlphaType.Unpremul))
			return;
		if (bitmap.RowBytes != bitmap.Width * 4)
			return;
		if (destination is null)
			return;
		try {
			// CreateStandardRgb is process-wide. Do not dispose it, and do not dispose
			// a source the caller still holds.
			Profile from = source ?? Profile.CreateStandardRgb ();
			using var transform = new Transform (from, Format.Rgba8, destination, Format.Rgba8,
				Intent.Perceptual, CmsTransforms.For (print: true, copyAlpha: true));
			var pixels = bitmap.GetPixels ();
			transform.Apply (pixels, pixels, (uint)(bitmap.Width * bitmap.Height));
			bitmap.NotifyPixelsChanged ();
		} catch (Exception e) when (e is CmsException || ProfileCatalog.IsNativeLibraryMissing (e)) {
			Logger.Log.Warning ($"Leaving the image unmanaged: {e.Message}");
		}
	}
}
