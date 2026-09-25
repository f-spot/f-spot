// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;

using FSpot;

namespace FSpot.Avalonia.Services;

// Restores and saves the main window's position, size and maximized state.
// Mirrors GTK's MainWindow, which restores MainWindowWidth/Height (only when
// a previous run saved something > 0, otherwise the designed default size
// wins) and MainWindowX/Y, then snapshots them again in Close().
public static class MainWindowStateStore
{
	public static void Restore (Window window)
	{
		int width = LibraryPreferences.MainWindowWidth;
		int height = LibraryPreferences.MainWindowHeight;
		if (width > 0 && height > 0) {
			window.Width = width;
			window.Height = height;
			window.Position = new PixelPoint (LibraryPreferences.MainWindowX, LibraryPreferences.MainWindowY);
			window.WindowStartupLocation = WindowStartupLocation.Manual;
		}
		if (LibraryPreferences.MainWindowMaximized)
			window.WindowState = WindowState.Maximized;
	}

	public static void Save (Window window)
	{
		bool maximized = window.WindowState == WindowState.Maximized;
		LibraryPreferences.MainWindowMaximized = maximized;
		if (maximized)
			return;
		LibraryPreferences.MainWindowX = window.Position.X;
		LibraryPreferences.MainWindowY = window.Position.Y;
		LibraryPreferences.MainWindowWidth = (int)window.Width;
		LibraryPreferences.MainWindowHeight = (int)window.Height;
	}
}
