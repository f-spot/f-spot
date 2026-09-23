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

	public static int DecodeWidth (double uiScale)
		=> Math.Min (MaxPixels, HiDpi.PixelSize (Dip, uiScale));
}
