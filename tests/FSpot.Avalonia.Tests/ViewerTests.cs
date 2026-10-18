// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;
using System.Reflection;

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

using FSpot.Avalonia.Services;
using FSpot.Avalonia.ViewModels;
using FSpot.Resources.Lang;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class ViewerSettingsTests
{
	[UiTestMethod]
	[DataRow ("NONE", ViewerTransparency.Background)]
	[DataRow ("CHECK_PATTERN", ViewerTransparency.CheckPattern)]
	[DataRow ("COLOR", ViewerTransparency.CustomColor)]
	[DataRow ("bogus", ViewerTransparency.Background)]
	[DataRow (null, ViewerTransparency.Background)]
	public void ParseTransparency_FollowsTheGtkValues (string? value, ViewerTransparency expected)
		=> Assert.AreEqual (expected, ViewerSettings.ParseTransparency (value));

	[UiTestMethod]
	public void FormatTransparency_RoundTrips ()
	{
		foreach (var mode in Enum.GetValues<ViewerTransparency> ())
			Assert.AreEqual (mode, ViewerSettings.ParseTransparency (ViewerSettings.FormatTransparency (mode)));
		Assert.AreEqual ("NONE", ViewerSettings.FormatTransparency (ViewerTransparency.Background));
	}

	[UiTestMethod]
	public void Color_ParsesAndFormatsGtkHex ()
	{
		Assert.IsTrue (ViewerSettings.TryParseColor ("#ff8001", out byte r, out byte g, out byte b));
		Assert.AreEqual (0xff, r);
		Assert.AreEqual (0x80, g);
		Assert.AreEqual (0x01, b);
		Assert.AreEqual ("#ff8001", ViewerSettings.FormatColor (r, g, b));
	}

	[UiTestMethod]
	[DataRow ("")]
	[DataRow ("ff8001")]
	[DataRow ("#ff80")]
	[DataRow ("#gg0000")]
	[DataRow (null)]
	public void NormalizeColor_FallsBackToBlack (string? value)
		=> Assert.AreEqual ("#000000", ViewerSettings.NormalizeColor (value));
}

[TestClass]
public sealed class ViewerZoomTests
{
	[UiTestMethod]
	public void MinFor_FitsWithoutUpscaling ()
	{
		Assert.AreEqual (0.5, ViewerZoom.MinFor (2000, 1000, 1000, 1000), 1e-9);
		Assert.AreEqual (1.0, ViewerZoom.MinFor (100, 100, 1000, 1000), 1e-9);
		Assert.AreEqual (ViewerZoom.FallbackMin, ViewerZoom.MinFor (0, 100, 1000, 1000), 1e-9);
	}

	[UiTestMethod]
	public void Normalized_IsLinearBetweenMinAndMax ()
	{
		Assert.AreEqual (0, ViewerZoom.Normalize (0.5, 0.5), 1e-9);
		Assert.AreEqual (1, ViewerZoom.Normalize (ViewerZoom.Max, 0.5), 1e-9);
		double half = ViewerZoom.FromNormalized (0.5, 0.5);
		Assert.AreEqual ((ViewerZoom.Max + 0.5) / 2, half, 1e-9);
		Assert.AreEqual (0.5, ViewerZoom.Normalize (half, 0.5), 1e-9);
	}

	[UiTestMethod]
	public void Clamp_KeepsZoomInRange ()
	{
		Assert.AreEqual (0.2, ViewerZoom.Clamp (0.01, 0.2), 1e-9);
		Assert.AreEqual (ViewerZoom.Max, ViewerZoom.Clamp (50, 0.2), 1e-9);
	}
}

[TestClass]
[DoNotParallelize]
public sealed class ViewerViewModelTests
{
	string folder = string.Empty;

	[TestInitialize]
	public void CreateFolder ()
	{
		folder = Path.Combine (Path.GetTempPath (), "fspot-viewer-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (folder);
		foreach (string name in new[] { "b.jpg", "a.jpg", "notes.txt", "c.JPG" })
			File.WriteAllText (Path.Combine (folder, name), string.Empty);
	}

	[TestCleanup]
	public void DeleteFolder ()
	{
		try {
			Directory.Delete (folder, true);
		} catch (IOException) {
		}
	}

	static bool IsJpeg (string path)
		=> Path.GetExtension (path).Equals (".jpg", StringComparison.OrdinalIgnoreCase);

	string P (string name) => Path.Combine (folder, name);

	[UiTestMethod]
	public void ExpandPaths_ListsLoadableFilesOfAFolderInNameOrder ()
	{
		var paths = ViewerViewModel.ExpandPaths (new[] { folder }, IsJpeg);

		Assert.AreSequenceEqual (new[] { P ("a.jpg"), P ("b.jpg"), P ("c.JPG") }, paths.ToArray ());
	}

	[UiTestMethod]
	public void ExpandPaths_SkipsMissingAndUnloadableFiles ()
	{
		var uri = new Uri (P ("b.jpg")).AbsoluteUri;
		var paths = ViewerViewModel.ExpandPaths (new[] { P ("notes.txt"), P ("missing.jpg"), uri, "http://example.com/x.jpg" }, IsJpeg);

		Assert.AreSequenceEqual (new[] { P ("b.jpg") }, paths.ToArray ());
	}

	[UiTestMethod]
	public void Initialize_OneFileKeepsTheSidePaneClosed ()
	{
		var viewModel = new ViewerViewModel (new RecordingViewerShell (), IsJpeg);
		viewModel.Initialize (new[] { P ("a.jpg") });

		Assert.IsFalse (viewModel.ShowSidebar);
		Assert.AreEqual (0, viewModel.SelectedIndex);
		Assert.AreEqual (P ("a.jpg"), viewModel.CurrentItem?.Path);
	}

	[UiTestMethod]
	public void Initialize_AFolderOpensTheSidePane ()
	{
		var viewModel = new ViewerViewModel (new RecordingViewerShell (), IsJpeg);
		viewModel.Initialize (new[] { folder });

		Assert.HasCount (3, viewModel.Items);
		Assert.IsTrue (viewModel.ShowSidebar);
		Assert.IsTrue (viewModel.SidebarVisible);
	}

	[UiTestMethod]
	public void LoadPaths_OpensTheSidePaneOnceThereIsMoreThanOnePhoto ()
	{
		var viewModel = new ViewerViewModel (new RecordingViewerShell (), IsJpeg);
		viewModel.Initialize (new[] { P ("a.jpg") });
		viewModel.LoadPaths (new[] { P ("b.jpg") });

		Assert.IsTrue (viewModel.ShowSidebar);
		Assert.AreEqual (0, viewModel.SelectedIndex);
	}

	[UiTestMethod]
	public void StatusText_MatchesTheGtkStatusLabel ()
	{
		var viewModel = new ViewerViewModel (new RecordingViewerShell (), IsJpeg);
		viewModel.Initialize (new[] { folder });
		viewModel.DisplayFileNames = true;

		Assert.AreEqual ("a.jpg  -  " + string.Format (Strings.XPhotos, 3), viewModel.StatusText);

		viewModel.SelectedIndex = 1;
		Assert.StartsWith ("b.jpg  -  ", viewModel.StatusText);

		viewModel.ToggleFileNamesCommand.Execute (null);
		Assert.AreEqual (string.Format (Strings.XPhotos, 3), viewModel.StatusText);
	}

	[UiTestMethod]
	public void FormatStatus_UsesTheSingularForOneOrNone ()
	{
		Assert.AreEqual (string.Format (Strings.XPhoto, 0), ViewerViewModel.FormatStatus (null, 0));
		Assert.AreEqual (string.Format (Strings.XPhoto, 1), ViewerViewModel.FormatStatus (null, 1));
		Assert.AreEqual (string.Format (Strings.XPhotos, 2), ViewerViewModel.FormatStatus (null, 2));
	}

	[UiTestMethod]
	public async Task OpenFolder_ReplacesThePhotos ()
	{
		var shell = new RecordingViewerShell { Folder = folder };
		var viewModel = new ViewerViewModel (shell, IsJpeg);
		viewModel.Initialize (new[] { P ("a.jpg") });

		await viewModel.OpenFolderCommand.ExecuteAsync (null);

		Assert.AreEqual (Strings.SelectFolder, shell.FolderTitle);
		Assert.HasCount (3, viewModel.Items);
		Assert.AreEqual (folder, viewModel.CurrentUri);
		Assert.AreEqual (0, viewModel.SelectedIndex);
	}

	[UiTestMethod]
	public async Task Open_ReplacesThePhotosWithTheChosenFile ()
	{
		var shell = new RecordingViewerShell ();
		shell.Files.Add (P ("c.JPG"));
		var viewModel = new ViewerViewModel (shell, IsJpeg);
		viewModel.Initialize (new[] { folder });

		await viewModel.OpenCommand.ExecuteAsync (null);

		Assert.AreEqual (Strings.Open, shell.FilesTitle);
		Assert.HasCount (1, viewModel.Items);
		Assert.AreEqual (P ("c.JPG"), viewModel.CurrentItem?.Path);
	}

	[UiTestMethod]
	public void NewWindow_OpensTheFirstUri ()
	{
		var shell = new RecordingViewerShell ();
		var viewModel = new ViewerViewModel (shell, IsJpeg);
		viewModel.Initialize (new[] { P ("b.jpg"), P ("a.jpg") });

		viewModel.NewWindowCommand.Execute (null);

		Assert.IsNotNull (shell.NewWindow);
		Assert.AreSequenceEqual (new[] { P ("b.jpg") }, shell.NewWindow.ToArray ());
	}

	[UiTestMethod]
	public void Slideshow_StartsAtTheSelectedPhoto ()
	{
		var shell = new RecordingViewerShell ();
		var viewModel = new ViewerViewModel (shell, IsJpeg);
		viewModel.Initialize (new[] { folder });
		viewModel.SelectedIndex = 2;

		viewModel.SlideshowCommand.Execute (null);

		Assert.AreEqual (2, shell.SlideshowStart);
		Assert.HasCount (3, shell.SlideshowPaths!);
	}

	[UiTestMethod]
	public void PhotoCommands_NeedAPhoto ()
	{
		var viewModel = new ViewerViewModel (new RecordingViewerShell (), IsJpeg);
		viewModel.Initialize (Array.Empty<string> ());

		Assert.IsFalse (viewModel.RotateLeftCommand.CanExecute (null));
		Assert.IsFalse (viewModel.RotateRightCommand.CanExecute (null));
		Assert.IsFalse (viewModel.SetAsBackgroundCommand.CanExecute (null));

		viewModel.LoadPaths (new[] { P ("a.jpg") });

		Assert.IsTrue (viewModel.RotateLeftCommand.CanExecute (null));
		Assert.IsTrue (viewModel.RotateRightCommand.CanExecute (null));
	}

	[UiTestMethod]
	public async Task SetAsBackground_UsesTheCurrentPhoto ()
	{
		var shell = new RecordingViewerShell ();
		var viewModel = new ViewerViewModel (shell, IsJpeg);
		viewModel.Initialize (new[] { folder });
		viewModel.SelectedIndex = 1;

		await viewModel.SetAsBackgroundCommand.ExecuteAsync (null);

		Assert.AreEqual (P ("b.jpg"), shell.Background);
	}

	[UiTestMethod]
	public void Toggles_FlipToolbarAndSidePane ()
	{
		var viewModel = new ViewerViewModel (new RecordingViewerShell (), IsJpeg);
		viewModel.Initialize (new[] { folder });
		viewModel.ShowToolbar = true;

		viewModel.ToggleToolbarCommand.Execute (null);
		viewModel.ToggleSidebarCommand.Execute (null);

		Assert.IsFalse (viewModel.ShowToolbar);
		Assert.IsFalse (viewModel.ShowSidebar);

		viewModel.ToggleSidebarCommand.Execute (null);
		viewModel.IsFullScreen = true;
		Assert.IsTrue (viewModel.ShowSidebar);
		Assert.IsFalse (viewModel.SidebarVisible);

		viewModel.HideSidebarCommand.Execute (null);
		Assert.IsFalse (viewModel.ShowSidebar);
	}

	[UiTestMethod]
	public void Zoom_StepsByTheGtkFactorWithinRange ()
	{
		var viewModel = new ViewerViewModel (new RecordingViewerShell (), IsJpeg);
		viewModel.ResetZoom (0.5);

		viewModel.ZoomOutCommand.Execute (null);
		Assert.AreEqual (0.5, viewModel.Zoom, 1e-9);
		Assert.AreEqual (0, viewModel.NormalizedZoom, 1e-9);

		viewModel.ZoomInCommand.Execute (null);
		Assert.AreEqual (0.5 * ViewerZoom.Factor, viewModel.Zoom, 1e-9);

		viewModel.NormalizedZoom = 1;
		Assert.AreEqual (ViewerZoom.Max, viewModel.Zoom, 1e-9);
		viewModel.ZoomInCommand.Execute (null);
		Assert.AreEqual (ViewerZoom.Max, viewModel.Zoom, 1e-9);
	}

	[UiTestMethod]
	public void UpdateMinZoom_KeepsAFittedPhotoFitted ()
	{
		var viewModel = new ViewerViewModel (new RecordingViewerShell (), IsJpeg);
		viewModel.ResetZoom (0.5);
		viewModel.UpdateMinZoom (0.25);
		Assert.AreEqual (0.25, viewModel.Zoom, 1e-9);

		viewModel.Zoom = 2;
		viewModel.UpdateMinZoom (0.4);
		Assert.AreEqual (2, viewModel.Zoom, 1e-9);
	}

	[UiTestMethod]
	public void SavePreferences_KeepsTheSizeOnlyWhenNotMaximized ()
	{
		var size = ViewerSettings.Size;
		bool maximized = ViewerSettings.Maximized;
		bool toolbar = ViewerSettings.ShowToolbar;
		bool names = ViewerSettings.ShowFilenames;
		try {
			var viewModel = new ViewerViewModel (new RecordingViewerShell (), IsJpeg) {
				ShowToolbar = false,
				DisplayFileNames = false
			};
			viewModel.SavePreferences (800, 600, false);
			Assert.AreEqual ((800, 600), ViewerSettings.Size);

			viewModel.SavePreferences (1920, 1080, true);
			Assert.AreEqual ((800, 600), ViewerSettings.Size);
			Assert.IsTrue (ViewerSettings.Maximized);
			Assert.IsFalse (ViewerSettings.ShowToolbar);
			Assert.IsFalse (ViewerSettings.ShowFilenames);
		} finally {
			ViewerSettings.Size = size;
			ViewerSettings.Maximized = maximized;
			ViewerSettings.ShowToolbar = toolbar;
			ViewerSettings.ShowFilenames = names;
		}
	}
}

[TestClass]
public sealed class ViewerLaunchTests
{
	[UiTestMethod]
	public void Paths_ReturnsTheFilesAfterView ()
	{
		var paths = ViewerLaunch.Paths (new[] { "--view", "/tmp/a.jpg", "/tmp/b.jpg" });

		Assert.IsNotNull (paths);
		Assert.AreSequenceEqual (new[] { "/tmp/a.jpg", "/tmp/b.jpg" }, paths.ToArray ());
	}

	[UiTestMethod]
	public void Paths_IsNullWithoutViewOrWithoutFiles ()
	{
		Assert.IsNull (ViewerLaunch.Paths (Array.Empty<string> ()));
		Assert.IsNull (ViewerLaunch.Paths (new[] { "/tmp/a.jpg" }));
		Assert.IsNull (ViewerLaunch.Paths (new[] { "-v" }));
	}
}

[TestClass]
[DoNotParallelize]
public sealed class ViewerChromeTests
{
	[UiTestMethod]
	public void ViewerStrings_AllResolveFromTheResources ()
	{
		var properties = typeof (ViewerStrings).GetProperties (BindingFlags.Public | BindingFlags.Static);
		Assert.IsGreaterThan (10, properties.Length);
		foreach (var property in properties) {
			string value = (string)property.GetValue (null)!;
			Assert.IsFalse (string.IsNullOrWhiteSpace (value), property.Name);
			Assert.AreNotEqual (property.Name, value, property.Name);
		}
		Assert.AreEqual ("F-Spot View", ViewerStrings.FSpotView);
	}

	[UiTestMethod]
	public void PlainText_StripsPangoMarkup ()
	{
		Assert.AreEqual ("Image Interpolation", ViewerPreferencesWindow.PlainText (ViewerStrings.BImageInterpolationB));
		Assert.StartsWith ("You can choose how to display transparent parts", ViewerPreferencesWindow.PlainText (ViewerStrings.TransparencyDetail));
		Assert.AreEqual (string.Empty, ViewerPreferencesWindow.PlainText (null));
	}

	[UiTestMethod]
	public void AutomationIds_AreUniqueAndDistinctFromTheMainCatalog ()
	{
		static IEnumerable<string> Values (Type type)
			=> type.GetFields (BindingFlags.Public | BindingFlags.Static)
				.Where (field => field.IsLiteral && field.FieldType == typeof (string))
				.Select (field => (string)field.GetRawConstantValue ()!);

		var viewer = Values (typeof (ViewerAutomationIds)).ToList ();
		Assert.HasCount (viewer.Distinct (StringComparer.Ordinal).Count (), viewer);
		Assert.IsEmpty (viewer.Intersect (Values (typeof (AutomationIds)), StringComparer.Ordinal));
	}

	[UiTestMethod]
	public void TransparencyBrush_FollowsTheMode ()
	{
		AvaloniaApp.Ensure ();
		Assert.IsNull (ViewerWindow.TransparencyBrush (ViewerTransparency.Background, "#ffffff"));
		Assert.IsInstanceOfType<DrawingBrush> (ViewerWindow.TransparencyBrush (ViewerTransparency.CheckPattern, "#ffffff"));
		var solid = ViewerWindow.TransparencyBrush (ViewerTransparency.CustomColor, "#102030") as SolidColorBrush;
		Assert.IsNotNull (solid);
		Assert.AreEqual (Color.FromRgb (0x10, 0x20, 0x30), solid.Color);
	}
}

[TestClass]
[DoNotParallelize]
public sealed class ViewerWindowTests
{
	[UiTestMethod]
	public void Window_ShowsTheMenuToolbarAndPhoto ()
	{
		AvaloniaApp.Ensure ();
		string sample = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
		var window = new ViewerWindow (new[] { sample }, _ => true);
		window.Show ();
		try {
			Assert.AreEqual (ViewerStrings.FSpotView, window.Title);
			Assert.HasCount (1, window.ViewModel.Items);
			Assert.AreEqual (sample, window.ViewModel.CurrentItem?.Path);
			Assert.IsFalse (window.ViewModel.SidebarVisible);
			Assert.AreEqual (0, window.BodyGrid.ColumnDefinitions[0].Width.Value);
		} finally {
			window.Close ();
		}
	}

	[UiTestMethod]
	public void FolderTray_DecodesTheVisibleWindowNotTheFolder ()
	{
		var window = ShowTray (out var thumbs, out var directory);
		try {
			Dispatcher.UIThread.RunJobs ();
			Assert.IsGreaterThan (0, thumbs.Calls);
			Assert.IsLessThan (40, thumbs.Calls);
		} finally {
			window.Close ();
			directory.Delete (recursive: true);
		}
	}

	[UiTestMethod]
	public void FolderTray_CancelsAThumbnailThatScrollsAway ()
	{
		var window = ShowTray (out var thumbs, out var directory);
		try {
			Dispatcher.UIThread.RunJobs ();
			int shown = thumbs.Calls;
			Assert.IsGreaterThan (0, shown);
			var first = new CancellationToken[shown];
			for (int i = 0; i < shown; i++)
				first[i] = thumbs.Loads[i].Token;

			window.ViewModel.SelectedIndex = window.ViewModel.Items.Count - 1;
			Dispatcher.UIThread.RunJobs ();

			int cancelled = 0;
			foreach (var token in first)
				if (token.IsCancellationRequested)
					cancelled++;
			int stillRunning = 0;
			foreach (var load in thumbs.Loads)
				if (!load.Token.IsCancellationRequested)
					stillRunning++;
			Assert.IsGreaterThan (0, cancelled);
			Assert.IsGreaterThan (0, stillRunning);
		} finally {
			window.Close ();
			directory.Delete (recursive: true);
		}
	}

	[UiTestMethod]
	public void Viewer_DoesNotDecodeAPhotoSkippedWhileItWasWaiting ()
	{
		AvaloniaApp.Ensure ();
		int started = 0;
		string? seen = null;
		string root = Path.Combine (Path.GetTempPath (), "fspot-viewer-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (root);
		string first = Path.Combine (root, "a.jpg");
		string second = Path.Combine (root, "b.jpg");
		File.WriteAllBytes (first, Array.Empty<byte> ());
		File.WriteAllBytes (second, Array.Empty<byte> ());
		ViewerWindow window;
		using (FSpot.Avalonia.Imaging.DecodeGate.Enter ()) {
			window = new ViewerWindow (new[] { first, second }, _ => true, new NullThumbnails (), (path, _) => {
				Interlocked.Increment (ref started);
				seen = path;
				return null;
			});
			window.ViewModel.SelectedIndex = 1;
		}

		try {
			var until = DateTime.UtcNow.AddSeconds (2);
			while (DateTime.UtcNow < until) {
				Dispatcher.UIThread.RunJobs ();
				Thread.Sleep (20);
			}

			Assert.AreEqual (1, started);
			Assert.AreEqual (second, seen);
		} finally {
			window.Close ();
			Directory.Delete (root, true);
		}
	}

	[UiTestMethod]
	public void ViewerTray_RetriesAThumbnailAfterTheLoadFails ()
	{
		AvaloniaApp.Ensure ();
		string root = Path.Combine (Path.GetTempPath (), "fspot-tray-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (root);
		string photo = Path.Combine (root, "a.jpg");
		File.WriteAllBytes (photo, Array.Empty<byte> ());
		var thumbs = new TrayFailThenBitmap ();
		var window = new ViewerWindow (new[] { photo }, _ => true, thumbs);
		var border = new Border ();
		try {
			window.OnTrayPrepared (null, new ContainerPreparedEventArgs (border, 0));
			Assert.AreEqual (1, thumbs.Calls);
			Assert.IsNull (window.ViewModel.Items[0].Thumbnail);

			window.OnTrayPrepared (null, new ContainerPreparedEventArgs (border, 0));
			Assert.AreEqual (2, thumbs.Calls);
			Assert.IsNotNull (window.ViewModel.Items[0].Thumbnail);
		} finally {
			window.Close ();
			Directory.Delete (root, true);
		}
	}

	sealed class TrayFailThenBitmap : IPhotoThumbnailLoader
	{
		public int Calls { get; private set; }

		public Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken)
		{
			Calls++;
			if (Calls == 1)
				throw new InvalidOperationException ("tray broke");
			var bitmap = new WriteableBitmap (new global::Avalonia.PixelSize (1, 1), new global::Avalonia.Vector (96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
			return Task.FromResult<Bitmap?> (bitmap);
		}
	}

	static ViewerWindow ShowTray (out MainWindowViewModelTests.TokenThumbnails thumbs, out DirectoryInfo directory)
	{
		AvaloniaApp.Ensure ();
		thumbs = new MainWindowViewModelTests.TokenThumbnails ();
		string path = Path.Combine (Path.GetTempPath (), "fspot-tray-" + Guid.NewGuid ().ToString ("N"));
		directory = Directory.CreateDirectory (path);
		var files = new string[400];
		for (int i = 0; i < files.Length; i++) {
			files[i] = Path.Combine (path, i.ToString ("000") + ".jpg");
			File.WriteAllBytes (files[i], Array.Empty<byte> ());
		}

		var window = new ViewerWindow (files, _ => true, thumbs);
		window.WindowState = WindowState.Normal;
		window.Width = 720;
		window.Height = 480;
		window.FolderTray.MaxHeight = 220;
		window.Show ();
		return window;
	}

	[UiTestMethod]
	public void PreferencesWindow_WritesTheViewerSettings ()
	{
		AvaloniaApp.Ensure ();
		bool interpolation = ViewerSettings.Interpolation;
		var transparency = ViewerSettings.Transparency;
		int changes = 0;
		EventHandler counted = (_, _) => changes++;
		ViewerSettings.Changed += counted;
		try {
			ViewerSettings.Transparency = ViewerTransparency.Background;
			var window = ViewerPreferencesWindow.ShowUp (null);
			try {
				Assert.AreSame (window, ViewerPreferencesWindow.ShowUp (null));
				Assert.IsTrue (window.AsBackground.IsChecked);

				window.AsCheckPattern.IsChecked = true;
				Assert.AreEqual (ViewerTransparency.CheckPattern, ViewerSettings.Transparency);

				window.Interpolation.IsChecked = !interpolation;
				Assert.AreEqual (!interpolation, ViewerSettings.Interpolation);
				Assert.IsGreaterThanOrEqualTo (3, changes);
			} finally {
				window.Close ();
			}
		} finally {
			ViewerSettings.Changed -= counted;
			ViewerSettings.Interpolation = interpolation;
			ViewerSettings.Transparency = transparency;
		}
	}
}

sealed class RecordingViewerShell : IViewerShell
{
	public List<string> Files { get; } = new ();
	public string? Folder { get; set; }
	public string? FilesTitle { get; private set; }
	public string? FolderTitle { get; private set; }
	public IReadOnlyList<string>? NewWindow { get; private set; }
	public IReadOnlyList<string>? SlideshowPaths { get; private set; }
	public int SlideshowStart { get; private set; } = -1;
	public string? Background { get; private set; }

	public void Close () { }

	public void OpenNewWindow (IReadOnlyList<string> paths) => NewWindow = paths;

	public Task<IReadOnlyList<string>> PickFilesAsync (string title, bool allowMultiple)
	{
		FilesTitle = title;
		return Task.FromResult<IReadOnlyList<string>> (Files);
	}

	public Task<string?> PickFolderAsync (string title)
	{
		FolderTitle = title;
		return Task.FromResult (Folder);
	}

	public void ToggleFullscreen () { }

	public void ShowSlideshow (IReadOnlyList<string> paths, int startIndex)
	{
		SlideshowPaths = paths;
		SlideshowStart = startIndex;
	}

	public Task ShowPreferencesAsync () => Task.CompletedTask;
	public Task ShowAboutAsync () => Task.CompletedTask;
	public Task ShowWarningAsync (string title, string message) => Task.CompletedTask;

	public Task SetDesktopBackgroundAsync (string path)
	{
		Background = path;
		return Task.CompletedTask;
	}
}
