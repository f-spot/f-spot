// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using FSpot.Avalonia.Controls;
using FSpot.Imaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ImageSelectionOverlayTests
{
	[UiTestMethod]
	public void CompleteSelection_CommitsTheMappedRect ()
	{
		AvaloniaApp.Ensure ();
		var command = new RecordingCommand ();
		var overlay = ShowOverlay (new ImageSelectionOverlay {
			Width = 200,
			Height = 100,
			ImageWidth = 200,
			ImageHeight = 100,
			SelectionCommand = command
		});

		overlay.CompleteSelection (new Point (10, 10), new Point (50, 40));

		Assert.IsNotNull (command.Last);
		Assert.AreEqual (10, command.Last.Value.X);
		Assert.AreEqual (10, command.Last.Value.Y);
		Assert.AreEqual (40, command.Last.Value.Width);
		Assert.AreEqual (30, command.Last.Value.Height);
	}

	[UiTestMethod]
	public void CompleteSelection_UsesPreviewPixelSizeWhenImageWidthIsZero ()
	{
		AvaloniaApp.Ensure ();
		var command = new RecordingCommand ();
		using var preview = new WriteableBitmap (
			new PixelSize (100, 50),
			new global::Avalonia.Vector (96, 96),
			PixelFormat.Bgra8888,
			AlphaFormat.Unpremul);
		var overlay = ShowOverlay (new ImageSelectionOverlay {
			Width = 100,
			Height = 50,
			ImageWidth = 0,
			ImageHeight = 0,
			Preview = preview,
			SelectionCommand = command
		});

		overlay.CompleteSelection (new Point (0, 0), new Point (100, 50));

		Assert.IsNotNull (command.Last);
		Assert.AreEqual (100, command.Last.Value.Width);
		Assert.AreEqual (50, command.Last.Value.Height);
	}

	[UiTestMethod]
	public void CompleteSelection_AppliesPanAndZoomBeforeMapping ()
	{
		AvaloniaApp.Ensure ();
		var command = new RecordingCommand ();
		var overlay = ShowOverlay (new ImageSelectionOverlay {
			Width = 200,
			Height = 100,
			ImageWidth = 200,
			ImageHeight = 100,
			Zoom = 2,
			SelectionCommand = command
		});

		overlay.CompleteSelection (new Point (0, 0), new Point (200, 100));

		Assert.IsNotNull (command.Last);
		Assert.AreEqual (100, command.Last.Value.Width);
		Assert.AreEqual (50, command.Last.Value.Height);
	}

	[UiTestMethod]
	public void CompleteSelection_DoesNotExecuteWhenImageSizeIsMissing ()
	{
		AvaloniaApp.Ensure ();
		var command = new RecordingCommand ();
		var overlay = ShowOverlay (new ImageSelectionOverlay {
			Width = 200,
			Height = 100,
			SelectionCommand = command
		});

		overlay.CompleteSelection (new Point (10, 10), new Point (50, 40));

		Assert.IsTrue (command.Last is { IsEmpty: true });
	}

	[UiTestMethod]
	public void CompleteSelection_ClickClearsThePreviousRegion ()
	{
		AvaloniaApp.Ensure ();
		var command = new RecordingCommand ();
		var overlay = ShowOverlay (new ImageSelectionOverlay {
			Width = 200,
			Height = 100,
			ImageWidth = 200,
			ImageHeight = 100,
			SelectionCommand = command
		});

		overlay.CompleteSelection (new Point (10, 10), new Point (80, 60));
		Assert.IsTrue (command.Last is { IsEmpty: false });

		overlay.CompleteSelection (new Point (20, 20), new Point (20, 20));

		Assert.IsTrue (command.Last is { IsEmpty: true });
	}

	[UiTestMethod]
	public void CompleteSelection_ClampsToTheSelectionRatio ()
	{
		AvaloniaApp.Ensure ();
		var command = new RecordingCommand ();
		var overlay = ShowOverlay (new ImageSelectionOverlay {
			Width = 200,
			Height = 100,
			ImageWidth = 200,
			ImageHeight = 100,
			SelectionXyRatio = 1.0,
			SelectionCommand = command
		});

		overlay.CompleteSelection (new Point (10, 10), new Point (50, 30));

		Assert.IsNotNull (command.Last);
		Assert.AreEqual (command.Last.Value.Width, command.Last.Value.Height);
		Assert.AreEqual (40, command.Last.Value.Width);
	}

	[UiTestMethod]
	public void ChangingTheRatio_ReconstrainsTheCurrentSelection ()
	{
		AvaloniaApp.Ensure ();
		var command = new RecordingCommand ();
		var overlay = ShowOverlay (new ImageSelectionOverlay {
			Width = 200,
			Height = 100,
			ImageWidth = 200,
			ImageHeight = 100,
			SelectionCommand = command
		});
		overlay.CompleteSelection (new Point (10, 10), new Point (70, 40));

		overlay.SelectionXyRatio = 1.0;

		Assert.IsNotNull (command.Last);
		Assert.AreEqual (60, command.Last.Value.Width);
		Assert.AreEqual (60, command.Last.Value.Height);
	}

	[UiTestMethod]
	public void ChangingTheRatio_ClearsTheSelectionWhenTheConstraintCannotFit ()
	{
		AvaloniaApp.Ensure ();
		var command = new RecordingCommand ();
		var overlay = ShowOverlay (new ImageSelectionOverlay {
			Width = 200,
			Height = 100,
			ImageWidth = 200,
			ImageHeight = 100,
			SelectionCommand = command
		});
		overlay.CompleteSelection (new Point (10, 10), new Point (70, 40));
		Assert.IsFalse (command.Last is { IsEmpty: true });

		overlay.SelectionXyRatio = 10_000;

		Assert.IsTrue (command.Last is { IsEmpty: true });

		overlay.SelectionXyRatio = 1;
		Assert.IsTrue (command.Last is { IsEmpty: true });
	}

	static ImageSelectionOverlay ShowOverlay (ImageSelectionOverlay overlay)
	{
		var window = new Window {
			Width = overlay.Width,
			Height = overlay.Height,
			Content = overlay
		};
		window.Show ();
		return overlay;
	}

	sealed class RecordingCommand : ICommand
	{
		public FSpot.Imaging.PixelRect? Last { get; private set; }

		public bool CanExecute (object? parameter) => true;

		public void Execute (object? parameter)
			=> Last = (FSpot.Imaging.PixelRect)parameter!;

		public event EventHandler? CanExecuteChanged { add { } remove { } }
	}
}
