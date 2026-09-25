// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace FSpot.Avalonia.Dialogs;

/// <summary>
/// The "Predefined icons" in Edit Tag Icon. GTK listed the icon theme's Emblems context;
/// the Avalonia client offers the emblem icons it packs.
/// </summary>
public static partial class PredefinedTagIcons
{
	public static IReadOnlyList<string> Names (IEnumerable<string> packedFiles)
		=> packedFiles
			.Select (file => SizeSuffix ().Replace (Path.GetFileNameWithoutExtension (file), string.Empty))
			.Where (stem => stem.StartsWith ("emblem-", StringComparison.OrdinalIgnoreCase))
			.Distinct (StringComparer.OrdinalIgnoreCase)
			.Order (StringComparer.OrdinalIgnoreCase)
			.ToList ();

	[GeneratedRegex ("-(16|22|24|32|48)$")]
	private static partial Regex SizeSuffix ();
}

/// <summary>
/// Binds an icon name to the packed SVG the tag sidebar would show for it.
/// </summary>
public sealed class TagIconPathConverter : global::Avalonia.Data.Converters.IValueConverter
{
	public static TagIconPathConverter Instance { get; } = new ();

	public object? Convert (object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
		=> TagIcons.UriFor (value as string);

	public object? ConvertBack (object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
		=> throw new NotSupportedException ();
}
