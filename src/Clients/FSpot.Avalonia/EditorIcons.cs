// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Avalonia.Data.Converters;

namespace FSpot.Avalonia;

public static class EditorIcons
{
	public static readonly IValueConverter Uri = new FuncValueConverter<string?, string?> (UriFor);
	public static readonly IValueConverter IsAvailable = new FuncValueConverter<string?, bool> (name => UriFor (name) != null);

	public static string? UriFor (string? iconName)
		=> ChromeIcons.AvaresUri (iconName, PackedIconFiles.All);
}

public static class TagIcons
{
	public static string UriFor (string? iconName)
	{
		if (string.IsNullOrEmpty (iconName))
			return "avares://FSpot.Avalonia/Icons/tag-16.svg";
		if (iconName.Contains (':', StringComparison.Ordinal))
			return iconName;
		return ChromeIcons.AvaresUri (iconName, PackedIconFiles.All)
			?? "avares://FSpot.Avalonia/Icons/tag-16.svg";
	}
}
