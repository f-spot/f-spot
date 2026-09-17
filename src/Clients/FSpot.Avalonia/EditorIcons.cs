// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Data.Converters;

namespace FSpot.Avalonia;

public static class EditorIcons
{
	public static readonly IValueConverter Uri = new FuncValueConverter<string?, string?> (UriFor);
	public static readonly IValueConverter IsAvailable = new FuncValueConverter<string?, bool> (name => UriFor (name) != null);

	public static string? UriFor (string? iconName)
	{
		if (string.IsNullOrEmpty (iconName))
			return null;

		string? file = iconName switch {
			"adjust-colors" => "adjust-colors-22.svg",
			"align-horizon" => "align-horizon-22.svg",
			"autocolor" => "autocolor-22.svg",
			"color-desaturate" => "color-desaturate-22.svg",
			"color-sepia" => "color-sepia-22.svg",
			_ => null
		};
		return file is null ? null : $"avares://FSpot.Avalonia/Icons/{file}";
	}
}
