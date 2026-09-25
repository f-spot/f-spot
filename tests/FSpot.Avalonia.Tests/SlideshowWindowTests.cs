// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Input;

using FSpot.Avalonia.Services;
using FSpot.Core;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class SlideshowWindowTests
{
	[TestMethod]
	public void FullScreenView_OpensPaused_AndKeysDriveTheHost ()
	{
		AvaloniaApp.Ensure ();
		var host = new RecordingHost ();
		var window = new SlideshowWindow (new[] { "/missing/a.jpg", "/missing/b.jpg" }, 0, host, play: false);
		window.Show ();

		Assert.IsFalse (window.IsPlaying);
		Press (window, Key.Right);
		Assert.AreEqual ("/missing/b.jpg", window.CurrentPath);

		Press (window, Key.D4, KeyModifiers.Alt);
		Assert.AreEqual (("/missing/b.jpg", 4u), host.Rated);

		Press (window, Key.OemCloseBrackets);
		Assert.AreEqual (("/missing/b.jpg", true), host.Rotated);

		var info = window.FindControl<Border> ("InfoOverlay")!;
		Press (window, Key.I);
		Assert.IsTrue (info.IsVisible);

		Press (window, Key.Escape);
		Assert.IsFalse (window.IsVisible);
		Assert.AreEqual ("/missing/b.jpg", host.Focused);
	}

	[TestMethod]
	public void Slideshow_OpensPlaying_AndPlayPauseToggles ()
	{
		AvaloniaApp.Ensure ();
		var window = new SlideshowWindow (new[] { "/missing/a.jpg" }, 0, null, play: true);
		window.Show ();

		Assert.IsTrue (window.IsPlaying);
		window.PlayPause ();
		Assert.IsFalse (window.IsPlaying);
		window.Close ();
	}

	static void Press (Window window, Key key, KeyModifiers modifiers = KeyModifiers.None)
		=> window.RaiseEvent (new KeyEventArgs {
			RoutedEvent = InputElement.KeyDownEvent,
			Key = key,
			KeyModifiers = modifiers,
			Source = window,
		});

	sealed class RecordingHost : IFullScreenHost
	{
		public (string, uint)? Rated { get; private set; }
		public (string, bool)? Rotated { get; private set; }
		public string? Focused { get; private set; }

		public IPhoto? PhotoAt (string path) => null;
		public void FocusPhoto (string path) => Focused = path;
		public void RatePhoto (string path, uint rating) => Rated = (path, rating);
		public void RotatePhoto (string path, bool clockwise) => Rotated = (path, clockwise);
		public Task<bool> RemovePhotoAsync (string path, bool deleteFromDrive) => Task.FromResult (false);
	}
}
