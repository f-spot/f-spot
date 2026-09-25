// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Input;
using Avalonia.Media.Imaging;

using FSpot.Avalonia.Controls;
using FSpot.Avalonia.Input;
using FSpot.Avalonia.Services;
using FSpot.Avalonia.ViewModels;
using FSpot.Core;
using FSpot.Utils;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class FullScreenTests
{
	[UiTestMethod]
	[DataRow (Key.F)]
	[DataRow (Key.Q)]
	[DataRow (Key.F11)]
	[DataRow (Key.Escape)]
	public void ExitKeys_MatchGtkFullScreenView (Key key)
		=> Assert.AreEqual (FullScreenCommand.Exit, FullScreenKeys.Map (key, KeyModifiers.None).Command);

	[UiTestMethod]
	public void I_TogglesInfo_AndBracketsRotate ()
	{
		Assert.AreEqual (FullScreenCommand.ToggleInfo, FullScreenKeys.Map (Key.I, KeyModifiers.None).Command);
		Assert.AreEqual (FullScreenCommand.ToggleInfo, FullScreenKeys.Map (Key.I, KeyModifiers.Shift).Command);
		Assert.AreEqual (FullScreenCommand.RotateLeft, FullScreenKeys.Map (Key.OemOpenBrackets, KeyModifiers.None).Command);
		Assert.AreEqual (FullScreenCommand.RotateRight, FullScreenKeys.Map (Key.OemCloseBrackets, KeyModifiers.None).Command);
	}

	[UiTestMethod]
	public void AltDigits_RateZeroToFive ()
	{
		Key[] digits = [Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5];
		for (uint i = 0; i < digits.Length; i++) {
			var action = FullScreenKeys.Map (digits[i], KeyModifiers.Alt);
			Assert.AreEqual (FullScreenCommand.Rate, action.Command);
			Assert.AreEqual (i, action.Rating);
		}
		Assert.AreEqual (3u, FullScreenKeys.Map (Key.NumPad3, KeyModifiers.Alt).Rating);
		Assert.AreEqual (FullScreenCommand.None, FullScreenKeys.Map (Key.D3, KeyModifiers.None).Command);
		Assert.AreEqual (FullScreenCommand.None, FullScreenKeys.Map (Key.D6, KeyModifiers.Alt).Command);
	}

	[UiTestMethod]
	public void Delete_RemovesAndShiftDeleteDeletes ()
	{
		Assert.AreEqual (FullScreenCommand.RemoveFromCatalog, FullScreenKeys.Map (Key.Delete, KeyModifiers.None).Command);
		Assert.AreEqual (FullScreenCommand.DeleteFromDrive, FullScreenKeys.Map (Key.Delete, KeyModifiers.Shift).Command);
	}

	[UiTestMethod]
	public void ArrowsNavigate_AndModifiedLettersPassThrough ()
	{
		Assert.AreEqual (FullScreenCommand.Next, FullScreenKeys.Map (Key.Right, KeyModifiers.None).Command);
		Assert.AreEqual (FullScreenCommand.Next, FullScreenKeys.Map (Key.Space, KeyModifiers.None).Command);
		Assert.AreEqual (FullScreenCommand.Previous, FullScreenKeys.Map (Key.Left, KeyModifiers.None).Command);
		Assert.AreEqual (FullScreenCommand.None, FullScreenKeys.Map (Key.Q, KeyModifiers.Meta).Command);
		Assert.AreEqual (FullScreenCommand.None, FullScreenKeys.Map (Key.F, KeyModifiers.Control).Command);
	}

	[UiTestMethod]
	public void OverlayTimings_MatchGtk ()
	{
		Assert.AreEqual (TimeSpan.FromSeconds (2), ControlOverlayPolicy.AutoHide);
		Assert.AreEqual (TimeSpan.FromSeconds (3), ControlOverlayPolicy.CursorHide);
		Assert.AreEqual (0.0, ControlOverlayPolicy.TargetOpacity (ControlOverlayVisibility.None));
		Assert.AreEqual (0.4, ControlOverlayPolicy.TargetOpacity (ControlOverlayVisibility.Partial));
		Assert.AreEqual (0.8, ControlOverlayPolicy.TargetOpacity (ControlOverlayVisibility.Full));
	}

	[UiTestMethod]
	public void Controls_RevealOnlyInTheBottomQuarter ()
	{
		Assert.IsFalse (ControlOverlayPolicy.RevealsControls (700, 1000));
		Assert.IsTrue (ControlOverlayPolicy.RevealsControls (751, 1000));
		Assert.IsFalse (ControlOverlayPolicy.RevealsControls (10, 0));
	}

	[UiTestMethod]
	public void Fade_StepsTowardTheTargetAndSettles ()
	{
		Assert.AreEqual (0.04, ControlOverlayPolicy.StepToward (0, 0.4), 1e-9);
		Assert.AreEqual (0.36, ControlOverlayPolicy.StepToward (0.4, 0), 1e-9);
		Assert.AreEqual (0.4, ControlOverlayPolicy.StepToward (0.38, 0.4));

		double opacity = 0;
		for (int i = 0; i < 100 && opacity != 0.8; i++)
			opacity = ControlOverlayPolicy.StepToward (opacity, 0.8);
		Assert.AreEqual (0.8, opacity);
	}

	[UiTestMethod]
	public void ToggleFullscreen_OpensTheFullScreenViewAtTheFocusedPhoto ()
	{
		var shell = new Shell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		viewModel.SeedCatalog (new StubPhoto ("/a.jpg", 1), new StubPhoto ("/b.jpg", 2), new StubPhoto ("/c.jpg", 3));
		viewModel.SelectPhoto (viewModel.Photos[1]);

		viewModel.ToggleFullscreenCommand.Execute (null);

		Assert.IsNotNull (shell.FullScreen);
		Assert.HasCount (3, shell.FullScreen.Value.Paths);
		Assert.AreEqual (1, shell.FullScreen.Value.Start);
		Assert.IsNull (shell.Slideshow);
	}

	[UiTestMethod]
	public void Slideshow_StartsAtTheFocusedPhoto ()
	{
		var shell = new Shell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		viewModel.SeedCatalog (new StubPhoto ("/a.jpg", 1), new StubPhoto ("/b.jpg", 2), new StubPhoto ("/c.jpg", 3));
		viewModel.SelectPhoto (viewModel.Photos[2]);

		viewModel.SlideshowCommand.Execute (null);

		Assert.IsNotNull (shell.Slideshow);
		Assert.AreEqual (2, shell.Slideshow.Value.Start);
		Assert.Contains ("c.jpg", shell.Slideshow.Value.Paths[2]);
	}

	[UiTestMethod]
	public void EmptyCollection_WarnsInsteadOfOpening ()
	{
		var shell = new Shell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());

		viewModel.ToggleFullscreenCommand.Execute (null);

		Assert.IsNull (shell.FullScreen);
		Assert.IsTrue (shell.Warned);
	}

	[UiTestMethod]
	public void Host_FocusPhoto_SelectsThePhotoTheViewWasOn ()
	{
		var shell = new Shell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		viewModel.SeedCatalog (new StubPhoto ("/a.jpg", 1), new StubPhoto ("/b.jpg", 2));
		viewModel.SelectPhoto (viewModel.Photos[0]);
		viewModel.ToggleFullscreenCommand.Execute (null);
		string path = shell.FullScreen!.Value.Paths[1];

		IFullScreenHost host = viewModel;
		host.FocusPhoto (path);

		Assert.AreSame (viewModel.Photos[1], viewModel.SelectedPhoto);
		Assert.IsFalse (viewModel.Photos[0].Selected);
		Assert.AreSame (viewModel.Photos[1].Photo, host.PhotoAt (path));
		Assert.IsNull (host.PhotoAt ("/nowhere.jpg"));
	}

	sealed class Shell : IAvaloniaShell
	{
		public (IReadOnlyList<string> Paths, int Start)? FullScreen { get; private set; }
		public (IReadOnlyList<string> Paths, int Start)? Slideshow { get; private set; }
		public bool Warned { get; private set; }

		public void Close () { }
		public void ToggleFullscreen () { }
		public void FocusFind () { }
		public Task ShowAboutAsync () => Task.CompletedTask;
		public Task ShowWarningAsync (string title, string message)
		{
			Warned = true;
			return Task.CompletedTask;
		}
		public Task<bool> ShowImportAsync () => Task.FromResult (false);
		public Task<bool> ShowImportAsync (IReadOnlyList<string> paths) => Task.FromResult (false);
		public Task<string?> PickFolderAsync (string title) => Task.FromResult<string?> (null);
		public Task<string?> PromptAsync (string title, string prompt, string defaultValue) => Task.FromResult<string?> (null);
		public Task<bool> ConfirmAsync (string title, string message, string okCaption) => Task.FromResult (false);
		public Task<(uint Min, uint Max)?> PromptRatingRangeAsync () => Task.FromResult<(uint, uint)?> (null);
		public Task<(DateTime Start, DateTime End)?> PromptDateRangeAsync () => Task.FromResult<(DateTime, DateTime)?> (null);
		public Task<uint?> PickImportRollAsync (IReadOnlyList<(uint Id, string Label)> rolls) => Task.FromResult<uint?> (null);
		public Task PrintPhotosAsync (IReadOnlyList<string> paths, PrintPageSetup? setup = null) => Task.CompletedTask;
		public Task MailPhotosAsync (IReadOnlyList<string> paths) => Task.CompletedTask;
		public void ShowSlideshow (IReadOnlyList<string> paths, int startIndex) => Slideshow = (paths, startIndex);
		public void ShowFullScreen (IReadOnlyList<string> paths, int startIndex) => FullScreen = (paths, startIndex);
	}

	sealed class NullThumbnails : IPhotoThumbnailLoader
	{
		public Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken)
			=> Task.FromResult<Bitmap?> (null);
	}

	sealed class StubVersion : IPhotoVersion
	{
		public StubVersion (string path)
		{
			Uri = new SafeUri ("file://" + path, true);
			BaseUri = Uri;
			Filename = System.IO.Path.GetFileName (path);
		}

		public string Name => "Original";
		public bool IsProtected => true;
		public SafeUri BaseUri { get; }
		public string Filename { get; }
		public string ImportMD5 => string.Empty;
		public SafeUri Uri { get; set; }
	}

	sealed class StubPhoto : IPhoto
	{
		public StubPhoto (string path, int day)
		{
			Name = System.IO.Path.GetFileName (path);
			DefaultVersion = new StubVersion (path);
			Versions = new[] { DefaultVersion };
			Time = new DateTime (2026, 1, day);
		}

		public DateTime Time { get; }
		public Tag[] Tags { get; } = [];
		public string Description => string.Empty;
		public string Name { get; }
		public uint Rating => 0;
		public IPhotoVersion DefaultVersion { get; }
		public IEnumerable<IPhotoVersion> Versions { get; }
	}
}
