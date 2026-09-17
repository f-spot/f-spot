// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Imaging;

using SkiaSharp;

namespace FSpot.Avalonia.Imaging;

// One decode path for thumbnails, the viewer, print and mail. Fits inside a box,
// never upscales, and refuses a bitmap whose pixel count would be huge.
static class SkiaScaledDecoder
{
	internal static float BoxScale (int width, int height, int? boxEdge)
	{
		if (boxEdge is not int edge || edge <= 0 || width <= 0 || height <= 0)
			return 1f;
		return Math.Min (1f, Math.Min ((float)edge / width, (float)edge / height));
	}

	internal static bool ExceedsCap (int width, int height)
		=> DecodeBudget.Exceeds (width, height);

	internal static SKBitmap? TryDecode (SKCodec codec, int? boxEdge)
	{
		var info = codec.Info;
		float scale = BoxScale (info.Width, info.Height, boxEdge);
		if (scale >= 1f && ExceedsCap (info.Width, info.Height))
			return null;

		var target = scale >= 1f
			? new SKSizeI (info.Width, info.Height)
			: codec.GetScaledDimensions (scale);
		if (ExceedsCap (target.Width, target.Height))
			return null;

		var alpha = info.AlphaType == SKAlphaType.Opaque ? SKAlphaType.Opaque : SKAlphaType.Premul;
		var bitmap = DecodeAt (codec, target.Width, target.Height, alpha);
		// JPEG only scales by eighths. When that lands well below the box, decode the
		// original and shrink — but never when the original itself is over the cap.
		int wanted = boxEdge is int edge && edge > 0 ? Math.Min (edge, Math.Max (info.Width, info.Height)) : 0;
		bool coarse = bitmap is not null && wanted > 0 && Math.Max (bitmap.Width, bitmap.Height) < wanted;
		if ((bitmap is null || coarse) && !ExceedsCap (info.Width, info.Height)
			&& (target.Width != info.Width || target.Height != info.Height)) {
			bitmap?.Dispose ();
			bitmap = DecodeAt (codec, info.Width, info.Height, alpha);
		}
		if (bitmap is null)
			return null;

		if (boxEdge is int limit && limit > 0 && (bitmap.Width > limit || bitmap.Height > limit))
			bitmap = Shrink (bitmap, limit);
		return bitmap;
	}

	static SKBitmap? DecodeAt (SKCodec codec, int width, int height, SKAlphaType alpha)
	{
		var decodeInfo = new SKImageInfo (width, height, SKColorType.Rgba8888, alpha);
		var bitmap = new SKBitmap (decodeInfo);
		var result = codec.GetPixels (decodeInfo, bitmap.GetPixels ());
		if (result is SKCodecResult.Success or SKCodecResult.IncompleteInput)
			return bitmap;
		bitmap.Dispose ();
		return null;
	}

	static SKBitmap Shrink (SKBitmap bitmap, int maxEdge)
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
}
