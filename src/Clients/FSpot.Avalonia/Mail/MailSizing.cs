// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

using FSpot.Resources.Lang;

namespace FSpot.Avalonia.Mail;

// The size arithmetic of the GTK SendEmail dialog.
public static class MailSizing
{
	// Index 0 is Original; then Tiny, Small, Medium, Large and Extra large, in pixels.
	public static IReadOnlyList<int> Sizes { get; } = [0, 320, 480, 640, 800, 1024];

	public const int MediumIndex = 3;

	// Estimated size relative to the original after shrinking to each size.
	public static IReadOnlyList<double> ReferenceScales { get; } = [0, 0.0186, 0.0348, 0.0532, 0.0826, 0.1234];

	// SendEmail rescales the reference table by how much the first photo actually
	// shrank when resized to medium.
	public static double[] Calibrate (long originalSize, long mediumSize)
	{
		var scales = ReferenceScales.ToArray ();
		if (originalSize <= 0)
			return scales;
		double scalePercentage = 1 - ((float)(originalSize - mediumSize) / originalSize);
		double scaleScale = scalePercentage / ReferenceScales[MediumIndex];
		for (int k = 0; k < scales.Length; k++)
			scales[k] = ReferenceScales[k] * scaleScale;
		return scales;
	}

	public static long Estimate (long totalOriginalSize, int sizeIndex, IReadOnlyList<double> scales)
		=> sizeIndex <= 0 || sizeIndex >= scales.Count
			? totalOriginalSize
			: Convert.ToInt64 (totalOriginalSize * scales[sizeIndex]);

	// Only JPEGs are resized; anything else forces Original (SendEmail's mime check).
	public static bool IsJpeg (string path)
	{
		string extension = Path.GetExtension (path);
		return extension.Equals (".jpg", StringComparison.OrdinalIgnoreCase)
			|| extension.Equals (".jpeg", StringComparison.OrdinalIgnoreCase)
			|| extension.Equals (".jpe", StringComparison.OrdinalIgnoreCase);
	}

	// The remembered Preferences.ExportEmailSize, clamped to a valid choice.
	public static int ClampIndex (int index)
		=> index >= 0 && index < Sizes.Count ? index : MediumIndex;

	// GLib.Format.SizeForDisplay: SI units with one decimal.
	public static string FormatSize (long bytes)
	{
		if (bytes < 1000)
			return string.Format (CultureInfo.CurrentCulture, "{0} {1}", bytes, Strings.Bytes);
		string[] units = ["kB", "MB", "GB", "TB"];
		double value = bytes;
		int unit = -1;
		do {
			value /= 1000;
			unit++;
		} while (value >= 1000 && unit < units.Length - 1);
		return string.Format (CultureInfo.CurrentCulture, "{0:0.0} {1}", value, units[unit]);
	}
}
