// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

using FSpot.Settings;

namespace FSpot.Avalonia.ViewModels;

public enum ViewerTransparency
{
	Background,
	CheckPattern,
	CustomColor
}

// The GTK SingleView stored its window and view state under the Viewer* keys, and its
// preferences dialog the interpolation and transparency. Viewer windows and the viewer
// preferences dialog are separate windows, so a change made in the dialog is broadcast
// through Changed, the way GTK used Preferences.SettingChanged.
public static class ViewerSettings
{
	public const string NoneValue = "NONE";
	public const string CheckPatternValue = "CHECK_PATTERN";
	public const string ColorValue = "COLOR";
	public const string DefaultColor = "#000000";

	public static event EventHandler? Changed;

	public static bool Interpolation {
		get => Preferences.Get<bool> (Preferences.ViewerInterpolation);
		set {
			Preferences.Set (Preferences.ViewerInterpolation, value);
			Changed?.Invoke (null, EventArgs.Empty);
		}
	}

	public static ViewerTransparency Transparency {
		get => ParseTransparency (Preferences.Get<string> (Preferences.ViewerTransparency));
		set {
			Preferences.Set (Preferences.ViewerTransparency, FormatTransparency (value));
			Changed?.Invoke (null, EventArgs.Empty);
		}
	}

	public static string TransparentColor {
		get => NormalizeColor (Preferences.Get<string> (Preferences.ViewerTransColor));
		set {
			Preferences.Set (Preferences.ViewerTransColor, NormalizeColor (value));
			Changed?.Invoke (null, EventArgs.Empty);
		}
	}

	public static bool ShowToolbar {
		get => Preferences.Get<bool> (Preferences.ViewerShowToolbar);
		set => Preferences.Set (Preferences.ViewerShowToolbar, value);
	}

	public static bool ShowFilenames {
		get => Preferences.Get<bool> (Preferences.ViewerShawFilenames);
		set => Preferences.Set (Preferences.ViewerShawFilenames, value);
	}

	public static bool Maximized {
		get => Preferences.Get<bool> (Preferences.ViewerMaximized);
		set => Preferences.Set (Preferences.ViewerMaximized, value);
	}

	public static (int Width, int Height) Size {
		get => (Preferences.Get<int> (Preferences.ViewerWidth), Preferences.Get<int> (Preferences.ViewerHeight));
		set {
			Preferences.Set (Preferences.ViewerWidth, value.Width);
			Preferences.Set (Preferences.ViewerHeight, value.Height);
		}
	}

	// Anything GTK did not recognise fell through to NONE, drawn as the background.
	public static ViewerTransparency ParseTransparency (string? value)
		=> value switch {
			CheckPatternValue => ViewerTransparency.CheckPattern,
			ColorValue => ViewerTransparency.CustomColor,
			_ => ViewerTransparency.Background
		};

	public static string FormatTransparency (ViewerTransparency value)
		=> value switch {
			ViewerTransparency.CheckPattern => CheckPatternValue,
			ViewerTransparency.CustomColor => ColorValue,
			_ => NoneValue
		};

	// GTK wrote "#rrggbb" and parsed it back by substring; anything else would have thrown
	// there, so it falls back to the default colour here.
	public static string NormalizeColor (string? value)
	{
		if (TryParseColor (value, out byte r, out byte g, out byte b))
			return FormatColor (r, g, b);
		return DefaultColor;
	}

	public static bool TryParseColor (string? value, out byte r, out byte g, out byte b)
	{
		r = g = b = 0;
		if (value is null || value.Length != 7 || value[0] != '#')
			return false;
		return byte.TryParse (value.AsSpan (1, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out r)
			&& byte.TryParse (value.AsSpan (3, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out g)
			&& byte.TryParse (value.AsSpan (5, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out b);
	}

	public static string FormatColor (byte r, byte g, byte b)
		=> string.Create (CultureInfo.InvariantCulture, $"#{r:x2}{g:x2}{b:x2}");
}

// The GTK ImageView zoom: the slider is the zoom normalised between the fit-to-window
// minimum and a fixed maximum of 10, and the menu steps multiply by 1.1.
public static class ViewerZoom
{
	public const double Max = 10.0;
	public const double Factor = 1.1;
	public const double FallbackMin = 0.1;

	// ComputeMinZoom without upscaling: fit the image in the view, never above 1:1.
	public static double MinFor (double imageWidth, double imageHeight, double viewWidth, double viewHeight)
	{
		if (imageWidth <= 0 || imageHeight <= 0 || viewWidth <= 0 || viewHeight <= 0)
			return FallbackMin;
		double fit = Math.Min (viewWidth / imageWidth, viewHeight / imageHeight);
		return Math.Min (fit, 1.0);
	}

	public static double Clamp (double zoom, double min)
		=> Math.Clamp (zoom, min, Math.Max (min, Max));

	public static double Normalize (double zoom, double min)
		=> Max <= min ? 0 : Math.Clamp ((zoom - min) / (Max - min), 0, 1);

	public static double FromNormalized (double normalized, double min)
		=> Clamp ((Math.Clamp (normalized, 0, 1) * (Max - min)) + min, min);
}
