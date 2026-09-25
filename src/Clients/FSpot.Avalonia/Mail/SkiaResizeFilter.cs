// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.Printing;
using FSpot.Filters;

using Hyena;

using SkiaSharp;

namespace FSpot.Avalonia.Mail;

// ResizeFilter without Gdk: leaves photos that already fit alone, otherwise writes
// an upright JPEG (quality 95, as PixbufUtils.CreateDerivedVersion) no larger than Size.
public sealed class SkiaResizeFilter : IFilter
{
	public SkiaResizeFilter (uint size)
	{
		Size = size;
	}

	public uint Size { get; }

	public bool Convert (FilterRequest request)
	{
		string source = request.Current.LocalPath;
		using (var codec = SKCodec.Create (source)) {
			if (codec is null)
				throw new InvalidDataException ($"Unable to decode {source}");
			if (codec.Info.Width < Size && codec.Info.Height < Size)
				return false;
		}

		using var bitmap = PrintImageLoader.Load (source, (int)Size)
			?? throw new InvalidDataException ($"Unable to decode {source}");
		var destination = request.TempUri ("jpg");
		using (var image = SKImage.FromBitmap (bitmap))
		using (var data = image.Encode (SKEncodedImageFormat.Jpeg, 95))
		using (var stream = File.Create (destination.LocalPath))
			data.SaveTo (stream);
		request.Current = destination;
		return true;
	}
}
