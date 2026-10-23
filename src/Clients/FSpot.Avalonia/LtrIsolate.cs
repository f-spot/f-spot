// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FSpot.Avalonia;

// Filenames and other Latin technical values stay left-to-right inside a
// right-to-left window. A Hebrew version name keeps the window direction.
public static class LtrIsolate
{
	public static IMultiValueConverter VersionFlow { get; } = new VersionFlowConverter ();

	public static IValueConverter ShowFilename { get; } = new FilenameVisibleConverter (filename: true);

	public static IValueConverter ShowOther { get; } = new FilenameVisibleConverter (filename: false);

	public static bool IsFilename (string? text)
	{
		if (string.IsNullOrEmpty (text))
			return false;

		int slash = text.LastIndexOfAny (['/', '\\']);
		string name = slash >= 0 ? text[(slash + 1)..] : text;
		int dot = name.LastIndexOf ('.');
		if (dot <= 0 || dot >= name.Length - 1)
			return false;

		string extension = name[(dot + 1)..];
		if (extension.Length is < 1 or > 5)
			return false;

		foreach (char c in extension) {
			if (!char.IsAsciiLetter (c))
				return false;
		}

		return true;
	}

	sealed class VersionFlowConverter : IMultiValueConverter
	{
		public object? Convert (IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
		{
			string? name = values.Count > 0 ? values[0] as string : null;
			var ambient = values.Count > 1 && values[1] is FlowDirection flow
				? flow
				: FlowDirection.LeftToRight;
			return IsFilename (name) ? FlowDirection.LeftToRight : ambient;
		}
	}

	sealed class FilenameVisibleConverter : IValueConverter
	{
		readonly bool filename;

		public FilenameVisibleConverter (bool filename)
			=> this.filename = filename;

		public object? Convert (object? value, Type targetType, object? parameter, CultureInfo culture)
			=> IsFilename (value as string) == filename;

		public object? ConvertBack (object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotSupportedException ();
	}
}
