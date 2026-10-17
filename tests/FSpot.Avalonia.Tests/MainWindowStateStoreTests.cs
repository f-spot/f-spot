// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;

using FSpot.Avalonia.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class MainWindowStateStoreTests
{
	[TestMethod]
	public void Intersects_CountsDipSizeInPhysicalPixels ()
	{
		var area = new PixelRect (0, 0, 1920, 1080);
		Assert.IsTrue (MainWindowStateStore.Intersects (area, 2, -900, 10, 800, 600));
	}

	[TestMethod]
	public void Intersects_RejectsAWindowThatOnlyOverlapsWhenTheScaleIsIgnored ()
	{
		var area = new PixelRect (0, 0, 1920, 1080);
		Assert.IsFalse (MainWindowStateStore.Intersects (area, 0.5, -500, 10, 800, 600));
	}

	[UiTestMethod]
	public void Save_KeepsTheNormalBoundsWhileMinimizedOrFullscreen ()
	{
		var previous = Remember ();
		try {
			LibraryPreferences.MainWindowX = 10;
			LibraryPreferences.MainWindowY = 20;
			LibraryPreferences.MainWindowWidth = 800;
			LibraryPreferences.MainWindowHeight = 600;
			LibraryPreferences.MainWindowMaximized = true;

			var minimized = new Window {
				Width = 120,
				Height = 80,
				Position = new PixelPoint (-3000, -3000),
				WindowState = WindowState.Minimized
			};
			MainWindowStateStore.Save (minimized);

			Assert.AreEqual (10, LibraryPreferences.MainWindowX);
			Assert.AreEqual (20, LibraryPreferences.MainWindowY);
			Assert.AreEqual (800, LibraryPreferences.MainWindowWidth);
			Assert.AreEqual (600, LibraryPreferences.MainWindowHeight);
			Assert.IsTrue (LibraryPreferences.MainWindowMaximized);

			var fullscreen = new Window {
				Width = 1440,
				Height = 900,
				Position = new PixelPoint (2000, 0),
				WindowState = WindowState.FullScreen
			};
			MainWindowStateStore.Save (fullscreen);

			Assert.AreEqual (10, LibraryPreferences.MainWindowX);
			Assert.AreEqual (20, LibraryPreferences.MainWindowY);
			Assert.AreEqual (800, LibraryPreferences.MainWindowWidth);
			Assert.AreEqual (600, LibraryPreferences.MainWindowHeight);
			Assert.IsTrue (LibraryPreferences.MainWindowMaximized);
		} finally {
			Restore (previous);
		}
	}

	[UiTestMethod]
	public void Note_KeepsThisSessionsBoundsWhenQuitHappensMinimized ()
	{
		var previous = Remember ();
		try {
			LibraryPreferences.MainWindowX = 10;
			LibraryPreferences.MainWindowY = 20;
			LibraryPreferences.MainWindowWidth = 800;
			LibraryPreferences.MainWindowHeight = 600;
			LibraryPreferences.MainWindowMaximized = true;

			var window = new Window {
				Width = 640,
				Height = 480,
				Position = new PixelPoint (40, 50),
				WindowState = WindowState.Normal
			};
			MainWindowStateStore.Note (window);

			window.WindowState = WindowState.Minimized;
			window.Width = 120;
			window.Height = 80;
			window.Position = new PixelPoint (-3000, -3000);
			MainWindowStateStore.Save (window);

			Assert.AreEqual (40, LibraryPreferences.MainWindowX);
			Assert.AreEqual (50, LibraryPreferences.MainWindowY);
			Assert.AreEqual (640, LibraryPreferences.MainWindowWidth);
			Assert.AreEqual (480, LibraryPreferences.MainWindowHeight);
			Assert.IsFalse (LibraryPreferences.MainWindowMaximized);
		} finally {
			Restore (previous);
		}
	}

	[UiTestMethod]
	public void Save_StillRecordsAMaximizedWindowWithoutItsTransientSize ()
	{
		var previous = Remember ();
		try {
			LibraryPreferences.MainWindowX = 10;
			LibraryPreferences.MainWindowY = 20;
			LibraryPreferences.MainWindowWidth = 800;
			LibraryPreferences.MainWindowHeight = 600;
			LibraryPreferences.MainWindowMaximized = false;

			var window = new Window {
				Width = 100,
				Height = 100,
				Position = new PixelPoint (-1, -1),
				WindowState = WindowState.Maximized
			};
			MainWindowStateStore.Save (window);

			Assert.AreEqual (10, LibraryPreferences.MainWindowX);
			Assert.AreEqual (800, LibraryPreferences.MainWindowWidth);
			Assert.IsTrue (LibraryPreferences.MainWindowMaximized);
		} finally {
			Restore (previous);
		}
	}

	[UiTestMethod]
	public void Restore_SkipsARectangleThatIsNotOnACurrentScreen ()
	{
		AvaloniaApp.Ensure ();
		var previous = Remember ();
		var window = new Window { Width = 200, Height = 150, WindowStartupLocation = WindowStartupLocation.CenterScreen };
		window.Show ();
		try {
			int x = 100_000;
			int y = 100_000;
			if (window.Screens?.All is { Count: > 0 } screens) {
				int right = 0;
				int bottom = 0;
				foreach (var screen in screens) {
					if (screen.WorkingArea.Right > right)
						right = screen.WorkingArea.Right;
					if (screen.WorkingArea.Bottom > bottom)
						bottom = screen.WorkingArea.Bottom;
				}
				x = right + 2000;
				y = bottom + 2000;
			}

			LibraryPreferences.MainWindowX = x;
			LibraryPreferences.MainWindowY = y;
			LibraryPreferences.MainWindowWidth = 640;
			LibraryPreferences.MainWindowHeight = 480;
			LibraryPreferences.MainWindowMaximized = false;

			MainWindowStateStore.Restore (window);

			Assert.AreEqual (200, window.Width);
			Assert.AreEqual (150, window.Height);
		} finally {
			window.Close ();
			Restore (previous);
		}
	}

	[UiTestMethod]
	public void Restore_PlacesARectangleThatSitsOnACurrentScreen ()
	{
		AvaloniaApp.Ensure ();
		var previous = Remember ();
		var window = new Window { Width = 200, Height = 150 };
		window.Show ();
		try {
			var screens = window.Screens?.All;
			if (screens is null || screens.Count == 0)
				Assert.Fail ("headless run has no screen to place a window on");

			var area = screens[0].WorkingArea;
			int x = area.X + 8;
			int y = area.Y + 8;
			LibraryPreferences.MainWindowX = x;
			LibraryPreferences.MainWindowY = y;
			LibraryPreferences.MainWindowWidth = 640;
			LibraryPreferences.MainWindowHeight = 480;
			LibraryPreferences.MainWindowMaximized = false;

			MainWindowStateStore.Restore (window);

			Assert.AreEqual (640, window.Width);
			Assert.AreEqual (480, window.Height);
			Assert.AreEqual (x, window.Position.X);
			Assert.AreEqual (y, window.Position.Y);
		} finally {
			window.Close ();
			Restore (previous);
		}
	}

	readonly record struct Bounds (int X, int Y, int Width, int Height, bool Maximized);

	static Bounds Remember ()
		=> new (
			LibraryPreferences.MainWindowX,
			LibraryPreferences.MainWindowY,
			LibraryPreferences.MainWindowWidth,
			LibraryPreferences.MainWindowHeight,
			LibraryPreferences.MainWindowMaximized);

	static void Restore (Bounds bounds)
	{
		LibraryPreferences.MainWindowX = bounds.X;
		LibraryPreferences.MainWindowY = bounds.Y;
		LibraryPreferences.MainWindowWidth = bounds.Width;
		LibraryPreferences.MainWindowHeight = bounds.Height;
		LibraryPreferences.MainWindowMaximized = bounds.Maximized;
	}
}
