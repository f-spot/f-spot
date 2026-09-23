// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Styling;

using FSpot;

namespace FSpot.Avalonia;

public static class AppearanceTheme
{
	public const string System = "System";
	public const string Light = "Light";
	public const string Dark = "Dark";

	public static string Normalize (string? value)
		=> value is Light or Dark or System ? value : System;

	public static ThemeVariant ToVariant (string? value)
		=> Normalize (value) switch {
			Light => ThemeVariant.Light,
			Dark => ThemeVariant.Dark,
			_ => ThemeVariant.Default
		};

	public static void Apply (Application? application)
	{
		if (application is null)
			return;
		application.RequestedThemeVariant = ToVariant (LibraryPreferences.ColorScheme);
	}
}
