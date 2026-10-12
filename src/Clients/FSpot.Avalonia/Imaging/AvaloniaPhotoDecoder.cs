//
// AvaloniaPhotoDecoder.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Media.Imaging;

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
		=> DecodeGate.Run (() => DecodeNow (stream, path, decodeToWidth, svgBackgroundArgb));

	static Bitmap DecodeNow (Stream stream, string? path, int? decodeToWidth, uint svgBackgroundArgb)
	{
		if (SvgRasterizer.IsSvg (path, stream)) {
			int maxEdge = decodeToWidth ?? DefaultSvgMaxEdge;
			byte[] png = SvgRasterizer.RasterizePng (stream, maxEdge, svgBackgroundArgb);
			return new Bitmap (new MemoryStream (png));
		}

		if (stream.CanSeek)
			stream.Position = 0;
		using var codec = SKCodec.Create (stream) ?? throw new InvalidDataException ("Unable to decode the image.");
		using var bitmap = SkiaScaledDecoder.TryDecode (codec, decodeToWidth)
			?? throw new InvalidDataException (DecodeBudget.TooLargeMessage);
		using var image = SKImage.FromBitmap (bitmap);
		using var data = image.Encode (SKEncodedImageFormat.Png, 100)
			?? throw new InvalidDataException ("Unable to decode the image.");
		var encoded = new MemoryStream ();
		data.SaveTo (encoded);
		encoded.Position = 0;
		return new Bitmap (encoded);
	}

	internal static Bitmap DecodeFile (string path, int? decodeToWidth = null, uint svgBackgroundArgb = 0)
	{
		using var stream = System.IO.File.OpenRead (path);
		var bitmap = Decode (stream, path, decodeToWidth, svgBackgroundArgb);
		if (SvgRasterizer.IsSvg (path, stream: null))
			return bitmap;
		return AvaloniaImagingBackend.ApplyOrientation (bitmap, ReadOrientation (path));
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
