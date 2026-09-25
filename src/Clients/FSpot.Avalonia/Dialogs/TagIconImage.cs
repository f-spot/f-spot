// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Imaging;
using FSpot.Settings;

namespace FSpot.Avalonia.Dialogs;

/// <summary>
/// Builds a tag icon from part of a photo, like the GTK PixbufUtils.TagIconFromPixbuf:
/// crop to the (square) selection, center-crop what is left to a square, and scale it
/// to the large tag icon size.
/// </summary>
public static class TagIconImage
{
	public const int Size = (int)IconSize.Large;

	/// <summary>
	/// The GTK icon selection is locked to a 1:1 ratio; keep the largest centered square.
	/// </summary>
	public static PixelRect Square (PixelRect selection)
	{
		if (selection.IsEmpty)
			return selection;
		int side = Math.Min (selection.Width, selection.Height);
		return new PixelRect (
			selection.X + (selection.Width - side) / 2,
			selection.Y + (selection.Height - side) / 2,
			side, side);
	}

	public static PixelRect CenterSquare (int width, int height)
	{
		int side = Math.Min (width, height);
		return new PixelRect ((width - side) / 2, (height - side) / 2, side, side);
	}

	public static ImageBuffer Create (ImageBuffer source, PixelRect? selection, Func<ImageBuffer, int, int, ImageBuffer> scale)
	{
		var image = source;
		if (selection is { IsEmpty: false } area)
			image = ImageBuffers.Crop (image, Square (area));
		if (image.Width != image.Height)
			image = ImageBuffers.Crop (image, CenterSquare (image.Width, image.Height));
		return scale (image, Size, Size);
	}
}
