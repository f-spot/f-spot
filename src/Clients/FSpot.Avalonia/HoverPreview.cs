//
// HoverPreview.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia;

/// <summary>
/// GTK PreviewPopup sizes the image to a third of the average screen edge and
/// labels it with the description, the time, and the file name.
/// </summary>
public static class HoverPreview
{
	public static int Edge (int screenWidth, int screenHeight)
	{
		int width = Math.Max (0, screenWidth);
		int height = Math.Max (0, screenHeight);
		return Math.Max (1, ((width + height) / 2) / 3);
	}

	public static string Caption (string? description, DateTime time, string? name)
	{
		string line = $"{time}   {name}";
		return string.IsNullOrEmpty (description) ? line : description + Environment.NewLine + line;
	}
}
