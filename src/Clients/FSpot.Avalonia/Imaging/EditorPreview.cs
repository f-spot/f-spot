//
// EditorPreview.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia;

/// <summary>
/// Decode width for the one photo open in the editor or slideshow.
/// A camera original is not held uncompressed; the grid never uses this size.
/// </summary>
public static class EditorPreview
{
	public const double Dip = 1600;
	public const int MaxPixels = 4096;

	// The editor pane on a retina screen is covered by 2048 pixels. Decoding the
	// full 1600dip times the scale, then converting every pixel, is what made
	// opening a photo wait.
	public const int OpenEdge = 2048;

	public static int DecodeWidth (double uiScale)
	{
		int scaled = HiDpi.PixelSize (Dip, uiScale);
		if (scaled < Dip)
			scaled = (int)Dip;
		return Math.Min (OpenEdge, scaled);
	}
}
