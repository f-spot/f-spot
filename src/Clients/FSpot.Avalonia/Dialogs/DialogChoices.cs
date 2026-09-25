// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Core;
using FSpot.Imaging;

namespace FSpot.Avalonia.Dialogs;

/// <summary>
/// What the Set Date Range dialog returned. A null range is "All dates", which clears the filter.
/// </summary>
public sealed record DateRangeChoice ((DateTime Start, DateTime End)? Range);

public sealed record CreateTagChoice (string Name, Category Parent, bool AutoIcon);

/// <summary>
/// The icon picked in Edit Tag Icon: a predefined (theme) icon, an image, or no image at all.
/// </summary>
public sealed record TagIconChoice (string? ThemeIconName, ImageBuffer? Icon)
{
	public static TagIconChoice NoImage { get; } = new (null, null);

	public bool Clears => ThemeIconName is null && Icon is null;

	public void ApplyTo (Tag tag)
	{
		if (ThemeIconName is not null) {
			// Drop any cached image first: Tag.Icon prefers it over the theme name.
			tag.Icon = null;
			tag.ThemeIconName = ThemeIconName;
			return;
		}
		tag.Icon = Icon;
	}
}

public sealed record EditTagChoice (string Name, Category Parent, TagIconChoice? Icon);

/// <summary>
/// Everything the Edit Tag Icon dialog needs about the tag.
/// </summary>
public sealed record TagIconSource (Tag Tag, IReadOnlyList<string> PhotoPaths);
