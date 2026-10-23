//
// ThemeInk.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace FSpot.Avalonia.Controls;

public static class ThemeInk
{
	// Year captions sit on the timeline chrome. Fluent's base-high color (and a brush
	// built from it with StaticResource) can be the wrong luminance, or a translucent
	// black that paints as gray. Keep the theme color only when it still reads.
	public static Color Foreground (Control host)
	{
		bool dark = OnDarkPaper (host);
		var theme = host.ActualThemeVariant;
		if (TryInk (host, "SystemBaseHighColor", theme, out var ink) && ReadsOn (ink, dark))
			return Color.FromRgb (ink.R, ink.G, ink.B);
		return dark ? Colors.White : Colors.Black;
	}

	public static Color Chrome (Control host)
	{
		if (TryInk (host, "SystemControlBackgroundChromeMediumLowBrush", host.ActualThemeVariant, out var paper))
			return paper;
		return Colors.Gray;
	}

	static bool OnDarkPaper (Control host)
	{
		var theme = host.ActualThemeVariant;
		if (TryInk (host, "SystemControlBackgroundChromeMediumLowBrush", theme, out var paper)) {
			double luminance = Painted (paper, dark: false);
			if (luminance >= 0.62)
				return false;
			if (luminance <= 0.35)
				return true;
		}
		return theme == ThemeVariant.Dark;
	}

	static bool TryInk (Control host, string key, ThemeVariant theme, out Color color)
	{
		color = default;
		if (!host.TryFindResource (key, theme, out var value) || value is null)
			return false;
		if (value is Color ink) {
			color = ink;
			return true;
		}
		if (value is not ISolidColorBrush brush)
			return false;
		color = brush.Color;
		if (brush.Opacity < 1) {
			int alpha = (int)Math.Round (color.A * Math.Clamp (brush.Opacity, 0, 1));
			color = Color.FromArgb ((byte)alpha, color.R, color.G, color.B);
		}
		return true;
	}

	static bool ReadsOn (Color ink, bool dark)
	{
		double luminance = Painted (ink, dark);
		return dark ? luminance > 0.62 : luminance < 0.35;
	}

	// Alpha is composited onto the track. RGB-only luminance calls translucent black "dark".
	static double Painted (Color ink, bool dark)
	{
		double alpha = ink.A / 255.0;
		double paper = dark ? 0 : 255;
		double r = ink.R * alpha + paper * (1 - alpha);
		double g = ink.G * alpha + paper * (1 - alpha);
		double b = ink.B * alpha + paper * (1 - alpha);
		return (r + g + b) / (3.0 * 255);
	}
}
