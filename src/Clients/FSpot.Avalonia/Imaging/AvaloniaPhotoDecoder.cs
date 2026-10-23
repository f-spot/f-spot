//
// AvaloniaPhotoDecoder.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using FSpot.Imaging;
using FSpot.Utils;

using Hyena;

using SkiaSharp;

using TagLib.Image;

namespace FSpot.Avalonia.Imaging;

static class AvaloniaPhotoDecoder
{
	internal const int DefaultSvgMaxEdge = 2048;

	internal static Bitmap Decode (Stream stream, string? path = null, int? decodeToWidth = null, uint svgBackgroundArgb = 0)
		=> Decode (stream, path, decodeToWidth, svgBackgroundArgb, ImageOrientation.TopLeft);

	internal static Bitmap Decode (Stream stream, string? path, int? decodeToWidth, uint svgBackgroundArgb, ImageOrientation orientation)
		=> DecodeGate.Run (() => DecodeNow (stream, path, decodeToWidth, svgBackgroundArgb, orientation));

	static Bitmap DecodeNow (Stream stream, string? path, int? decodeToWidth, uint svgBackgroundArgb, ImageOrientation orientation)
	{
		if (SvgRasterizer.IsSvg (path, stream)) {
			int maxEdge = decodeToWidth ?? DefaultSvgMaxEdge;
			byte[] png = SvgRasterizer.RasterizePng (stream, maxEdge, svgBackgroundArgb);
			return new Bitmap (new MemoryStream (png));
		}

		if (stream.CanSeek)
			stream.Position = 0;
		using var codec = SKCodec.Create (stream) ?? throw new InvalidDataException ("Unable to decode the image.");
		var bitmap = SkiaScaledDecoder.TryDecode (codec, decodeToWidth)
			?? throw new InvalidDataException (DecodeBudget.TooLargeMessage);
		var oriented = SkiaOrient.Apply (bitmap, orientation);
		try {
			// The window composites Bgra. An Rgba buffer tagged the other way is the blue flash
			// before the color-managed copy replaces it.
			using var bgra = oriented.ColorType == SKColorType.Bgra8888 ? null : oriented.Copy (SKColorType.Bgra8888);
			SKBitmap pixels = bgra ?? oriented;
			var format = pixels.ColorType == SKColorType.Bgra8888 ? PixelFormat.Bgra8888 : PixelFormat.Rgba8888;
			var alpha = pixels.AlphaType == SKAlphaType.Opaque ? AlphaFormat.Opaque : AlphaFormat.Premul;
			return new Bitmap (
				format,
				alpha,
				pixels.GetPixels (),
				new PixelSize (pixels.Width, pixels.Height),
				new global::Avalonia.Vector (96, 96),
				pixels.RowBytes);
		} finally {
			oriented.Dispose ();
		}
	}

	internal static Bitmap DecodeFile (string path, int? decodeToWidth = null, uint svgBackgroundArgb = 0)
	{
		// FileShare.Delete lets a background decode finish while Windows removes the file.
		ImageOrientation orientation = SvgRasterizer.IsSvg (path, stream: null)
			? ImageOrientation.TopLeft
			: ReadOrientation (path);
		using var stream = new System.IO.FileStream (path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete);
		return Decode (stream, path, decodeToWidth, svgBackgroundArgb, orientation);
	}

	internal static ImageOrientation ReadOrientation (string path)
	{
		try {
			using var metadata = MetadataUtils.Parse (new SafeUri (path));
			return metadata?.ImageTag?.Orientation ?? ImageOrientation.TopLeft;
		} catch (Exception) {
			return ImageOrientation.TopLeft;
		}
	}
}
