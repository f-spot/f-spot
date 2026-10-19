// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Imaging;

// lcms sees straight RGB. Avalonia composites a Premul surface.
// Alpha 0 and 255 are already equal in both associations.
static class StraightAlpha
{
	internal static void UnpremultiplyRgba (byte[] rgba)
	{
		for (int i = 0; i < rgba.Length; i += 4) {
			byte alpha = rgba[i + 3];
			if (alpha is 0 or 255)
				continue;
			rgba[i] = Unpremultiply (rgba[i], alpha);
			rgba[i + 1] = Unpremultiply (rgba[i + 1], alpha);
			rgba[i + 2] = Unpremultiply (rgba[i + 2], alpha);
		}
	}

	internal static byte Premultiply (byte channel, byte alpha)
	{
		if (alpha == 255)
			return channel;
		if (alpha == 0)
			return 0;
		return (byte)(channel * alpha / 255);
	}

	static byte Unpremultiply (byte channel, byte alpha)
		=> (byte)(channel * 255 / alpha);
}
