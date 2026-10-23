// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Imaging;

using SkiaSharp;

namespace FSpot.Avalonia.Imaging;

// One decode path for thumbnails, the viewer, print and mail. Fits inside a box,
// never upscales, and refuses a bitmap whose pixel count would be huge.
static class SkiaScaledDecoder
{
	// One JPEG eighth below the request is the next coarser sample. Closer than that
	// is not worth a second decode of the original.
	internal static bool SampleIsCoarse (int decodedLongEdge, int wantedLongEdge)
		=> wantedLongEdge > 0 && decodedLongEdge > 0 && decodedLongEdge * 8 < wantedLongEdge * 7;

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
		// JPEG only scales by eighths. A sample within one eighth of the box is kept:
		// 2000px for a 2048 request must not decode the whole 12 megapixel frame.
		int wanted = boxEdge is int edge && edge > 0 ? Math.Min (edge, Math.Max (info.Width, info.Height)) : 0;
		int decodedEdge = bitmap is null ? 0 : Math.Max (bitmap.Width, bitmap.Height);
		bool coarse = SampleIsCoarse (decodedEdge, wanted);
		if ((bitmap is null || coarse) && !ExceedsCap (info.Width, info.Height)
			&& (target.Width != info.Width || target.Height != info.Height)) {
			bitmap?.Dispose ();
			bitmap = DecodeAt (codec, info.Width, info.Height, alpha);
		}
		if (bitmap is null)
			return null;

		if (boxEdge is int limit && limit > 0 && bitmap is not null
			&& Math.Max (bitmap.Width, bitmap.Height) != Math.Min (limit, Math.Max (info.Width, info.Height)))
			bitmap = FitBox (bitmap, Math.Min (limit, Math.Max (info.Width, info.Height)));
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

	static SKBitmap FitBox (SKBitmap bitmap, int maxEdge)
	{
		int longest = Math.Max (bitmap.Width, bitmap.Height);
		if (maxEdge <= 0 || longest == maxEdge)
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
