// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Interactivity;

using FSpot.Resources.Lang;
using FSpot.Utils;

using Hyena;

namespace FSpot.Avalonia;

// One row of the failed-import details list: the file name plus the folder
// it lives in, matching the (hidden-header) two column TreeView in
// FSpot.Gtk's ImportFailureDialog.
public sealed record ImportFailureItem (string FileName, string Path);

// Avalonia counterpart of FSpot.Gtk's ImportFailureDialog: an error icon,
// a bold header, an explanatory message, and a "Details" expander listing
// every file that failed to import.
public partial class ImportFailureWindow : Window
{
	public ImportFailureWindow ()
		=> InitializeComponent ();

	public static ImportFailureWindow Create (IEnumerable<SafeUri> failed)
	{
		var window = new ImportFailureWindow ();
		window.Apply (failed);
		return window;
	}

	void Apply (IEnumerable<SafeUri> failed)
	{
		HeaderBlock.Text = Strings.SomeFilesFailedToImport;
		MessageBlock.Text = Strings.SomeFilesCouldNotBeImported;
		FileList.ItemsSource = failed
			.Select (uri => new ImportFailureItem (uri.GetFilename (), uri.GetBaseUri ().AbsolutePath))
			.ToList ();
	}

	void OnClose (object? sender, RoutedEventArgs e)
		=> Close ();
}
