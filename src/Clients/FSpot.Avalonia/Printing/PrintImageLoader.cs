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
		return Orient (bitmap, codec.EncodedOrigin);
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

	// ColorManagement.ApplyProfile: sRGB to the print profile, in place. Like GTK it
	// leaves images with alpha alone and degrades to unmanaged colour on a CMS failure.
	public static void ApplyProfile (SKBitmap bitmap, Profile destination)
	{
		if (bitmap.AlphaType != SKAlphaType.Opaque || bitmap.ColorType != SKColorType.Rgba8888)
			return;
		if (bitmap.RowBytes != bitmap.Width * 4)
			return;
		try {
			using var source = Profile.CreateStandardRgb ();
			using var transform = new Transform (source, Format.Rgba8, destination, Format.Rgba8, Intent.Perceptual, 0);
			var pixels = bitmap.GetPixels ();
			transform.Apply (pixels, pixels, (uint)(bitmap.Width * bitmap.Height));
			bitmap.NotifyPixelsChanged ();
		} catch (Exception e) when (e is CmsException || ProfileCatalog.IsNativeLibraryMissing (e)) {
			Logger.Log.Warning ($"Leaving the image unmanaged: {e.Message}");
		}
	}
}
