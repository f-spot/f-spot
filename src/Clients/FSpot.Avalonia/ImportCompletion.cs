// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.Services;
using FSpot.Resources.Lang;
using FSpot.Utils;

using Hyena;

namespace FSpot.Avalonia;

public static class ImportCompletion
{
	public static bool ShouldReloadCatalog (int photosImported)
		=> photosImported > 0;

	public static string FormatFailures (IEnumerable<SafeUri> failed)
	{
		var names = failed
			.Select (uri => uri.GetFilename ())
			.Where (name => !string.IsNullOrEmpty (name))
			.ToList ();
		if (names.Count == 0)
			return string.Empty;

		return Strings.ImportFailures + Environment.NewLine + string.Join (Environment.NewLine, names);
	}

	public static async Task ReportFailuresAsync (IAvaloniaShell shell, IEnumerable<SafeUri> failed)
	{
		string message = FormatFailures (failed);
		if (message.Length == 0)
			return;

		await shell.ShowWarningAsync (Strings.ImportFailures, message);
	}
}
