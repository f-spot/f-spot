// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia;

public static class HiDpi
{
	public const double ImportListDip = 128;
	public const double ImportPreviewDip = 512;

	public static int PixelSize (double dip, double scale)
	{
		if (scale <= 0)
			scale = 1;
		return Math.Max (1, (int)Math.Round (Math.Max (0, dip) * scale));
	}
}
