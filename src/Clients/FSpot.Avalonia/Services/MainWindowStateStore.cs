// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

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
		if (!TryPlace (window))
			window.Opened += PlaceWhenScreensExist;
		if (LibraryPreferences.MainWindowMaximized)
			window.WindowState = WindowState.Maximized;
	}

	public static void Save (Window window)
	{
		// Minimized and fullscreen frames are transient. Keep the last normal bounds
		// and whatever maximized flag the previous normal or maximized save stored.
		if (window.WindowState is WindowState.Minimized or WindowState.FullScreen)
			return;
		bool maximized = window.WindowState == WindowState.Maximized;
		LibraryPreferences.MainWindowMaximized = maximized;
		if (maximized)
			return;
		LibraryPreferences.MainWindowX = window.Position.X;
		LibraryPreferences.MainWindowY = window.Position.Y;
		LibraryPreferences.MainWindowWidth = (int)window.Width;
		LibraryPreferences.MainWindowHeight = (int)window.Height;
	}

	static void PlaceWhenScreensExist (object? sender, EventArgs e)
	{
		if (sender is not Window window)
			return;
		window.Opened -= PlaceWhenScreensExist;
		TryPlace (window);
	}

	static bool TryPlace (Window window)
	{
		int width = LibraryPreferences.MainWindowWidth;
		int height = LibraryPreferences.MainWindowHeight;
		if (width <= 0 || height <= 0)
			return true;
		var screens = window.Screens?.All;
		if (screens is null || screens.Count == 0)
			return false;
		int x = LibraryPreferences.MainWindowX;
		int y = LibraryPreferences.MainWindowY;
		if (!IntersectsAScreen (screens, x, y, width, height))
			return true;
		window.Width = width;
		window.Height = height;
		window.Position = new PixelPoint (x, y);
		window.WindowStartupLocation = WindowStartupLocation.Manual;
		return true;
	}

	static bool IntersectsAScreen (IReadOnlyList<Screen> screens, int x, int y, int width, int height)
	{
		foreach (var screen in screens) {
			if (Intersects (screen.WorkingArea, screen.Scaling, x, y, width, height))
				return true;
		}
		return false;
	}

	internal static bool Intersects (PixelRect area, double scaling, int x, int y, int width, int height)
	{
		if (double.IsNaN (scaling) || double.IsInfinity (scaling) || scaling <= 0)
			scaling = 1;
		double right = x + width * scaling;
		double bottom = y + height * scaling;
		return x < area.Right && area.X < right && y < area.Bottom && area.Y < bottom;
	}
}
