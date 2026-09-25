// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Services;

// What the standalone viewer's view model asks of its window (GTK SingleView).
public interface IViewerShell
{
	void Close ();
	void OpenNewWindow (IReadOnlyList<string> paths);
	Task<IReadOnlyList<string>> PickFilesAsync (string title, bool allowMultiple);
	Task<string?> PickFolderAsync (string title);
	void ToggleFullscreen ();
	void ShowSlideshow (IReadOnlyList<string> paths, int startIndex);
	Task ShowPreferencesAsync ();
	Task ShowAboutAsync ();
	Task ShowWarningAsync (string title, string message);
	Task SetDesktopBackgroundAsync (string path);
}
