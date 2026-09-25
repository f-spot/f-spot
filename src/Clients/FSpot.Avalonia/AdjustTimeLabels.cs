// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Resources.Lang;

namespace FSpot.Avalonia;

/// <summary>
/// Section headers for the Adjust Time dialog. GTK's AdjustTimeDialog.ui gives each frame a
/// Pango "&lt;b&gt;...&lt;/b&gt;" caption; the Avalonia window renders the same Strings text in
/// a bold TextBlock instead, so the markup is stripped here rather than shown literally.
/// </summary>
public static class AdjustTimeLabels
{
	public static string ReferencePhoto => GtkMarkupText.StripBold (Strings.BReferencePhotoB);
	public static string Action => GtkMarkupText.StripBold (Strings.BActionB);
	public static string Photos => GtkMarkupText.StripBold (Strings.BPhotosB);
}
