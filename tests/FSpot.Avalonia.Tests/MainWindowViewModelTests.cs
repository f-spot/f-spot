// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;

using FSpot.Avalonia.Imaging;

using FSpot.Avalonia.Dialogs;
using FSpot.Avalonia.Input;

using FSpot;
using FSpot.Avalonia.Models;
using FSpot.Avalonia.Services;
using FSpot.Avalonia.ViewModels;
using FSpot.Core;
using FSpot.Database;
using FSpot.Editors;
using FSpot.Import;
using FSpot.Query;
using FSpot.Resources.Lang;
using FSpot.Utils;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed partial class MainWindowViewModelTests
{
	public TestContext TestContext { get; set; } = null!;

	[UiTestMethod]
	public void TileSize_FollowsZoom ()
	{
		var viewModel = NewViewModel ();
		viewModel.Zoom = 96;
		Assert.AreEqual (96, viewModel.TileSize);
		Assert.AreEqual (96 + 24, viewModel.FilmstripPaneHeight);
		viewModel.Zoom = 160;
		Assert.AreEqual (160, viewModel.TileSize);
	}

	[UiTestMethod]
	public async Task CopyCommand_CopiesSelectedPhotoPaths ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		var item = new PhotoItemViewModel (new StubPhoto ("/photos/a.jpg"));
		viewModel.Photos.Add (item);
		item.Selected = true;

		Assert.IsTrue (viewModel.CopyCommand.CanExecute (null));
		await viewModel.CopyCommand.ExecuteAsync (null);
		Assert.IsNotNull (shell.Copied);
		Assert.HasCount (1, shell.Copied);
		Assert.Contains ("a.jpg", shell.Copied[0]);
	}

	[UiTestMethod]
	public async Task SetAsBackgroundCommand_UsesTheSelectedPhoto ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		var item = new PhotoItemViewModel (new StubPhoto ("/photos/wall.jpg"));
		viewModel.Photos.Add (item);
		item.Selected = true;

		Assert.IsTrue (viewModel.SetAsBackgroundCommand.CanExecute (null));
		await viewModel.SetAsBackgroundCommand.ExecuteAsync (null);
		string? background = shell.BackgroundPath;
		Assert.IsNotNull (background);
		Assert.Contains ("wall.jpg", background);
	}

	[UiTestMethod]
	public async Task PageSetupCommand_StoresSetupThatPrintUses ()
	{
		var shell = new RecordingShell {
			PageSetupResult = new PrintPageSetup { PaperWidthMm = 210, PaperHeightMm = 297, Landscape = true }
		};
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		var item = new PhotoItemViewModel (new StubPhoto ("/photos/a.jpg"));
		viewModel.Photos.Add (item);
		item.Selected = true;

		await viewModel.PageSetupCommand.ExecuteAsync (null);
		Assert.AreEqual (210, viewModel.PageSetup.PaperWidthMm);
		Assert.IsTrue (viewModel.PageSetup.Landscape);
		await viewModel.PrintCommand.ExecuteAsync (null);
		Assert.IsTrue (shell.PrintedWithSetup);
	}

	[UiTestMethod]
	public void ShowToolsCommand_OpensExtensions ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		viewModel.ShowToolsCommand.Execute (null);
		Assert.IsTrue (shell.ExtensionsShown);
		Assert.IsNotNull (shell.ShownAddins);
		Assert.IsGreaterThan (0, shell.ShownAddins.Count);
	}

	[UiTestMethod]
	public async Task LoadThumbnails_DecodesAtDipTimesUiScale ()
	{
		var thumbs = new RecordingThumbnails ();
		var viewModel = new MainWindowViewModel (null, new RecordingShell (), thumbs) {
			Zoom = 100,
			UiScale = 2
		};
		viewModel.SeedCatalog (new StubPhoto ("/a.jpg"));
		await viewModel.RealizeThumbnailAsync (new object (), viewModel.Photos[0]);
		Assert.AreEqual (HiDpi.PixelSize (100, 2), thumbs.LastSize);
	}

	[UiTestMethod]
	public async Task VisibleWindow_DecodesThatWindowNotTheCatalog ()
	{
		var thumbs = new CountingThumbnails ();
		var viewModel = new MainWindowViewModel (null, new RecordingShell (), thumbs);
		var photos = new IPhoto[10_000];
		for (int i = 0; i < photos.Length; i++)
			photos[i] = new StubPhoto ($"/photos/{i}.jpg");
		viewModel.SeedCatalog (photos);
		int already = thumbs.Calls;

		for (int i = 200; i < 240; i++)
			await viewModel.RealizeThumbnailAsync (new object (), viewModel.Photos[i]);

		Assert.AreEqual (40, thumbs.Calls - already);
	}

	[UiTestMethod]
	[DoNotParallelize]
	public async Task ScrollingPastAPhoto_DisposesItsThumbnail ()
	{
		AvaloniaApp.Ensure ();
		var thumbs = new BitmapThumbnails ();
		var viewModel = new MainWindowViewModel (null, new RecordingShell (), thumbs);
		viewModel.SeedCatalog (new StubPhoto ("/a.jpg"), new StubPhoto ("/b.jpg"));
		var container = new object ();
		await viewModel.RealizeThumbnailAsync (container, viewModel.Photos[0]);
		var thumbnail = viewModel.Photos[0].Thumbnail;
		Assert.IsNotNull (thumbnail);

		viewModel.ReleaseThumbnail (container);

		Assert.IsNull (viewModel.Photos[0].Thumbnail);
		Assert.ThrowsExactly<ObjectDisposedException> (() => _ = thumbnail.PixelSize);
	}

	[UiTestMethod]
	public async Task ReleasingAThumbnailCancelsThatDecodeAndLeavesTheOneStillShown ()
	{
		var thumbs = new TokenThumbnails ();
		var viewModel = new MainWindowViewModel (null, new RecordingShell (), thumbs);
		viewModel.SeedCatalog (new StubPhoto ("/a.jpg"), new StubPhoto ("/b.jpg"));
		var firstHold = new object ();
		var stillShown = new object ();
		var secondHold = new object ();
		int before = thumbs.Calls;
		await viewModel.RealizeThumbnailAsync (firstHold, viewModel.Photos[0]);
		await viewModel.RealizeThumbnailAsync (secondHold, viewModel.Photos[1]);
		await viewModel.RealizeThumbnailAsync (stillShown, viewModel.Photos[0]);
		Assert.AreEqual (before + 2, thumbs.Calls);
		var firstToken = thumbs.Loads[before].Token;
		var secondToken = thumbs.Loads[before + 1].Token;

		viewModel.ReleaseThumbnail (firstHold);
		Assert.IsFalse (firstToken.IsCancellationRequested);
		Assert.IsFalse (secondToken.IsCancellationRequested);

		viewModel.ReleaseThumbnail (stillShown);
		Assert.IsTrue (firstToken.IsCancellationRequested);
		Assert.IsFalse (secondToken.IsCancellationRequested);
	}

	[UiTestMethod]
	[DoNotParallelize]
	public async Task Editing_DecodesOneCappedPreviewAndDisposesThePrevious ()
	{
		AvaloniaApp.Ensure ();
		var thumbs = new BitmapThumbnails ();
		var viewModel = new MainWindowViewModel (null, new RecordingShell (), thumbs) {
			UiScale = 3
		};
		viewModel.SeedCatalog (new StubPhoto ("/a.jpg"), new StubPhoto ("/b.jpg"));
		viewModel.ViewMode = LibraryViewMode.PhotoView;
		await viewModel.ReloadPreviewAsync ();
		Assert.Contains (EditorPreview.DecodeWidth (3), thumbs.Sizes);
		var first = viewModel.Preview;
		Assert.IsNotNull (first);

		viewModel.SelectedPhoto = viewModel.Photos[1];
		await viewModel.ReloadPreviewAsync ();

		Assert.ThrowsExactly<ObjectDisposedException> (() => _ = first.PixelSize);
		Assert.IsNotNull (viewModel.Preview);
		viewModel.ViewMode = LibraryViewMode.IconView;
		await viewModel.ReloadPreviewAsync ();
		Assert.IsNull (viewModel.Preview);
	}

	[UiTestMethod]
	public async Task ImportList_DoesNotDecodeEveryFile ()
	{
		var thumbs = new CountingThumbnails ();
		var viewModel = new ImportViewModel (null!, new RecordingShell (), thumbs, static _ => { });
		for (int i = 0; i < 1_000; i++)
			viewModel.AddPendingPhoto (new PhotoItemViewModel (new StubPhoto ($"/import/{i}.jpg")));
		Assert.AreEqual (1, thumbs.Calls);

		for (int i = 0; i < 30; i++)
			await viewModel.RealizeThumbnailAsync (new object (), viewModel.Photos[i]);

		Assert.AreEqual (31, thumbs.Calls);
	}

	[UiTestMethod]
	public async Task ReleasingAnImportThumbnailCancelsThatDecodeAndLeavesTheOneStillShown ()
	{
		var thumbs = new TokenThumbnails ();
		var viewModel = new ImportViewModel (null!, new RecordingShell (), thumbs, static _ => { });
		viewModel.AddPendingPhoto (new PhotoItemViewModel (new StubPhoto ("/import/a.jpg")));
		viewModel.AddPendingPhoto (new PhotoItemViewModel (new StubPhoto ("/import/b.jpg")));
		var firstHold = new object ();
		var stillShown = new object ();
		var secondHold = new object ();
		int before = thumbs.Calls;
		await viewModel.RealizeThumbnailAsync (firstHold, viewModel.Photos[0]);
		await viewModel.RealizeThumbnailAsync (secondHold, viewModel.Photos[1]);
		await viewModel.RealizeThumbnailAsync (stillShown, viewModel.Photos[0]);
		Assert.AreEqual (before + 2, thumbs.Calls);
		var firstToken = thumbs.Loads[before].Token;
		var secondToken = thumbs.Loads[before + 1].Token;

		viewModel.ReleaseThumbnail (firstHold);
		Assert.IsFalse (firstToken.IsCancellationRequested);
		Assert.IsFalse (secondToken.IsCancellationRequested);

		viewModel.ReleaseThumbnail (stillShown);
		Assert.IsTrue (firstToken.IsCancellationRequested);
		Assert.IsFalse (secondToken.IsCancellationRequested);
	}

	[UiTestMethod]
	public async Task Import_UsesASnapshotSoAScanCannotChangeTheSetBeingImported ()
	{
		bool reloaded = false;
		var viewModel = new ImportViewModel (null!, new RecordingShell (), new CountingThumbnails (), _ => reloaded = true);
		viewModel.SourcePath = Path.Combine (Path.GetTempPath (), "fspot-import-source");
		using var release = new ManualResetEventSlim (false);
		viewModel.ScanOverride = _ => ScanTwoThenBlock (release);
		var importer = new FrozenImport (() => {
			viewModel.RecurseSubdirectories = !viewModel.RecurseSubdirectories;
			viewModel.MergeRawAndJpeg = !viewModel.MergeRawAndJpeg;
			viewModel.AddScanned (new StubPhoto ("/import/late.jpg"));
		});
		viewModel.Importer = () => importer;

		var scanning = viewModel.ScanAsync ();
		await PumpUntil (() => viewModel.Photos.Count == 1);
		var importing = viewModel.ImportCommand.ExecuteAsync (null);
		await Pump ();
		Assert.IsFalse (importing.IsCompleted);

		release.Set ();
		await PumpUntil (() => viewModel.Photos.Count >= 2);
		await importing;
		await scanning;

		Assert.AreEqual (2, importer.Seen);
		Assert.AreEqual (2, importer.Enumerated);
		Assert.HasCount (3, viewModel.Photos);
		Assert.IsTrue (reloaded);
	}

	[UiTestMethod]
	public async Task Import_WhileTheScanIsStillRunning_StopsOfferingImport_AndKeepsTheOriginalSource ()
	{
		string root = Path.Combine (Path.GetTempPath (), "fspot-import-switch-" + Guid.NewGuid ().ToString ("N"));
		string sourceA = Path.Combine (root, "a");
		string sourceB = Path.Combine (root, "b");
		Directory.CreateDirectory (sourceA);
		Directory.CreateDirectory (sourceB);
		bool reloaded = false;
		var viewModel = new ImportViewModel (null!, new RecordingShell (), new CountingThumbnails (), _ => reloaded = true);
		viewModel.SourcePath = sourceA;
		using var release = new ManualResetEventSlim (false);
		viewModel.ScanOverride = _ => viewModel.SourcePath == sourceB
			? new IPhoto[] { new StubPhoto ("/import/from-b.jpg") }
			: ScanTwoThenBlock (release);
		var importer = new FrozenImport (() => { });
		viewModel.Importer = () => importer;

		try {
			var scanning = viewModel.ScanAsync ();
			await PumpUntil (() => viewModel.Photos.Count == 1);
			var importing = viewModel.ImportCommand.ExecuteAsync (null);
			await Pump ();
			Assert.AreEqual (Strings.ImportingPhotos, viewModel.StatusText);

			viewModel.SelectedSourceOption = ImportSourceOption.ForSource ("Other", sourceB);
			release.Set ();
			await importing;
			await scanning;

			Assert.HasCount (2, importer.Names);
			Assert.AreEqual ("a.jpg", importer.Names[0]);
			Assert.AreEqual ("b.jpg", importer.Names[1]);
			Assert.IsTrue (reloaded);
		} finally {
			release.Set ();
			if (Directory.Exists (root))
				Directory.Delete (root, true);
		}
	}

	[UiTestMethod]
	public async Task Import_AfterAFailure_ImportsAgain ()
	{
		int calls = 0;
		var viewModel = new ImportViewModel (null!, new RecordingShell (), new CountingThumbnails (), static _ => { });
		viewModel.AddScanned (new StubPhoto ("/import/a.jpg"));
		viewModel.Importer = () => new ThrowingImport (() => calls++);

		await viewModel.ImportCommand.ExecuteAsync (null);

		Assert.IsTrue (viewModel.CanImport);
		Assert.IsFalse (viewModel.IsBusy);
		Assert.AreEqual ("import broke", viewModel.StatusText);

		await viewModel.ImportCommand.ExecuteAsync (null);
		Assert.AreEqual (2, calls);
	}

	[UiTestMethod]
	public async Task RemovePhoto_KeepsThePhotoWhenTheOriginalIsLockedAndTheDefaultWasEdited ()
	{
		string root = Path.Combine (Path.GetTempPath (), "fspot-slide-lock-" + Guid.NewGuid ().ToString ("N"));
		string catalogDir = Path.Combine (root, "catalog");
		string originalDir = Path.Combine (root, "originals");
		string editDir = Path.Combine (root, "edits");
		Directory.CreateDirectory (catalogDir);
		Directory.CreateDirectory (originalDir);
		Directory.CreateDirectory (editDir);
		string previousBase = FSpot.Settings.FSpotConfiguration.BaseDirectory;
		SafeUri previousLibrary = FSpot.Settings.FSpotConfiguration.PhotoUri;
		Db? database = null;
		var lockedDir = new DirectoryInfo (originalDir);
		UnixFileMode previousMode = default;
		FileAttributes previousAttributes = default;
		string originalPath = Path.Combine (originalDir, "carol.jpg");
		bool windows = OperatingSystem.IsWindows ();
		bool locked = false;
		try {
			EnsureMergeServices ();
			File.Copy (Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg"), originalPath);
			string editedPath = Path.Combine (editDir, "edited.jpg");
			File.Copy (originalPath, editedPath);
			var images = TinyIoC.TinyIoCContainer.Current.Resolve<FSpot.Imaging.IImageFileFactory> ();
			var thumbs = TinyIoC.TinyIoCContainer.Current.Resolve<FSpot.Thumbnail.IThumbnailService> ();
			database = new Db (images, thumbs, new MergeUpdater ());
			database.Init (Path.Combine (catalogDir, FSpot.Settings.FSpotConfiguration.DatabaseName), true);
			FSpot.Settings.FSpotConfiguration.BaseDirectory = catalogDir;
			FSpot.Settings.FSpotConfiguration.PhotoUri = SafeUri.FromPath (Path.Combine (root, "library"));
			Directory.CreateDirectory (FSpot.Settings.FSpotConfiguration.PhotoUri.LocalPath);
			uint roll = database.Rolls.Create (new DateTime (2019, 3, 4, 0, 0, 0, DateTimeKind.Utc)).Id;
			Photo created = database.Photos.CreateFrom (new FilePhoto (SafeUri.FromPath (originalPath)), true, roll);
			uint edited = created.AddVersion (SafeUri.FromPath (editDir), "edited.jpg", "Edited");
			created.DefaultVersionId = edited;
			database.Photos.Commit (created);

			var shell = new RecordingShell { ConfirmResult = true };
			var viewModel = new MainWindowViewModel (database, shell, new NullThumbnails ());
			viewModel.LoadCatalog ();
			Assert.HasCount (1, viewModel.Photos);
			var photo = (Photo)viewModel.Photos[0].Photo;
			string slideshowPath = photo.DefaultVersion.Uri.LocalPath;
			Assert.AreEqual ("edited.jpg", Path.GetFileName (slideshowPath));
			originalPath = photo.VersionUri (Photo.OriginalVersionId).LocalPath;

			if (windows) {
				previousAttributes = File.GetAttributes (originalPath);
				File.SetAttributes (originalPath, previousAttributes | FileAttributes.ReadOnly);
			} else {
				previousMode = lockedDir.UnixFileMode;
				lockedDir.UnixFileMode = UnixFileMode.UserRead | UnixFileMode.UserExecute;
			}
			locked = true;

			CatalogRemoval removed = await viewModel.RemovePhotoAsync (slideshowPath, deleteFromDrive: true);

			Assert.IsFalse (File.Exists (slideshowPath));
			Assert.IsTrue (File.Exists (originalPath));
			Assert.HasCount (1, viewModel.Photos);
			Assert.IsFalse (removed.LeftGrid);
			Assert.AreEqual (originalPath, removed.Path);
		} finally {
			if (locked) {
				if (windows)
					File.SetAttributes (originalPath, previousAttributes);
				else
					lockedDir.UnixFileMode = previousMode;
			}
			database?.Dispose ();
			FSpot.Settings.FSpotConfiguration.BaseDirectory = previousBase;
			FSpot.Settings.FSpotConfiguration.PhotoUri = previousLibrary;
			if (Directory.Exists (root))
				Directory.Delete (root, true);
		}
	}

	[UiTestMethod]
	public void RotateSelection_MissingFile_NamesTheDecodedFile ()
	{
		string directory = Path.Combine (Path.GetTempPath (), "fspot-rotate-ui-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (directory);
		string path = Path.Combine (directory, "Carol Jones.jpg");
		try {
			EnsureMergeServices ();
			var shell = new RecordingShell ();
			var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
			var item = new PhotoItemViewModel (new FilePhoto (SafeUri.FromPath (path)));
			viewModel.Photos.Add (item);
			viewModel.SelectPhoto (item);

			viewModel.RotateRightCommand.Execute (null);

			Assert.AreEqual (Strings.ErrorWhileRotatingPhoto, shell.WarningTitle);
			Assert.AreEqual (
				string.Format (Strings.ReceivedErrorXWhileAttemptingToRotateY, Strings.FileNotFonud, "Carol Jones.jpg"),
				shell.WarningMessage);
		} finally {
			Directory.Delete (directory, true);
		}
	}

	static IEnumerable<IPhoto> ScanTwoThenBlock (ManualResetEventSlim release)
	{
		yield return new StubPhoto ("/import/a.jpg");
		if (!release.Wait (TimeSpan.FromSeconds (5)))
			yield break;
		yield return new StubPhoto ("/import/b.jpg");
	}

	static async Task Pump ()
	{
		await Dispatcher.UIThread.InvokeAsync (() => { }, DispatcherPriority.Background);
		await Dispatcher.UIThread.InvokeAsync (() => { }, DispatcherPriority.Background);
	}

	// The scan posts each photo from the thread pool. A fixed hop count can
	// finish before that post exists.
	static async Task PumpUntil (Func<bool> done)
	{
		long started = Environment.TickCount64;
		while (!done () && Environment.TickCount64 - started < 5_000) {
			await Pump ();
			await Task.Delay (10);
		}

		Assert.IsTrue (done ());
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void ClosingTheImportWindow_CancelsInFlightWorkWithoutClosingTwice ()
	{
		AvaloniaApp.Ensure ();
		int closes = 0;
		var viewModel = new ImportViewModel (null!, new RecordingShell (), new CountingThumbnails (), _ => closes++);
		var window = new ImportWindow (viewModel);
		window.Show ();
		try {
			window.Close ();
		} finally {
			if (window.IsVisible)
				window.Close ();
		}

		Assert.AreEqual (0, closes);
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void PhotoGrid_SizesCellsFromZoom ()
	{
		AvaloniaApp.Ensure ();
		var window = new MainWindow (null) { Width = 900, Height = 600 };
		window.Show ();
		try {
			var viewModel = (MainWindowViewModel)window.DataContext!;
			var photos = new IPhoto[200];
			for (int i = 0; i < photos.Length; i++)
				photos[i] = new StubPhoto ($"/photos/{i}.jpg");
			viewModel.SeedCatalog (photos);
			viewModel.Zoom = 160;
			Dispatcher.UIThread.RunJobs ();
			var panel = window.GetVisualDescendants ().OfType<Controls.UniformVirtualizingWrapPanel> ().Single ();
			Assert.AreEqual (viewModel.GridCellWidth, panel.ItemWidth, 0.5);
			Assert.AreEqual (viewModel.GridCellHeight, panel.ItemHeight, 0.5);
			Assert.IsGreaterThan (0, panel.RealizedCount);
			Assert.IsLessThan (80, panel.RealizedCount);
		} finally {
			window.Close ();
		}
	}

	[UiTestMethod]
	public async Task HoverPreview_StaysClosedWhenThePreferenceIsOff ()
	{
		bool previous = LibraryPreferences.ShowHoverPreview;
		try {
			LibraryPreferences.ShowHoverPreview = false;
			var viewModel = new MainWindowViewModel (null, new RecordingShell (), new CountingThumbnails ());
			viewModel.SeedCatalog (new StubPhoto ("/a.jpg"));
			await viewModel.ShowHoverAsync (viewModel.Photos[0], 1920, 1080);
			Assert.IsFalse (viewModel.ShowHoverPreview);
		} finally {
			LibraryPreferences.ShowHoverPreview = previous;
		}
	}

	[UiTestMethod]
	[DoNotParallelize]
	public async Task ReplacingTheHoveredThumbnail_KeepsThePreviewOpen ()
	{
		AvaloniaApp.Ensure ();
		var thumbs = new BitmapThumbnails ();
		var viewModel = new MainWindowViewModel (null, new RecordingShell (), thumbs) {
			Zoom = 100,
			UiScale = 1
		};
		viewModel.SeedCatalog (new StubPhoto ("/a.jpg"));
		await viewModel.RealizeThumbnailAsync (new object (), viewModel.Photos[0]);
		await viewModel.ShowHoverAsync (viewModel.Photos[0], 1920, 1080);
		Assert.IsTrue (viewModel.ShowHoverPreview);
		Assert.AreEqual (HoverPreview.Edge (1920, 1080), viewModel.HoverEdge);
		Assert.AreNotSame (viewModel.Photos[0].Thumbnail, viewModel.HoverThumbnail);
		var preview = viewModel.HoverThumbnail;

		viewModel.Zoom = 160;
		await viewModel.ReloadThumbnailsAsync ();

		Assert.IsTrue (viewModel.ShowHoverPreview);
		Assert.AreSame (preview, viewModel.HoverThumbnail);
		Assert.Contains (viewModel.Photos[0].Photo.Name, viewModel.HoverDetail);
	}

	[UiTestMethod]
	public async Task OpeningALargeCatalog_DoesNotDecodeEveryPhoto ()
	{
		var thumbs = new CountingThumbnails ();
		var viewModel = new MainWindowViewModel (null, new RecordingShell (), thumbs);
		var photos = new IPhoto[10_000];
		for (int i = 0; i < photos.Length; i++)
			photos[i] = new StubPhoto ($"/photos/{i}.jpg");

		viewModel.SeedCatalog (photos);
		await viewModel.ReloadThumbnailsAsync ();

		// The focused photo may build one histogram. The other 9,999 photos stay undecoded.
		Assert.IsLessThanOrEqualTo (1, thumbs.Calls);
	}

	[UiTestMethod]
	public async Task ImportThumbnail_RetriesAfterADecodeFailure ()
	{
		var thumbs = new FailUntilAllowed ();
		var viewModel = new ImportViewModel (null!, new RecordingShell (), thumbs, static _ => { });
		viewModel.AddPendingPhoto (new PhotoItemViewModel (new StubPhoto ("/import/a.jpg")));
		var container = new object ();

		await viewModel.RealizeThumbnailAsync (container, viewModel.Photos[0]);
		Assert.AreEqual (1, thumbs.GridCalls);
		Assert.IsNull (viewModel.Photos[0].Thumbnail);

		thumbs.Allow = true;
		await viewModel.RealizeThumbnailAsync (container, viewModel.Photos[0]);
		Assert.AreEqual (2, thumbs.GridCalls);
		Assert.IsNotNull (viewModel.Photos[0].Thumbnail);
	}

	[UiTestMethod]
	public async Task ReloadThumbnails_ContinuesAfterOneItemIsCancelled ()
	{
		var thumbs = new CancelSecondOfFirstPhoto ();
		var viewModel = new MainWindowViewModel (null, new RecordingShell (), thumbs);
		viewModel.SeedCatalog (new StubPhoto ("/a.jpg"), new StubPhoto ("/b.jpg"));
		await viewModel.RealizeThumbnailAsync (new object (), viewModel.Photos[0]);
		await viewModel.RealizeThumbnailAsync (new object (), viewModel.Photos[1]);
		Assert.AreEqual (1, thumbs.BLoads);

		thumbs.CancelA = true;
		await viewModel.ReloadThumbnailsAsync ();

		Assert.AreEqual (2, thumbs.BLoads);
	}

	[UiTestMethod]
	public async Task ReloadThumbnails_ReleasedCellIsLoadedAgainWhenItComesBack ()
	{
		var thumbs = new HoldAThenCount ();
		var viewModel = new MainWindowViewModel (null, new RecordingShell (), thumbs);
		viewModel.SeedCatalog (new StubPhoto ("/a.jpg"), new StubPhoto ("/b.jpg"));
		var bContainer = new object ();
		await viewModel.RealizeThumbnailAsync (new object (), viewModel.Photos[0]);
		await viewModel.RealizeThumbnailAsync (bContainer, viewModel.Photos[1]);
		Assert.AreEqual (1, thumbs.BLoads);

		thumbs.HoldA = true;
		Task reload = viewModel.ReloadThumbnailsAsync ();
		await thumbs.EnteredA.Task.WaitAsync (TestContext.CancellationToken);
		viewModel.ReleaseThumbnail (bContainer);
		thumbs.ReleaseA.SetResult (TinyBitmap ());
		await reload.WaitAsync (TestContext.CancellationToken);

		Assert.IsNull (viewModel.Photos[1].Thumbnail);
		Assert.AreEqual (1, thumbs.BLoads);
		await viewModel.RealizeThumbnailAsync (new object (), viewModel.Photos[1]);
		Assert.AreEqual (2, thumbs.BLoads);
		Assert.IsNotNull (viewModel.Photos[1].Thumbnail);
	}

	[UiTestMethod]
	public async Task RealizeThumbnail_RetriesAfterADecodeFailure ()
	{
		var thumbs = new FailUntilAllowed ();
		var viewModel = new MainWindowViewModel (null, new RecordingShell (), thumbs);
		viewModel.SeedCatalog (new StubPhoto ("/a.jpg"));
		var container = new object ();

		await viewModel.RealizeThumbnailAsync (container, viewModel.Photos[0]);
		Assert.AreEqual (1, thumbs.GridCalls);
		Assert.IsNull (viewModel.Photos[0].Thumbnail);

		thumbs.Allow = true;
		await viewModel.RealizeThumbnailAsync (container, viewModel.Photos[0]);
		Assert.AreEqual (2, thumbs.GridCalls);
		Assert.IsNotNull (viewModel.Photos[0].Thumbnail);
	}
	[UiTestMethod]
	public void SelectTag_HighlightsWithoutTogglingOff ()
	{
		var viewModel = NewViewModel ();
		var tag = new TagNode (new Tag (null, 1, "Vacation"));

		viewModel.SelectTagCommand.Execute (tag);
		viewModel.SelectTagCommand.Execute (tag);

		Assert.AreSame (tag, viewModel.SelectedTag);
	}

	[UiTestMethod]
	public void SelectTag_NullClearsTheHighlight ()
	{
		var viewModel = NewViewModel ();
		var tag = new TagNode (new Tag (null, 1, "Vacation"));
		viewModel.SelectTagCommand.Execute (tag);

		viewModel.SelectTagCommand.Execute (null);

		Assert.IsNull (viewModel.SelectedTag);
	}

	[UiTestMethod]
	public void DeleteTagCommand_CanExecute_ForARightClickedTagWithoutAPriorSelection ()
	{
		var viewModel = NewViewModel ();
		var tag = new TagNode (new Tag (null, 1, "Vacation"));

		Assert.IsNull (viewModel.SelectedTag);
		Assert.IsFalse (viewModel.DeleteTagCommand.CanExecute (null));
		Assert.IsTrue (viewModel.DeleteTagCommand.CanExecute (tag));
	}

	[UiTestMethod]
	public async Task DeleteTag_WarnsInsteadOfConfirmingWhenTheTagHasChildren ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		var root = new Category (null, 1, "Root");
		_ = new Tag (root, 2, "Vacation");
		var node = new TagNode (root);

		await viewModel.DeleteTagCommand.ExecuteAsync (node);

		Assert.AreEqual (Strings.TagIsNotEmpty, shell.WarningTitle);
		string? warning = shell.WarningMessage;
		Assert.IsNotNull (warning);
		Assert.Contains ("Root", warning);
		Assert.IsFalse (shell.ConfirmCalled);
	}

	[UiTestMethod]
	public void BeginRenameTag_SelectsTheTagSoDeleteCanRun ()
	{
		var viewModel = NewViewModel ();
		var tag = new TagNode (new Tag (null, 1, "Vacation"));

		viewModel.BeginRenameTagCommand.Execute (tag);

		Assert.AreSame (tag, viewModel.SelectedTag);
		Assert.IsTrue (tag.IsRenaming);
		Assert.IsTrue (viewModel.DeleteTagCommand.CanExecute (null));
	}

	[UiTestMethod]
	public void ActivateTag_OnACategory_ShowsPhotosTaggedWithChildren ()
	{
		var viewModel = NewViewModel ();
		var root = new Category (null, 1, "Root");
		var vacation = new Tag (root, 2, "Vacation");
		viewModel.SeedCatalog (
			new StubPhoto ("/beach.jpg", tags: vacation),
			new StubPhoto ("/desk.jpg", tags: new Tag (null, 9, "Work")));

		viewModel.ActivateTagCommand.Execute (new TagNode (root));

		Assert.HasCount (1, viewModel.Photos);
		Assert.AreEqual ("beach.jpg", viewModel.Photos[0].Caption);
	}

	[UiTestMethod]
	public void ActivateTag_SetsTheFindChipWithoutClearingTheHighlight ()
	{
		var viewModel = NewViewModel ();
		var tag = new TagNode (new Tag (null, 1, "Vacation"));
		viewModel.SelectTagCommand.Execute (tag);

		viewModel.ActivateTagCommand.Execute (tag);

		Assert.AreSame (tag, viewModel.SelectedTag);
		Assert.AreSame (tag, viewModel.ActiveTagFilter);
		Assert.IsTrue (viewModel.HasLibraryFilter);
	}

	[UiTestMethod]
	public void ClearTagFilter_RemovesTheFindChipAndKeepsTheHighlight ()
	{
		var viewModel = NewViewModel ();
		var tag = new TagNode (new Tag (null, 1, "Vacation"));
		viewModel.SelectTagCommand.Execute (tag);
		viewModel.ActivateTagCommand.Execute (tag);

		viewModel.ClearTagFilterCommand.Execute (null);

		Assert.AreSame (tag, viewModel.SelectedTag);
		Assert.IsNull (viewModel.ActiveTagFilter);
		Assert.IsFalse (viewModel.HasLibraryFilter);
	}

	[UiTestMethod]
	public void SelectFolder_HighlightsWithoutTogglingOff ()
	{
		var viewModel = NewViewModel ();
		var folder = new FolderNode (new FolderTreeNode ("photos", new SafeUri ("file:///photos/", true), 2, []), true);

		viewModel.SelectFolderCommand.Execute (folder);
		viewModel.SelectFolderCommand.Execute (folder);

		Assert.AreSame (folder, viewModel.SelectedFolder);
		Assert.IsNull (viewModel.ActiveFolderFilter);
	}

	[UiTestMethod]
	public void ActivateFolder_SetsTheFindChip ()
	{
		var viewModel = NewViewModel ();
		var folder = new FolderNode (new FolderTreeNode ("photos", new SafeUri ("file:///photos/", true), 2, []), true);

		viewModel.ActivateFolderCommand.Execute (folder);

		Assert.AreSame (folder, viewModel.ActiveFolderFilter);
		Assert.IsTrue (viewModel.HasLibraryFilter);
	}

	[UiTestMethod]
	public void EnteringPhotoView_SwitchesTheSidebarToEdit ()
	{
		var viewModel = NewViewModel ();
		Assert.AreEqual (0, viewModel.SidebarTab);

		viewModel.EditCommand.Execute (null);

		Assert.AreEqual (2, viewModel.SidebarTab);
		Assert.AreEqual (LibraryViewMode.PhotoView, viewModel.ViewMode);
		Assert.AreEqual (Strings.Edit, viewModel.SidebarPageTitle);
	}

	[UiTestMethod]
	public void SelectPhoto_WithoutAdditive_IsSingleSelection ()
	{
		var viewModel = NewViewModel ();
		var first = new PhotoItemViewModel (new StubPhoto ("/a.jpg"));
		var second = new PhotoItemViewModel (new StubPhoto ("/b.jpg"));
		viewModel.Photos.Add (first);
		viewModel.Photos.Add (second);

		viewModel.SelectPhoto (first);
		viewModel.SelectPhoto (second);

		Assert.IsFalse (first.Selected);
		Assert.IsTrue (second.Selected);
		Assert.AreSame (second, viewModel.SelectedPhoto);
	}

	[UiTestMethod]
	public void SelectPhoto_Additive_KeepsThePreviousSelection ()
	{
		var viewModel = NewViewModel ();
		var first = new PhotoItemViewModel (new StubPhoto ("/a.jpg"));
		var second = new PhotoItemViewModel (new StubPhoto ("/b.jpg"));
		viewModel.Photos.Add (first);
		viewModel.Photos.Add (second);

		viewModel.SelectPhoto (first);
		viewModel.SelectPhoto (second, additive: true);

		Assert.IsTrue (first.Selected);
		Assert.IsTrue (second.Selected);
		Assert.AreSame (second, viewModel.SelectedPhoto);
	}

	[UiTestMethod]
	public void EmptyStates_AreHiddenWhenThereIsNoDatabase ()
	{
		var viewModel = NewViewModel ();

		Assert.IsFalse (viewModel.ShowEmptyLibrary);
		Assert.IsFalse (viewModel.ShowNoMatches);
	}

	[UiTestMethod]
	public void LeavingPhotoView_RestoresTheBrowseSidebarTab ()
	{
		var viewModel = NewViewModel ();
		viewModel.SidebarTab = 1;
		viewModel.EditCommand.Execute (null);

		viewModel.BrowseCommand.Execute (null);

		Assert.AreEqual (1, viewModel.SidebarTab);
		Assert.AreEqual (LibraryViewMode.IconView, viewModel.ViewMode);
		Assert.AreEqual (Strings.Folders, viewModel.SidebarPageTitle);
	}

	[UiTestMethod]
	public void HideSidebar_ThenToggleSidebar_ShowsItAgain ()
	{
		var viewModel = NewViewModel ();
		Assert.IsTrue (viewModel.DisplaySidebar);

		viewModel.HideSidebarCommand.Execute (null);
		Assert.IsFalse (viewModel.DisplaySidebar);

		viewModel.ToggleSidebarCommand.Execute (null);
		Assert.IsTrue (viewModel.DisplaySidebar);
	}

	[UiTestMethod]
	public void RotateCommands_CanExecute_WhenTheFocusedPhotoIsNotFlaggedSelected ()
	{
		var viewModel = NewViewModel ();
		var item = new PhotoItemViewModel (new StubPhoto ("/a.jpg"));
		viewModel.Photos.Add (item);
		viewModel.SelectedPhoto = item;

		Assert.IsFalse (item.Selected);
		Assert.IsTrue (viewModel.RotateLeftCommand.CanExecute (null));
		Assert.IsTrue (viewModel.RotateRightCommand.CanExecute (null));
	}

	[UiTestMethod]
	public void RotateCommands_CannotExecute_WithoutAFocusedOrSelectedPhoto ()
	{
		var viewModel = NewViewModel ();
		viewModel.Photos.Add (new PhotoItemViewModel (new StubPhoto ("/a.jpg")));

		Assert.IsFalse (viewModel.RotateLeftCommand.CanExecute (null));
		Assert.IsFalse (viewModel.RotateRightCommand.CanExecute (null));
	}

	[UiTestMethod]
	public void ToggleLoupe_ShowsThenHides ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();
		viewModel.EditCommand.Execute (null);
		Assert.IsFalse (viewModel.IsLoupeVisible);

		viewModel.ToggleLoupeCommand.Execute (null);
		Assert.IsTrue (viewModel.IsLoupeVisible);

		viewModel.ToggleLoupeCommand.Execute (null);
		Assert.IsFalse (viewModel.IsLoupeVisible);
	}

	[UiTestMethod]
	public void ShowLoupe_RequiresEditModeAndAPreview ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();
		viewModel.ToggleLoupeCommand.Execute (null);
		Assert.IsFalse (viewModel.ShowLoupe);

		viewModel.EditCommand.Execute (null);
		Assert.IsFalse (viewModel.ShowLoupe);
	}

	[UiTestMethod]
	public void ToggleSidebar_HidesWhenVisible ()
	{
		var viewModel = NewViewModel ();
		viewModel.ToggleSidebarCommand.Execute (null);
		Assert.IsFalse (viewModel.DisplaySidebar);
	}

	[UiTestMethod]
	public void VisibleEditors_InBrowse_AreTheMultiPhotoTools ()
	{
		var viewModel = NewViewModel ();
		var ids = viewModel.VisibleEditors.Select (editor => editor.Id).ToList ();

		Assert.Contains (PhotoEditorCatalog.Desaturate.Id, ids);
		Assert.Contains (PhotoEditorCatalog.Flip.Id, ids);
		Assert.DoesNotContain (PhotoEditorCatalog.Crop.Id, ids);
		Assert.DoesNotContain (PhotoEditorCatalog.RedEye.Id, ids);
	}

	[UiTestMethod]
	public void VisibleEditors_InPhotoView_IncludeSelectionTools ()
	{
		var viewModel = NewViewModel ();
		viewModel.EditCommand.Execute (null);
		var ids = viewModel.VisibleEditors.Select (editor => editor.Id).ToList ();

		Assert.Contains (PhotoEditorCatalog.Crop.Id, ids);
		Assert.Contains (PhotoEditorCatalog.Color.Id, ids);
		Assert.HasCount (PhotoEditorCatalog.All.Count, ids);
	}

	[UiTestMethod]
	public void ApplyEditor_Tilt_StartsASettingsSessionThatCanCommit ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();

		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.Tilt);

		Assert.AreEqual (PhotoEditorCatalog.Tilt.Id, viewModel.EditorSession?.Editor.Id);
		Assert.IsTrue (viewModel.ShowTiltSettings);
		Assert.IsTrue (viewModel.CommitEditorCommand.CanExecute (null));
	}

	[UiTestMethod]
	public void ApplyEditor_Sharpen_StartsASettingsSession ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();

		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.Sharpen);

		Assert.AreEqual (PhotoEditorCatalog.Sharpen.Id, viewModel.EditorSession?.Editor.Id);
		Assert.IsTrue (viewModel.ShowSharpenSettings);
		Assert.AreEqual (0.5, viewModel.EditorSession?.Context.SharpenAmount);
	}

	[UiTestMethod]
	public void ApplyEditor_SoftFocus_StartsASettingsSession ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();

		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.SoftFocus);

		Assert.AreEqual (PhotoEditorCatalog.SoftFocus.Id, viewModel.EditorSession?.Editor.Id);
		Assert.IsTrue (viewModel.ShowSoftFocusSettings);
		Assert.AreEqual (0.5, viewModel.EditorSession?.Context.SoftFocusRadius);
	}

	[UiTestMethod]
	public void ApplyEditor_Crop_StartsASelectionSession ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();

		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.Crop);

		Assert.AreEqual (PhotoEditorCatalog.Crop.Id, viewModel.EditorSession?.Editor.Id);
		Assert.IsTrue (viewModel.ShowEditorSelection);
		Assert.IsFalse (viewModel.CommitEditorCommand.CanExecute (null));
	}

	[UiTestMethod]
	public void ApplyEditor_Color_StartsASettingsSessionThatCanCommit ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();

		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.Color);

		Assert.AreEqual (PhotoEditorCatalog.Color.Id, viewModel.EditorSession?.Editor.Id);
		Assert.IsTrue (viewModel.ShowColorSettings);
		Assert.IsTrue (viewModel.CommitEditorCommand.CanExecute (null));
	}

	[UiTestMethod]
	public void SetEditorSelection_EnablesCropCommit ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();
		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.Crop);

		viewModel.SetEditorSelection (new FSpot.Imaging.PixelRect (4, 8, 16, 16));

		Assert.IsTrue (viewModel.CommitEditorCommand.CanExecute (null));
		Assert.AreEqual (16, viewModel.EditorSession?.Selection.Width);
	}

	[UiTestMethod]
	public void SelectEditorRegionCommand_CommitsTheMappedRect ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();
		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.Crop);
		var rect = new FSpot.Imaging.PixelRect (10, 12, 30, 40);

		Assert.IsTrue (viewModel.SelectEditorRegionCommand.CanExecute (rect));
		viewModel.SelectEditorRegionCommand.Execute (rect);

		Assert.AreEqual (30, viewModel.EditorSession?.Selection.Width);
		Assert.IsTrue (viewModel.CommitEditorCommand.CanExecute (null));
	}

	[UiTestMethod]
	public void SelectEditorRegionCommand_EnablesCommitForEverySelectionEditor ()
	{
		var rect = new FSpot.Imaging.PixelRect (4, 8, 16, 16);
		foreach (var editor in PhotoEditorCatalog.All.Where (item => item.NeedsSelection)) {
			var viewModel = ViewModelWithFocusedPhoto ();
			viewModel.ApplyEditorCommand.Execute (editor);
			viewModel.SelectEditorRegionCommand.Execute (rect);
			Assert.IsTrue (viewModel.CommitEditorCommand.CanExecute (null), editor.Id);
			Assert.AreEqual (16, viewModel.EditorSession?.Selection.Width, editor.Id);
		}
	}

	[UiTestMethod]
	public void CancelEditor_ClearsTheSession ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();
		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.Resize);

		viewModel.CancelEditorCommand.Execute (null);

		Assert.IsNull (viewModel.EditorSession);
		Assert.IsFalse (viewModel.ShowEditorSettings);
	}

	[UiTestMethod]
	public void ApplyEditor_Crop_WithoutAPhoto_Warns ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());

		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.Crop);

		Assert.IsNull (viewModel.EditorSession);
		Assert.AreEqual (Strings.NoSelectionAvailable, shell.WarningMessage);
	}

	[UiTestMethod]
	public void FindByRating_FiltersTheLibraryAndDismissingTheChipRestoresIt ()
	{
		var viewModel = NewViewModel ();
		var low = new StubPhoto ("/low.jpg", new DateTime (2026, 6, 1), rating: 2);
		var high = new StubPhoto ("/high.jpg", new DateTime (2026, 6, 2), rating: 5);
		viewModel.SeedCatalog (low, high);

		viewModel.FindByRatingCommand.Execute (null);

		Assert.IsTrue (viewModel.HasFindRating);
		Assert.HasCount (1, viewModel.Photos);
		Assert.AreEqual ("high.jpg", viewModel.Photos[0].Caption);
		Assert.IsFalse (viewModel.ShowNoMatches);

		viewModel.DismissFindRatingCommand.Execute (null);

		Assert.IsFalse (viewModel.HasFindRating);
		Assert.HasCount (2, viewModel.Photos);
	}

	[UiTestMethod]
	public void FindByDate_AndImportRoll_SetDismissibleChips ()
	{
		var viewModel = NewViewModel ();
		viewModel.SeedCatalog (
			new StubPhoto ("/june.jpg", new DateTime (2026, 6, 15)),
			new StubPhoto ("/july.jpg", new DateTime (2026, 7, 2)));

		viewModel.FindByDateCommand.Execute (null);
		Assert.IsTrue (viewModel.HasFindDate);
		Assert.HasCount (1, viewModel.Photos);
		viewModel.DismissFindDateCommand.Execute (null);
		Assert.HasCount (2, viewModel.Photos);

		viewModel.FindByImportRollCommand.Execute (null);
		Assert.IsTrue (viewModel.HasFindRoll);
		viewModel.DismissFindRollCommand.Execute (null);
		Assert.IsFalse (viewModel.HasFindRoll);
	}

	[UiTestMethod]
	public void FindMatchAllTags_ChangesWhichPhotosMatch ()
	{
		var vacation = new Tag (null, 1, "Vacation");
		var work = new Tag (null, 2, "Work");
		var both = new StubPhoto ("/both.jpg", new DateTime (2026, 6, 1), 0, vacation, work);
		var onlyVacation = new StubPhoto ("/vac.jpg", new DateTime (2026, 6, 1), 0, vacation);
		var viewModel = NewViewModel ();
		viewModel.SeedCatalog (both, onlyVacation);
		viewModel.ActivateTagCommand.Execute (new TagNode (vacation));
		viewModel.ActivateTagCommand.Execute (new TagNode (work));

		Assert.HasCount (2, viewModel.Photos);

		viewModel.ToggleFindMatchAllTagsCommand.Execute (null);

		Assert.IsTrue (viewModel.FindMatchAllTags);
		Assert.HasCount (1, viewModel.Photos);
		Assert.AreEqual ("both.jpg", viewModel.Photos[0].Caption);
	}

	[UiTestMethod]
	public void FindThatMatchesNothing_ShowsNoMatchingPhotos ()
	{
		var viewModel = NewViewModel ();
		viewModel.SeedCatalog (new StubPhoto ("/a.jpg", new DateTime (2026, 1, 1), rating: 1));
		viewModel.FindByRatingCommand.Execute (null);

		Assert.IsTrue (viewModel.ShowNoMatches);
		Assert.IsTrue (viewModel.HasLibraryFilter);
		Assert.IsTrue (viewModel.ShowQueryBarNoMatches);
		Assert.IsEmpty (viewModel.Photos);
		Assert.AreEqual (QueryChipFormat.Rating (viewModel.FindMinRating, viewModel.FindMaxRating), viewModel.FindRatingLabel);
	}

	[UiTestMethod]
	public void FindTextThatMatchesNothing_ShowsFindBarNoMatches ()
	{
		var viewModel = NewViewModel ();
		viewModel.SeedCatalog (new StubPhoto ("/a.jpg", new DateTime (2026, 1, 1)));
		viewModel.ShowFindCommand.Execute (null);
		viewModel.FindText = "zzzz-no-such-photo";

		Assert.IsTrue (viewModel.IsFindBarVisible);
		Assert.IsTrue (viewModel.ShowNoMatches);
		Assert.IsTrue (viewModel.ShowFindBarNoMatches);
		Assert.IsEmpty (viewModel.Photos);
	}

	[UiTestMethod]
	public void CatalogPhotoItem_UsesTheCatalogIdAsAutomationId ()
	{
		var photo = new Photo (null, null, 42, DateTimeUtil.FromDateTime (new DateTime (2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)));
		photo.AddVersionUnsafely (Photo.OriginalVersionId, new SafeUri ("file:///photos/x.jpg"), "x.jpg", string.Empty, "Original", true);
		var item = new PhotoItemViewModel (photo);

		Assert.AreEqual ("photo.42", item.AutomationId);
		Assert.AreEqual (PhotoDateCaption.Format (photo, wide: false), item.DateCaption);
	}

	[UiTestMethod]
	public void PhotoItem_SkipsNullTagsWhenFormattingCaptions ()
	{
		var tag = new Tag (null, 1, "Vacation");
		var item = new PhotoItemViewModel (new StubPhoto ("/a.jpg", tags: [null, tag]));
		Assert.AreEqual ("Vacation", item.TagsCaption);
	}

	[UiTestMethod]
	public void FilePhotoItem_FallsBackToAStablePathAutomationId ()
	{
		var item = new PhotoItemViewModel (new StubPhoto ("/library/trip/img.jpg"));
		Assert.StartsWith ("photo.", item.AutomationId);
		Assert.DoesNotContain ("42", item.AutomationId);
		Assert.Contains ("img.jpg", item.AutomationId);
	}

	[UiTestMethod]
	public void FindByDate_UsesThePlaceholderChipFormat ()
	{
		var viewModel = NewViewModel ();
		viewModel.SeedCatalog (new StubPhoto ("/june.jpg", new DateTime (2026, 6, 15)));
		viewModel.FindByDateCommand.Execute (null);

		Assert.AreEqual (QueryChipFormat.Date (viewModel.FindStart, viewModel.FindEnd), viewModel.FindDateLabel);
	}

	[UiTestMethod]
	public void TimelineBucketToolTip_UsesTheMonthCountFormat ()
	{
		var viewModel = NewViewModel ();
		viewModel.SeedCatalog (new StubPhoto ("/june.jpg", new DateTime (2026, 6, 15)));
		var bucket = viewModel.TimelineBuckets.Single ();
		Assert.AreEqual (QueryChipFormat.TimelineMonth (bucket.Start, bucket.Count), bucket.ToolTip);
	}

	[UiTestMethod]
	public void SetTimelineRange_IncludesEveryMonthInTheSpan ()
	{
		var viewModel = NewViewModel ();
		viewModel.SeedCatalog (
			new StubPhoto ("/jan.jpg", new DateTime (2026, 1, 10)),
			new StubPhoto ("/mar.jpg", new DateTime (2026, 3, 10)),
			new StubPhoto ("/jun.jpg", new DateTime (2026, 6, 10)));

		viewModel.SetTimelineRangeCommand.Execute ((0, 2));

		Assert.HasCount (2, viewModel.Photos);
		Assert.DoesNotContain ("jun.jpg", viewModel.Photos.Select (item => item.Caption).ToList ());
	}

	[UiTestMethod]
	public void PreviewInterpolation_FollowsViewerInterpolation ()
	{
		var viewModel = NewViewModel ();
		viewModel.ViewerInterpolation = true;
		Assert.AreEqual (global::Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality, viewModel.PreviewInterpolation);
		viewModel.ViewerInterpolation = false;
		Assert.AreEqual (global::Avalonia.Media.Imaging.BitmapInterpolationMode.None, viewModel.PreviewInterpolation);
	}

	[UiTestMethod]
	public void HelpContentsCommand_OpensTheGtkHelpUri ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		viewModel.HelpContentsCommand.Execute (null);
		Assert.AreEqual (HelpContents.Current, shell.OpenedUri);
	}

	[UiTestMethod]
	public void ImportFromCameraCommand_WarnsWhenNoCamerasAreDetected ()
	{
		// Other tests register the real file system on the process container.
		// With that in place this Mac's root volume counts as an import source
		// and the command skips the empty-camera warning.
		var container = TinyIoC.TinyIoCContainer.Current;
		bool restoreFileSystem = container.Unregister<FSpot.FileSystem.IFileSystem> ();
		try {
			var shell = new RecordingShell ();
			var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
			viewModel.ImportFromCameraCommand.Execute (null);
			Assert.AreEqual (Strings.NoCamerasDetected, shell.WarningMessage);
		} finally {
			if (restoreFileSystem) {
				try {
					FSpot.FileSystem.ModuleController.RegisterTypes (container);
				} catch (ArgumentException) {
					// IEnvironment is still registered; IFileSystem was put back first.
				}
			}
		}
	}

	[UiTestMethod]
	public void SlideshowCommand_ShowsTheVisibleCollectionInOrder ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		viewModel.SeedCatalog (
			new StubPhoto ("/a.jpg", new DateTime (2026, 1, 1)),
			new StubPhoto ("/b.jpg", new DateTime (2026, 1, 2)));

		viewModel.SlideshowCommand.Execute (null);

		Assert.IsNotNull (shell.Slideshow);
		Assert.HasCount (2, shell.Slideshow);
		Assert.Contains ("a.jpg", shell.Slideshow[0]);
		Assert.Contains ("b.jpg", shell.Slideshow[1]);
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void IsMailEnabled_FollowsMailToEnabledPreferenceAtConstruction ()
	{
		bool original = LibraryPreferences.MailToEnabled;
		try {
			LibraryPreferences.MailToEnabled = false;
			Assert.IsFalse (new MainWindowViewModel (null, new RecordingShell (), new NullThumbnails ()).IsMailEnabled);

			LibraryPreferences.MailToEnabled = true;
			Assert.IsTrue (new MainWindowViewModel (null, new RecordingShell (), new NullThumbnails ()).IsMailEnabled);
		} finally {
			LibraryPreferences.MailToEnabled = original;
		}
	}

	[UiTestMethod]
	[DoNotParallelize]
	public async Task ShowPreferencesCommand_RefreshesIsMailEnabledFromPreferences ()
	{
		bool original = LibraryPreferences.MailToEnabled;
		try {
			LibraryPreferences.MailToEnabled = false;
			var viewModel = new MainWindowViewModel (null, new RecordingShell (), new NullThumbnails ());
			Assert.IsFalse (viewModel.IsMailEnabled);

			LibraryPreferences.MailToEnabled = true;
			await viewModel.ShowPreferencesCommand.ExecuteAsync (null);
			Assert.IsTrue (viewModel.IsMailEnabled);
		} finally {
			LibraryPreferences.MailToEnabled = original;
		}
	}

	[UiTestMethod]
	[DoNotParallelize]
	public async Task ShowPreferencesCommand_KeepsWindowTogglesThatPreferencesDoesNotWrite ()
	{
		bool sidebar = LibraryPreferences.ShowSidebar;
		bool timeline = LibraryPreferences.ShowTimeline;
		bool filmstrip = LibraryPreferences.ShowFilmstrip;
		try {
			LibraryPreferences.ShowSidebar = true;
			LibraryPreferences.ShowTimeline = true;
			LibraryPreferences.ShowFilmstrip = true;
			var viewModel = new MainWindowViewModel (null, new RecordingShell (), new NullThumbnails ()) {
				DisplaySidebar = false,
				DisplayTimeline = false,
				DisplayFilmstrip = false
			};

			await viewModel.ShowPreferencesCommand.ExecuteAsync (null);

			Assert.IsFalse (viewModel.DisplaySidebar);
			Assert.IsFalse (viewModel.DisplayTimeline);
			Assert.IsFalse (viewModel.DisplayFilmstrip);
		} finally {
			LibraryPreferences.ShowSidebar = sidebar;
			LibraryPreferences.ShowTimeline = timeline;
			LibraryPreferences.ShowFilmstrip = filmstrip;
		}
	}

	[UiTestMethod]
	public void PrintAndMail_UseTheSelectedPhotos ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		var first = new PhotoItemViewModel (new StubPhoto ("/a.jpg"));
		var second = new PhotoItemViewModel (new StubPhoto ("/b.jpg"));
		viewModel.Photos.Add (first);
		viewModel.Photos.Add (second);
		first.Selected = true;

		viewModel.PrintCommand.Execute (null);
		viewModel.MailCommand.Execute (null);

		Assert.IsNotNull (shell.Printed);
		Assert.HasCount (1, shell.Printed);
		Assert.Contains ("a.jpg", shell.Printed[0]);
		Assert.IsNotNull (shell.Mailed);
		Assert.HasCount (1, shell.Mailed);
	}

	[UiTestMethod]
	public void ExportCommand_CopiesTheSelectedFilesToTheFolder ()
	{
		string sourceDir = Path.Combine (Path.GetTempPath (), "fspot-vm-src-" + Guid.NewGuid ().ToString ("N"));
		string destDir = Path.Combine (Path.GetTempPath (), "fspot-vm-dst-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (sourceDir);
		string file = Path.Combine (sourceDir, "shot.jpg");
		File.WriteAllText (file, "photo");
		try {
			var shell = new RecordingShell { ExportFolder = destDir };
			var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
			var item = new PhotoItemViewModel (new StubPhoto (file));
			item.Selected = true;
			viewModel.Photos.Add (item);

			viewModel.ExportCommand.Execute (null);

			Assert.IsTrue (File.Exists (Path.Combine (destDir, "shot.jpg")));
		} finally {
			Directory.Delete (sourceDir, true);
			if (Directory.Exists (destDir))
				Directory.Delete (destDir, true);
		}
	}

	[UiTestMethod]
	public async Task ExportZipCommand_WritesTheSelectionIntoTheNamedArchive ()
	{
		string sourceDir = Path.Combine (Path.GetTempPath (), "fspot-vm-zip-src-" + Guid.NewGuid ().ToString ("N"));
		string destDir = Path.Combine (Path.GetTempPath (), "fspot-vm-zip-dst-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (sourceDir);
		string file = Path.Combine (sourceDir, "shot.jpg");
		File.WriteAllText (file, "photo");
		try {
			var shell = new RecordingShell { ExportFolder = destDir, PromptResult = "trip.zip" };
			var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
			var item = new PhotoItemViewModel (new StubPhoto (file));
			item.Selected = true;
			viewModel.Photos.Add (item);

			await viewModel.ExportZipCommand.ExecuteAsync (null);

			using var archive = System.IO.Compression.ZipFile.OpenRead (Path.Combine (destDir, "trip.zip"));
			Assert.HasCount (1, archive.Entries);
			Assert.AreEqual ("shot.jpg", archive.Entries[0].FullName);
			using var stream = archive.Entries[0].Open ();
			using var reader = new StreamReader (stream);
			Assert.AreEqual ("photo", reader.ReadToEnd ());
		} finally {
			Directory.Delete (sourceDir, true);
			if (Directory.Exists (destDir))
				Directory.Delete (destDir, true);
		}
	}

	[UiTestMethod]
	public async Task ExportZipCommand_ScalesTheLongerSideWhenAsked ()
	{
		string sourceDir = Path.Combine (Path.GetTempPath (), "fspot-vm-zip-src-" + Guid.NewGuid ().ToString ("N"));
		string destDir = Path.Combine (Path.GetTempPath (), "fspot-vm-zip-dst-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (sourceDir);
		string file = Path.Combine (sourceDir, "shot.jpg");
		File.Copy (Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg"), file);
		byte[] original = File.ReadAllBytes (file);
		try {
			var shell = new RecordingShell {
				ExportFolder = destDir,
				PromptResult = "trip.zip",
				AskForZipScale = true,
				ZipScale = new ZipScaleChoice (true, 32)
			};
			var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
			var item = new PhotoItemViewModel (new StubPhoto (file));
			item.Selected = true;
			viewModel.Photos.Add (item);

			await viewModel.ExportZipCommand.ExecuteAsync (null);

			Assert.AreSequenceEqual (original, File.ReadAllBytes (file));
			byte[] archived = ZipEntryBytes (Path.Combine (destDir, "trip.zip"), "shot.jpg");
			Assert.AreEqual (0xFF, archived[0]);
			Assert.AreEqual (0xD8, archived[1]);
			(int width, int height) = JpegSize (archived);
			int longer = Math.Max (width, height);
			Assert.IsLessThanOrEqualTo (32, longer);
			Assert.IsGreaterThanOrEqualTo (1, longer);
		} finally {
			Directory.Delete (sourceDir, true);
			if (Directory.Exists (destDir))
				Directory.Delete (destDir, true);
		}
	}

	[UiTestMethod]
	public async Task ExportZipCommand_WritesNothingWhenScaleIsCancelled ()
	{
		string sourceDir = Path.Combine (Path.GetTempPath (), "fspot-vm-zip-src-" + Guid.NewGuid ().ToString ("N"));
		string destDir = Path.Combine (Path.GetTempPath (), "fspot-vm-zip-dst-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (sourceDir);
		Directory.CreateDirectory (destDir);
		string file = Path.Combine (sourceDir, "shot.jpg");
		File.WriteAllText (file, "photo");
		try {
			var shell = new RecordingShell {
				ExportFolder = destDir,
				PromptResult = "trip.zip",
				AskForZipScale = true,
				ZipScale = null
			};
			var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
			var item = new PhotoItemViewModel (new StubPhoto (file));
			item.Selected = true;
			viewModel.Photos.Add (item);

			await viewModel.ExportZipCommand.ExecuteAsync (null);

			Assert.IsFalse (File.Exists (Path.Combine (destDir, "trip.zip")));
		} finally {
			Directory.Delete (sourceDir, true);
			Directory.Delete (destDir, true);
		}
	}

	static byte[] ZipEntryBytes (string zipPath, string name)
	{
		using var archive = System.IO.Compression.ZipFile.OpenRead (zipPath);
		var entry = archive.GetEntry (name);
		Assert.IsNotNull (entry);
		using var stream = entry.Open ();
		using var memory = new MemoryStream ();
		stream.CopyTo (memory);
		return memory.ToArray ();
	}

	static (int Width, int Height) JpegSize (byte[] data)
	{
		int i = 2;
		while (i + 9 < data.Length) {
			if (data[i] != 0xFF) {
				i++;
				continue;
			}
			byte marker = data[i + 1];
			if (marker == 0xC0 || marker == 0xC1 || marker == 0xC2) {
				int height = (data[i + 5] << 8) | data[i + 6];
				int width = (data[i + 7] << 8) | data[i + 8];
				return (width, height);
			}
			if (marker == 0xD8 || marker == 0xD9 || marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7)) {
				i += 2;
				continue;
			}
			int length = (data[i + 2] << 8) | data[i + 3];
			if (length < 2)
				break;
			i += 2 + length;
		}
		return (0, 0);
	}

	[UiTestMethod]
	public async Task ExportZipCommand_LeavesTheFolderAloneWhenTheNameIsNotAZip ()
	{
		string sourceDir = Path.Combine (Path.GetTempPath (), "fspot-vm-zip-src-" + Guid.NewGuid ().ToString ("N"));
		string destDir = Path.Combine (Path.GetTempPath (), "fspot-vm-zip-dst-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (sourceDir);
		Directory.CreateDirectory (destDir);
		string file = Path.Combine (sourceDir, "shot.jpg");
		File.WriteAllText (file, "photo");
		try {
			var shell = new RecordingShell { ExportFolder = destDir, PromptResult = "notes.txt" };
			var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
			var item = new PhotoItemViewModel (new StubPhoto (file));
			item.Selected = true;
			viewModel.Photos.Add (item);

			await viewModel.ExportZipCommand.ExecuteAsync (null);

			Assert.IsFalse (File.Exists (Path.Combine (destDir, "notes.txt")));
			Assert.IsEmpty (Directory.EnumerateFileSystemEntries (destDir).ToList ());
		} finally {
			Directory.Delete (sourceDir, true);
			if (Directory.Exists (destDir))
				Directory.Delete (destDir, true);
		}
	}

	[UiTestMethod]
	public async Task ExportZipCommand_KeepsTheArchiveInsideTheChosenFolder ()
	{
		string sourceDir = Path.Combine (Path.GetTempPath (), "fspot-vm-zip-src-" + Guid.NewGuid ().ToString ("N"));
		string destDir = Path.Combine (Path.GetTempPath (), "fspot-vm-zip-dst-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (sourceDir);
		string file = Path.Combine (sourceDir, "shot.jpg");
		File.WriteAllText (file, "photo");
		string escaped = Path.GetFullPath (Path.Combine (destDir, "..", "outside.zip"));
		try {
			var shell = new RecordingShell { ExportFolder = destDir, PromptResult = "../outside.zip" };
			var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
			var item = new PhotoItemViewModel (new StubPhoto (file));
			item.Selected = true;
			viewModel.Photos.Add (item);

			await viewModel.ExportZipCommand.ExecuteAsync (null);

			Assert.IsFalse (File.Exists (escaped));
		} finally {
			Directory.Delete (sourceDir, true);
			if (Directory.Exists (destDir))
				Directory.Delete (destDir, true);
			if (File.Exists (escaped))
				File.Delete (escaped);
		}
	}

	[UiTestMethod]
	public async Task ExportZipCommand_RefusesANameTheFileSystemRejects ()
	{
		string sourceDir = Path.Combine (Path.GetTempPath (), "fspot-vm-zip-src-" + Guid.NewGuid ().ToString ("N"));
		string destDir = Path.Combine (Path.GetTempPath (), "fspot-vm-zip-dst-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (sourceDir);
		Directory.CreateDirectory (destDir);
		string file = Path.Combine (sourceDir, "shot.jpg");
		File.WriteAllText (file, "photo");
		try {
			var shell = new RecordingShell { ExportFolder = destDir, PromptResult = "sum\0mer.zip" };
			var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
			var item = new PhotoItemViewModel (new StubPhoto (file));
			item.Selected = true;
			viewModel.Photos.Add (item);

			await viewModel.ExportZipCommand.ExecuteAsync (null);

			Assert.IsEmpty (Directory.GetFiles (destDir));
		} finally {
			Directory.Delete (sourceDir, true);
			if (Directory.Exists (destDir))
				Directory.Delete (destDir, true);
		}
	}

	[TestMethod]
	[OSCondition (ConditionMode.Exclude, OperatingSystems.Windows)]
	public async Task ExportZipCommand_RefusesANameThatIsALinkOutsideTheFolder ()
	{
		string sourceDir = Path.Combine (Path.GetTempPath (), "fspot-vm-zip-src-" + Guid.NewGuid ().ToString ("N"));
		string destDir = Path.Combine (Path.GetTempPath (), "fspot-vm-zip-dst-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (sourceDir);
		Directory.CreateDirectory (destDir);
		string file = Path.Combine (sourceDir, "shot.jpg");
		string outside = Path.Combine (sourceDir, "outside.zip");
		File.WriteAllText (file, "photo");
		File.WriteAllText (outside, "keep");
		File.CreateSymbolicLink (Path.Combine (destDir, "out.zip"), outside);
		try {
			var shell = new RecordingShell { ExportFolder = destDir, PromptResult = "out.zip" };
			var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
			var item = new PhotoItemViewModel (new StubPhoto (file));
			item.Selected = true;
			viewModel.Photos.Add (item);

			await viewModel.ExportZipCommand.ExecuteAsync (null);

			Assert.AreEqual ("keep", File.ReadAllText (outside));
		} finally {
			Directory.Delete (sourceDir, true);
			if (Directory.Exists (destDir))
				Directory.Delete (destDir, true);
		}
	}

	[UiTestMethod]
	public void ExportGalleryCommand_WritesTheSelectionIntoTheNamedFolder ()
	{
		string sourceDir = Path.Combine (Path.GetTempPath (), "fspot-vm-gal-src-" + Guid.NewGuid ().ToString ("N"));
		string destDir = Path.Combine (Path.GetTempPath (), "fspot-vm-gal-dst-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (sourceDir);
		string file = Path.Combine (sourceDir, "shot.jpg");
		File.WriteAllText (file, "photo");
		try {
			var shell = new RecordingShell { ExportFolder = destDir, PromptResult = "Trip" };
			var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
			var item = new PhotoItemViewModel (new StubPhoto (file));
			item.Selected = true;
			viewModel.Photos.Add (item);

			viewModel.ExportGalleryCommand.Execute (null);

			Assert.AreEqual ("photo", File.ReadAllText (Path.Combine (destDir, "Trip", "hq", "img-1.jpg")));
			Assert.Contains ("img-1.html", File.ReadAllText (Path.Combine (destDir, "Trip", "index.html")));
		} finally {
			Directory.Delete (sourceDir, true);
			if (Directory.Exists (destDir))
				Directory.Delete (destDir, true);
		}
	}

	[UiTestMethod]
	public void ExportGalleryCommand_KeepsTheGalleryInsideTheChosenFolder ()
	{
		string sourceDir = Path.Combine (Path.GetTempPath (), "fspot-vm-gal-src-" + Guid.NewGuid ().ToString ("N"));
		string destDir = Path.Combine (Path.GetTempPath (), "fspot-vm-gal-dst-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (sourceDir);
		string file = Path.Combine (sourceDir, "shot.jpg");
		File.WriteAllText (file, "photo");
		string outside = Path.GetFullPath (Path.Combine (destDir, "..", "out"));
		try {
			var shell = new RecordingShell { ExportFolder = destDir, PromptResult = "../out" };
			var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
			var item = new PhotoItemViewModel (new StubPhoto (file));
			item.Selected = true;
			viewModel.Photos.Add (item);

			viewModel.ExportGalleryCommand.Execute (null);

			Assert.IsFalse (Directory.Exists (outside));
			Assert.IsFalse (Directory.Exists (Path.Combine (destDir, "out")));
		} finally {
			Directory.Delete (sourceDir, true);
			if (Directory.Exists (destDir))
				Directory.Delete (destDir, true);
			if (Directory.Exists (outside))
				Directory.Delete (outside, true);
		}
	}

	[UiTestMethod]
	public void ReassignToNewImportRollCommand_DoesNothingWhenNothingIsSelected ()
	{
		string root = Path.Combine (Path.GetTempPath (), "fspot-vm-roll-empty-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (root);
		string previousBase = FSpot.Settings.FSpotConfiguration.BaseDirectory;
		Db? database = null;
		try {
			EnsureMergeServices ();
			var images = TinyIoC.TinyIoCContainer.Current.Resolve<FSpot.Imaging.IImageFileFactory> ();
			var thumbs = TinyIoC.TinyIoCContainer.Current.Resolve<FSpot.Thumbnail.IThumbnailService> ();
			database = new Db (images, thumbs, new MergeUpdater ());
			database.Init (Path.Combine (root, FSpot.Settings.FSpotConfiguration.DatabaseName), true);
			FSpot.Settings.FSpotConfiguration.BaseDirectory = root;
			var viewModel = new MainWindowViewModel (database, new RecordingShell (), new NullThumbnails ());

			Assert.IsFalse (viewModel.ReassignToNewImportRollCommand.CanExecute (null));
			viewModel.ReassignToNewImportRollCommand.Execute (null);

			Assert.AreEqual (0, RollCount (database));
		} finally {
			database?.Dispose ();
			FSpot.Settings.FSpotConfiguration.BaseDirectory = previousBase;
			if (Directory.Exists (root))
				Directory.Delete (root, true);
		}
	}

	[UiTestMethod]
	public void ReassignToNewImportRollCommand_DatesTheRollFromTheNewestSelectedPhoto ()
	{
		string root = Path.Combine (Path.GetTempPath (), "fspot-vm-roll-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (root);
		string previousBase = FSpot.Settings.FSpotConfiguration.BaseDirectory;
		Db? database = null;
		try {
			EnsureMergeServices ();
			string sample = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
			var images = TinyIoC.TinyIoCContainer.Current.Resolve<FSpot.Imaging.IImageFileFactory> ();
			var thumbs = TinyIoC.TinyIoCContainer.Current.Resolve<FSpot.Thumbnail.IThumbnailService> ();
			database = new Db (images, thumbs, new MergeUpdater ());
			database.Init (Path.Combine (root, FSpot.Settings.FSpotConfiguration.DatabaseName), true);
			FSpot.Settings.FSpotConfiguration.BaseDirectory = root;
			Photo older = RollPhoto (database, sample, root, "older.jpg", new DateTime (2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
			Photo newer = RollPhoto (database, sample, root, "newer.jpg", new DateTime (2020, 6, 1, 12, 0, 0, DateTimeKind.Utc));
			Photo aside = RollPhoto (database, sample, root, "aside.jpg", new DateTime (2019, 1, 1, 0, 0, 0, DateTimeKind.Utc));
			uint asideRoll = aside.RollId;
			DateTime newest = newer.Time;
			var viewModel = new MainWindowViewModel (database, new RecordingShell (), new NullThumbnails ());
			var olderItem = new PhotoItemViewModel (older);
			olderItem.Selected = true;
			var newerItem = new PhotoItemViewModel (newer);
			newerItem.Selected = true;
			viewModel.Photos.Add (olderItem);
			viewModel.Photos.Add (newerItem);
			viewModel.Photos.Add (new PhotoItemViewModel (aside));

			viewModel.ReassignToNewImportRollCommand.Execute (null);

			Assert.AreEqual (newer.RollId, older.RollId);
			Assert.AreNotEqual (asideRoll, newer.RollId);
			Assert.AreEqual (asideRoll, aside.RollId);
			Assert.AreEqual (newest, database.Rolls.Get (newer.RollId).Time);
			uint rollId = newer.RollId;
			uint olderId = older.Id;
			uint newerId = newer.Id;
			uint asideId = aside.Id;
			database.Dispose ();
			var again = new Db (images, thumbs, new MergeUpdater ());
			again.Init (Path.Combine (root, FSpot.Settings.FSpotConfiguration.DatabaseName), false);
			database = again;
			Assert.AreEqual (rollId, database.Photos.Get (olderId).RollId);
			Assert.AreEqual (rollId, database.Photos.Get (newerId).RollId);
			Assert.AreEqual (asideRoll, database.Photos.Get (asideId).RollId);
			Assert.AreEqual (newest, database.Rolls.Get (rollId).Time);
		} finally {
			database?.Dispose ();
			FSpot.Settings.FSpotConfiguration.BaseDirectory = previousBase;
			if (Directory.Exists (root))
				Directory.Delete (root, true);
		}
	}

	static Photo RollPhoto (Db database, string sample, string root, string name, DateTime time)
	{
		string path = Path.Combine (root, name);
		File.Copy (sample, path);
		Photo created = database.Photos.CreateFrom (new FilePhoto (SafeUri.FromPath (path)), true, database.Rolls.Create ().Id);
		created.Time = time;
		database.Photos.Commit (created);
		return database.Photos.Get (created.Id);
	}

	static int RollCount (Db database)
	{
		using var reader = database.Database.Query ("SELECT COUNT(*) FROM rolls");
		reader.Read ();
		return Convert.ToInt32 (reader[0]);
	}

	[UiTestMethod]
	public void MergeCatalogCommand_WarnsWhenThereIsNoCatalog ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());

		viewModel.MergeCatalogCommand.Execute (null);

		Assert.AreEqual (Strings.MergeDb, shell.WarningTitle);
		Assert.AreEqual (Strings.OpenACatalogBeforeMerging, shell.WarningMessage);
		Assert.IsFalse (shell.ConfirmCalled);
	}

	[UiTestMethod]
	public void MergeCatalogCommand_ShowsTheImportedTagAndFolder ()
	{
		string root = Path.Combine (Path.GetTempPath (), "fspot-vm-merge-" + Guid.NewGuid ().ToString ("N"));
		string destDir = Path.Combine (root, "dest");
		string sourceDir = Path.Combine (root, "source");
		string library = Path.Combine (root, "library");
		Directory.CreateDirectory (destDir);
		Directory.CreateDirectory (sourceDir);
		Directory.CreateDirectory (library);
		string previousBase = FSpot.Settings.FSpotConfiguration.BaseDirectory;
		SafeUri previousLibrary = FSpot.Settings.FSpotConfiguration.PhotoUri;
		Db? destination = null;
		Db? source = null;
		try {
			EnsureMergeServices ();
			string sample = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
			string shot = Path.Combine (sourceDir, "shot.jpg");
			File.Copy (sample, shot);
			var images = TinyIoC.TinyIoCContainer.Current.Resolve<FSpot.Imaging.IImageFileFactory> ();
			var thumbs = TinyIoC.TinyIoCContainer.Current.Resolve<FSpot.Thumbnail.IThumbnailService> ();
			destination = new Db (images, thumbs, new MergeUpdater ());
			destination.Init (Path.Combine (destDir, FSpot.Settings.FSpotConfiguration.DatabaseName), true);
			source = new Db (images, thumbs, new MergeUpdater ());
			source.Init (Path.Combine (sourceDir, "photos.db"), true);
			uint roll = source.Rolls.Create (new DateTime (2019, 3, 4, 0, 0, 0, DateTimeKind.Utc)).Id;
			Photo photo = source.Photos.CreateFrom (new FilePhoto (new SafeUri (shot)), true, roll);
			photo.AddTag (source.Tags.CreateTag (source.Tags.RootCategory, "Beach", false));
			source.Photos.Commit (photo);
			source.Dispose ();
			source = null;

			FSpot.Settings.FSpotConfiguration.BaseDirectory = destDir;
			FSpot.Settings.FSpotConfiguration.PhotoUri = SafeUri.FromPath (library);
			var shell = new RecordingShell {
				ConfirmResult = true,
				PickedFiles = new[] { Path.Combine (sourceDir, "photos.db") }
			};
			var viewModel = new MainWindowViewModel (destination, shell, new NullThumbnails ());
			viewModel.LoadCatalog ();
			Assert.IsFalse (HasTag (viewModel.Tags, "Beach"));

			viewModel.MergeCatalogCommand.Execute (null);

			Assert.IsTrue (HasTag (viewModel.Tags, "Beach"));
			Assert.IsNotEmpty (viewModel.Folders);
		} finally {
			source?.Dispose ();
			destination?.Dispose ();
			FSpot.Settings.FSpotConfiguration.BaseDirectory = previousBase;
			FSpot.Settings.FSpotConfiguration.PhotoUri = previousLibrary;
			if (Directory.Exists (root))
				Directory.Delete (root, true);
		}
	}

	[UiTestMethod]
	public void DevelopUfrawCommand_WarnsWhenThereIsNoCatalog ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		var item = new PhotoItemViewModel (new StubPhoto ("/tmp/a.cr2"));
		item.Selected = true;
		viewModel.Photos.Add (item);

		viewModel.DevelopUfrawCommand.Execute (null);

		Assert.AreEqual (Strings.DevelopInUFRaw2, shell.WarningTitle);
		Assert.AreEqual (Strings.OpenACatalogBeforeDeveloping, shell.WarningMessage);
	}

	[UiTestMethod]
	public void DevelopUfrawCommand_WarnsWhenUfrawIsNotInstalled ()
	{
		string root = Path.Combine (Path.GetTempPath (), "fspot-vm-ufraw-" + Guid.NewGuid ().ToString ("N"));
		string destDir = Path.Combine (root, "dest");
		Directory.CreateDirectory (destDir);
		string previousBase = FSpot.Settings.FSpotConfiguration.BaseDirectory;
		Db? database = null;
		try {
			EnsureMergeServices ();
			string rawPath = Path.Combine (destDir, "shot.cr2");
			File.WriteAllText (rawPath, "raw");
			var images = TinyIoC.TinyIoCContainer.Current.Resolve<FSpot.Imaging.IImageFileFactory> ();
			var thumbs = TinyIoC.TinyIoCContainer.Current.Resolve<FSpot.Thumbnail.IThumbnailService> ();
			database = new Db (images, thumbs, new MergeUpdater ());
			database.Init (Path.Combine (destDir, FSpot.Settings.FSpotConfiguration.DatabaseName), true);
			SafeUri uri = SafeUri.FromPath (rawPath);
			var photo = new Photo (images, thumbs, 1, DateTimeUtil.FromDateTime (new DateTime (2019, 3, 4, 0, 0, 0, DateTimeKind.Utc)));
			photo.AddVersionUnsafely (Photo.OriginalVersionId, uri.GetBaseUri (), uri.GetFilename (), string.Empty, "Original", true);

			FSpot.Settings.FSpotConfiguration.BaseDirectory = destDir;
			var shell = new RecordingShell ();
			var viewModel = new MainWindowViewModel (database, shell, new NullThumbnails ());
			var item = new PhotoItemViewModel (photo);
			item.Selected = true;
			viewModel.Photos.Add (item);

			viewModel.DevelopUfrawCommand.Execute (null);

			Assert.AreEqual (Strings.DevelopInUFRaw2, shell.WarningTitle);
			Assert.IsFalse (string.IsNullOrEmpty (shell.WarningMessage));
			Assert.AreEqual ((uint)Photo.OriginalVersionId, photo.DefaultVersionId);
		} finally {
			database?.Dispose ();
			FSpot.Settings.FSpotConfiguration.BaseDirectory = previousBase;
			if (Directory.Exists (root))
				Directory.Delete (root, true);
		}
	}

	[UiTestMethod]
	public void BatchDevelopCommand_WarnsWhenThereIsNoCatalog ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		var item = new PhotoItemViewModel (new StubPhoto ("/tmp/a.cr2"));
		item.Selected = true;
		viewModel.Photos.Add (item);

		viewModel.BatchDevelopCommand.Execute (null);

		Assert.AreEqual (Strings.BatchDevelop, shell.WarningTitle);
		Assert.AreEqual (Strings.OpenACatalogBeforeDeveloping, shell.WarningMessage);
	}

	[UiTestMethod]
	public void MergeRawJpegCommand_WarnsWhenThereIsNoCatalog ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());

		viewModel.MergeRawJpegCommand.Execute (null);

		Assert.AreEqual (Strings.MergeRaw, shell.WarningTitle);
		Assert.AreEqual (Strings.OpenACatalogBeforeMerging, shell.WarningMessage);
		Assert.IsFalse (shell.ConfirmCalled);
	}

	[UiTestMethod]
	public void ChangePathCommand_WarnsWhenThereIsNoCatalog ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());

		viewModel.ChangePathCommand.Execute (null);

		Assert.AreEqual (Strings.ChangePhotosDirectory, shell.WarningTitle);
		Assert.AreEqual (Strings.OpenACatalogBeforeChangingPaths, shell.WarningMessage);
	}

	[UiTestMethod]
	public void ChangePathCommand_LeavesPhotosAloneWhenTheOldBaseIsOnlyASlash ()
	{
		string root = Path.Combine (Path.GetTempPath (), "fspot-vm-path-" + Guid.NewGuid ().ToString ("N"));
		string destDir = Path.Combine (root, "dest");
		string library = Path.Combine (root, "library");
		Directory.CreateDirectory (destDir);
		Directory.CreateDirectory (library);
		string previousBase = FSpot.Settings.FSpotConfiguration.BaseDirectory;
		SafeUri previousLibrary = FSpot.Settings.FSpotConfiguration.PhotoUri;
		Db? database = null;
		try {
			EnsureMergeServices ();
			string shot = Path.Combine (destDir, "shot.jpg");
			File.WriteAllText (shot, "photo");
			var images = TinyIoC.TinyIoCContainer.Current.Resolve<FSpot.Imaging.IImageFileFactory> ();
			var thumbs = TinyIoC.TinyIoCContainer.Current.Resolve<FSpot.Thumbnail.IThumbnailService> ();
			database = new Db (images, thumbs, new MergeUpdater ());
			database.Init (Path.Combine (destDir, FSpot.Settings.FSpotConfiguration.DatabaseName), true);
			uint roll = database.Rolls.Create (new DateTime (2019, 3, 4, 0, 0, 0, DateTimeKind.Utc)).Id;
			database.Photos.CreateFrom (new FilePhoto (new SafeUri (shot)), true, roll);
			string before = database.Photos.Query ("SELECT * FROM photos")[0].DefaultVersion.Uri.AbsolutePath;

			FSpot.Settings.FSpotConfiguration.BaseDirectory = destDir;
			FSpot.Settings.FSpotConfiguration.PhotoUri = SafeUri.FromPath (library);
			var shell = new RecordingShell { PromptResult = "/" };
			var viewModel = new MainWindowViewModel (database, shell, new NullThumbnails ());

			viewModel.ChangePathCommand.Execute (null);

			string after = database.Photos.Query ("SELECT * FROM photos")[0].DefaultVersion.Uri.AbsolutePath;
			Assert.AreEqual (before, after);
			Assert.AreEqual (Strings.CouldNotFindAnOldBasePath, shell.WarningMessage);
			Assert.Contains (Strings.OldBasePathURIWas, shell.PromptText ?? string.Empty);
			Assert.Contains (library, shell.PromptText ?? string.Empty);
			if (PhotoPathChange.GuessOldBase (new[] { before }) == "/")
				Assert.AreEqual (string.Empty, shell.PromptDefault);
		} finally {
			database?.Dispose ();
			FSpot.Settings.FSpotConfiguration.BaseDirectory = previousBase;
			FSpot.Settings.FSpotConfiguration.PhotoUri = previousLibrary;
			if (Directory.Exists (root))
				Directory.Delete (root, true);
		}
	}

	[UiTestMethod]
	public void FolderShare_UsesCatalogCountAfterSeed ()
	{
		var viewModel = NewViewModel ();
		viewModel.SeedCatalog (
			new StubPhoto ("/a.jpg", new DateTime (2026, 1, 1)),
			new StubPhoto ("/b.jpg", new DateTime (2026, 1, 2)),
			new StubPhoto ("/c.jpg", new DateTime (2026, 1, 3)),
			new StubPhoto ("/d.jpg", new DateTime (2026, 1, 4)));
		var node = new FolderNode (new FolderTreeNode ("photos", new SafeUri ("file:///photos/", true), 2, []), true, viewModel.CatalogCount);
		Assert.AreEqual (4, viewModel.CatalogCount);
		Assert.AreEqual (0.5, node.Share, 0.0001);
	}

	[UiTestMethod]
	public void CreateRenameDeleteVersionCommands_MutateTheFocusedPhoto ()
	{
		var shell = new RecordingShell { PromptResult = "Edited" };
		var photo = CatalogPhoto ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		viewModel.SeedCatalog (photo);
		viewModel.Photos[0].Selected = true;
		viewModel.SelectedPhoto = viewModel.Photos[0];

		viewModel.CreateVersionCommand.Execute (null);
		Assert.HasCount (2, photo.Versions);
		Assert.AreEqual ("Edited", photo.DefaultVersion.Name);

		shell.PromptResult = "Final";
		viewModel.RenameVersionCommand.Execute (null);
		Assert.AreEqual ("Final", photo.DefaultVersion.Name);

		shell.ConfirmResult = true;
		viewModel.DeleteVersionCommand.Execute (null);
		Assert.HasCount (1, photo.Versions);
	}

	[UiTestMethod]
	public void AdjustTimeCommand_ShiftsTheFocusedPhoto ()
	{
		var shell = new RecordingShell { Hours = 3 };
		var photo = CatalogPhoto ();
		DateTime original = photo.Time;
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		viewModel.SeedCatalog (photo);
		viewModel.Photos[0].Selected = true;
		viewModel.SelectedPhoto = viewModel.Photos[0];

		viewModel.AdjustTimeCommand.Execute (null);

		Assert.AreEqual (original + TimeSpan.FromHours (3), photo.Time);
	}

	[UiTestMethod]
	public void AdjustTimeCommand_WhenSpacing_AnchorsOnTheChosenTimeNotTheOriginalOne ()
	{
		var first = CatalogPhoto ();
		var second = CatalogPhoto ();
		var anchorTime = new DateTime (2030, 1, 1, 9, 0, 0);
		var shell = new RecordingShell {
			AdjustTimeResult = new TimeAdjustChoice {
				ShiftByOffset = false,
				SpacingMinutes = 15,
				AnchorIndex = 0,
				AnchorTime = anchorTime
			}
		};
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		viewModel.SeedCatalog (first, second);
		viewModel.Photos[0].Selected = true;
		viewModel.Photos[1].Selected = true;

		viewModel.AdjustTimeCommand.Execute (null);

		// Row 0 is the anchor: it takes the dialog's chosen time exactly, not its own old time.
		Assert.AreEqual (anchorTime, first.Time);
		Assert.AreEqual (anchorTime + TimeSpan.FromMinutes (15), second.Time);
	}

	[UiTestMethod]
	public void RefreshThumbnailCommand_ClearsThePreviewCache ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();
		viewModel.RefreshThumbnailCommand.Execute (null);
		Assert.IsNull (viewModel.Photos[0].Thumbnail);
	}

	[UiTestMethod]
	public void ReparentTagCommand_MovesTheChildUnderTheParent ()
	{
		var people = new Category (null, 1, "People");
		var places = new Category (null, 2, "Places");
		var friends = new Tag (people, 3, "Friends");
		var child = new TagNode (friends);
		var parent = new TagNode (places);
		var viewModel = NewViewModel ();

		viewModel.ReparentTagCommand.Execute (((TagNode Child, TagNode Parent)?) (child, parent));

		Assert.AreSame (places, friends.Category);
	}

	[UiTestMethod]
	public void BeginAndCommitRenameTag_EditsTheNameInPlace ()
	{
		var tag = new Tag (null, 1, "Old");
		var node = new TagNode (tag);
		var viewModel = NewViewModel ();
		viewModel.BeginRenameTagCommand.Execute (node);
		Assert.IsTrue (node.IsRenaming);
		node.Name = "New";
		viewModel.CommitRenameTagCommand.Execute (node);
		Assert.IsFalse (node.IsRenaming);
		Assert.AreEqual ("New", tag.Name);
	}

	[UiTestMethod]
	public void RateFocused_SetsAndClearsTheFocusedPhoto ()
	{
		var viewModel = NewViewModel ();
		var photo = CatalogPhoto ();
		var item = new PhotoItemViewModel (photo);
		viewModel.Photos.Add (item);
		viewModel.SelectedPhoto = item;

		viewModel.RateFocused (4);
		Assert.AreEqual (4u, photo.Rating);
		Assert.AreEqual (4u, viewModel.Info.RatingValue);

		viewModel.RateFocused (0);
		Assert.AreEqual (0u, photo.Rating);
		Assert.AreEqual (0u, viewModel.Info.RatingValue);
	}

	[UiTestMethod]
	public void RatingKey_ReadsDigitsAndSkipsATextBox ()
	{
		Assert.AreEqual (0u, RatingKey.From (Key.D0, KeyModifiers.None, null));
		Assert.AreEqual (5u, RatingKey.From (Key.NumPad5, KeyModifiers.None, null));
		Assert.IsNull (RatingKey.From (Key.D3, KeyModifiers.Control, null));
		Assert.IsNull (RatingKey.From (Key.A, KeyModifiers.None, null));
	}

	[UiTestMethod]
	public void RatingKey_AlsoAcceptsAltPlusDigitLikeGtk ()
	{
		AvaloniaApp.Ensure ();
		Assert.AreEqual (0u, RatingKey.From (Key.D0, KeyModifiers.Alt, null));
		Assert.AreEqual (5u, RatingKey.From (Key.NumPad5, KeyModifiers.Alt, null));
		Assert.IsNull (RatingKey.From (Key.D3, KeyModifiers.Alt, new TextBox ()));
		Assert.IsNull (RatingKey.From (Key.D3, KeyModifiers.Control | KeyModifiers.Alt, null));
	}

	[UiTestMethod]
	public void CloseWindowCommand_ClosesWithoutQuittingTheApp ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());

		viewModel.CloseWindowCommand.Execute (null);

		Assert.IsTrue (shell.ClosedCalled);
		Assert.IsFalse (shell.QuitCalled);
	}

	[UiTestMethod]
	public void QuitCommand_QuitsRatherThanOnlyClosing ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());

		viewModel.QuitCommand.Execute (null);

		Assert.IsTrue (shell.QuitCalled);
		Assert.IsFalse (shell.ClosedCalled);
	}

	[UiTestMethod]
	public void SaveWindowState_RoundTripsTogglesZoomSidebarWidthAndPage ()
	{
		int originalSidebarPage = LibraryPreferences.SidebarPageLibrary;
		try {
			var viewModel = new MainWindowViewModel (null, new RecordingShell (), new NullThumbnails ());
			viewModel.DisplayToolbar = false;
			viewModel.DisplayDates = false;
			viewModel.DisplayTags = false;
			viewModel.DisplayRatings = false;
			viewModel.ReverseOrder = true;
			viewModel.Zoom = 200;
			viewModel.SidebarLength = new GridLength (300);
			viewModel.SidebarTab = 1;

			viewModel.SaveWindowState ();

			Assert.IsFalse (LibraryPreferences.ShowToolbar);
			Assert.IsFalse (LibraryPreferences.ShowDates);
			Assert.IsFalse (LibraryPreferences.ShowTags);
			Assert.IsFalse (LibraryPreferences.ShowRatings);
			Assert.IsTrue (LibraryPreferences.GroupAdaptorOrderAsc);
			Assert.AreEqual (200.0, LibraryPreferences.Zoom);
			Assert.AreEqual (300, LibraryPreferences.SidebarPosition);
			Assert.AreEqual (1, LibraryPreferences.SidebarPageLibrary);

			// A newly constructed ViewModel (e.g. after relaunching the app)
			// picks all of this back up.
			var restored = new MainWindowViewModel (null, new RecordingShell (), new NullThumbnails ());
			Assert.IsFalse (restored.DisplayToolbar);
			Assert.IsFalse (restored.DisplayDates);
			Assert.IsFalse (restored.DisplayTags);
			Assert.IsFalse (restored.DisplayRatings);
			Assert.IsTrue (restored.ReverseOrder);
			Assert.AreEqual (200.0, restored.Zoom);
			Assert.AreEqual (300.0, restored.SidebarLength.Value);
			Assert.AreEqual (1, restored.SidebarTab);
		} finally {
			// Sibling tests construct a fresh ViewModel and expect the
			// documented defaults, so put every toggle this test flipped
			// back the way it found them.
			LibraryPreferences.ShowToolbar = true;
			LibraryPreferences.ShowDates = true;
			LibraryPreferences.ShowTags = true;
			LibraryPreferences.ShowRatings = true;
			LibraryPreferences.GroupAdaptorOrderAsc = false;
			LibraryPreferences.Zoom = 0.0;
			LibraryPreferences.SidebarPosition = 130;
			LibraryPreferences.SidebarPageLibrary = originalSidebarPage;
		}
	}

	[UiTestMethod]
	public async Task ImportDropped_ReportsRejectedFilesAndImportsTheRest ()
	{
		string root = Path.Combine (Path.GetTempPath (), "fspot-drop-" + Path.GetRandomFileName ());
		Directory.CreateDirectory (root);
		string jpeg = Path.Combine (root, "photo.jpg");
		string notes = Path.Combine (root, "notes.txt");
		await File.WriteAllTextAsync (jpeg, "jpg", TestContext.CancellationToken);
		await File.WriteAllTextAsync (notes, "text", TestContext.CancellationToken);
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		try {
			await viewModel.ImportDroppedAsync (new[] { jpeg, notes, root }, path => path.EndsWith (".jpg"));

			Assert.IsNotNull (shell.WarningMessage);
			Assert.Contains ("notes.txt", shell.WarningMessage);
			Assert.IsNotNull (shell.ImportedPaths);
			Assert.Contains (Path.GetFullPath (jpeg), shell.ImportedPaths);
			Assert.Contains (Path.GetFullPath (root), shell.ImportedPaths);
			Assert.DoesNotContain (Path.GetFullPath (notes), shell.ImportedPaths);
		} finally {
			Directory.Delete (root, true);
		}
	}

	[UiTestMethod]
	public void SetStarCommand_WritesTheRatingThroughRatingChosen ()
	{
		uint? chosen = null;
		var info = new PhotoInfoViewModel ();
		info.RatingChosen += value => chosen = value;
		info.SetStarCommand.Execute (4u);
		Assert.AreEqual (4u, info.RatingValue);
		Assert.AreEqual (4u, chosen);
		Assert.AreEqual ("★★★★☆", info.StarCaption);
		Assert.AreEqual ("★", info.StarButtons[3].Glyph);
		Assert.AreEqual ("☆", info.StarButtons[4].Glyph);
		info.SetStarCommand.Execute (4u);
		Assert.AreEqual (0u, info.RatingValue);
		Assert.AreEqual (0u, chosen);
		Assert.AreEqual ("☆☆☆☆☆", info.StarCaption);
	}

	[UiTestMethod]
	public void EditBarDescription_IsWrittenOntoThePhoto ()
	{
		var viewModel = NewViewModel ();
		var photo = CatalogPhoto ();
		viewModel.SeedCatalog (photo);
		viewModel.SelectPhoto (viewModel.Photos[0]);

		viewModel.Info.Description = "Picnic";

		Assert.AreEqual ("Picnic", photo.Description);
	}

	[UiTestMethod]
	public async Task CopyCommand_UsesThePhotoTheFilmstripFocused ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		var first = new PhotoItemViewModel (new StubPhoto ("/photos/first.jpg"));
		var second = new PhotoItemViewModel (new StubPhoto ("/photos/second.jpg"));
		viewModel.Photos.Add (first);
		viewModel.Photos.Add (second);
		first.Selected = true;
		viewModel.SelectedPhoto = first;

		viewModel.SelectFromFilmstrip (second);
		await viewModel.CopyCommand.ExecuteAsync (null);

		Assert.IsFalse (first.Selected);
		Assert.IsTrue (second.Selected);
		Assert.AreSame (second, viewModel.SelectedPhoto);
		Assert.IsNotNull (shell.Copied);
		Assert.Contains ("second.jpg", shell.Copied[0]);
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void Histogram_IgnoresALoadThatFinishesAfterSelectionMoves ()
	{
		AvaloniaApp.Ensure ();
		var late = new WriteableBitmap (new PixelSize (2, 2), new global::Avalonia.Vector (96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
		var previous = SynchronizationContext.Current;
		SynchronizationContext.SetSynchronizationContext (null);
		try {
			var gate = new GateThumbnails ();
			var viewModel = new MainWindowViewModel (null, new RecordingShell (), gate);
			var first = new PhotoItemViewModel (new StubPhoto ("/photos/first.jpg"));
			var second = new PhotoItemViewModel (new StubPhoto ("/photos/second.jpg"));
			viewModel.Photos.Add (first);
			viewModel.Photos.Add (second);

			viewModel.SelectedPhoto = first;
			Assert.IsTrue (gate.Started.Task.IsCompleted);
			Assert.AreEqual (HistogramRenderer.SourceEdge, gate.Sizes[0]);
			viewModel.SelectedPhoto = second;
			var current = viewModel.Histogram;
			Assert.IsNotNull (current);
			Assert.AreEqual (256, current.PixelSize.Width);
			Assert.AreEqual (128, current.PixelSize.Height);

			gate.Release.SetResult (late);

			Assert.AreSame (current, viewModel.Histogram);
		} finally {
			SynchronizationContext.SetSynchronizationContext (previous);
			late.Dispose ();
		}
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void Histogram_UsesA256FitNotTheGridThumbnail ()
	{
		AvaloniaApp.Ensure ();
		var thumbs = new BitmapThumbnails ();
		var viewModel = new MainWindowViewModel (null, new RecordingShell (), thumbs);
		var item = new PhotoItemViewModel (new StubPhoto ("/photos/one.jpg"));
		item.Thumbnail = new WriteableBitmap (new PixelSize (4, 4), new global::Avalonia.Vector (96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
		viewModel.Photos.Add (item);

		viewModel.SelectedPhoto = item;
		Dispatcher.UIThread.RunJobs ();

		Assert.Contains (HistogramRenderer.SourceEdge, thumbs.Sizes);
		Assert.IsNotNull (viewModel.Histogram);
		Assert.AreEqual (256, viewModel.Histogram.PixelSize.Width);
		Assert.AreEqual (128, viewModel.Histogram.PixelSize.Height);

		viewModel.UiScale = 2;
		Dispatcher.UIThread.RunJobs ();
		Assert.AreEqual (256, viewModel.Histogram.PixelSize.Width);
		Assert.AreEqual (128, viewModel.Histogram.PixelSize.Height);
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void Histogram_FollowsTheEditorPreviewAndClearsItWhenThePhotoChanges ()
	{
		AvaloniaApp.Ensure ();
		var thumbs = new BitmapThumbnails ();
		var viewModel = new MainWindowViewModel (null, new RecordingShell (), thumbs);
		var first = new PhotoItemViewModel (new StubPhoto ("/photos/first.jpg"));
		var second = new PhotoItemViewModel (new StubPhoto ("/photos/second.jpg"));
		viewModel.Photos.Add (first);
		viewModel.Photos.Add (second);
		viewModel.SelectedPhoto = first;
		Dispatcher.UIThread.RunJobs ();
		int loads = thumbs.Sizes.Count;

		// Headless Lock() frees its buffer, so a filled WriteableBitmap reads back empty.
		// CountBgra walks CopyPixels, which this bitmap supplies.
		var preview = new FilledBitmap (8, 8, 255);
		viewModel.EditedPreview = preview;
		Dispatcher.UIThread.RunJobs ();

		Assert.HasCount (loads, thumbs.Sizes);
		Assert.IsNotNull (viewModel.Histogram);
		Assert.AreEqual (256, viewModel.Histogram.PixelSize.Width);
		Assert.AreEqual (128, viewModel.Histogram.PixelSize.Height);
		Assert.AreEqual (255, Bottom (viewModel.Histogram, 255)[2]);

		viewModel.SelectedPhoto = second;
		Dispatcher.UIThread.RunJobs ();

		Assert.IsNull (viewModel.EditedPreview);
		Assert.IsNotNull (viewModel.Histogram);
		byte[] plate = Bottom (viewModel.Histogram, 255);
		Assert.AreEqual (0, plate[2]);
		Assert.AreEqual (255, plate[3]);
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void SidebarHistogram_ScalesUniformlyWithin128 ()
	{
		AvaloniaApp.Ensure ();
		var window = new MainWindow (null) { Width = 900, Height = 600 };
		window.Show ();
		try {
			window.UpdateLayout ();
			var expander = window.GetVisualDescendants ().OfType<Expander> ()
				.Single (control => AutomationProperties.GetAutomationId (control) == AutomationIds.SidebarHistogram);
			var image = expander.Content as Image ?? expander.GetVisualDescendants ().OfType<Image> ().Single ();
			Assert.AreEqual (128, image.MaxHeight);
			Assert.AreEqual (256, image.MaxWidth);
			Assert.AreEqual (Stretch.Uniform, image.Stretch);
			Assert.IsTrue (double.IsNaN (image.Height));
			Assert.AreEqual (BitmapInterpolationMode.None, RenderOptions.GetBitmapInterpolationMode (image));
		} finally {
			window.Close ();
		}
	}

	static byte[] Bottom (Bitmap bitmap, int x)
	{
		int width = bitmap.PixelSize.Width;
		int y = bitmap.PixelSize.Height - 1;
		var pixel = new byte[4];
		unsafe {
			var row = new byte[width * 4];
			fixed (byte* pointer = row)
				bitmap.CopyPixels (new PixelRect (0, y, width, 1), (nint)pointer, row.Length, width * 4);
			pixel[0] = row[x * 4];
			pixel[1] = row[x * 4 + 1];
			pixel[2] = row[x * 4 + 2];
			pixel[3] = row[x * 4 + 3];
		}

		return pixel;
	}

	[UiTestMethod]
	public void InfoVisibility_LoadsTheSavedDateFlag ()
	{
		var saved = InfoFieldVisibility.Load (InfoFieldVisibility.LibraryContext);
		saved.Date = false;
		saved.Size = false;
		saved.Save ();
		try {
			var viewModel = NewViewModel ();
			Assert.IsFalse (viewModel.InfoVisibility.Date);
			Assert.IsFalse (viewModel.InfoVisibility.Size);
			viewModel.ToggleInfoFieldCommand.Execute ("Size");
			Assert.IsTrue (viewModel.InfoVisibility.Size);
			Assert.IsTrue (InfoFieldVisibility.Load (InfoFieldVisibility.LibraryContext).Size);
		} finally {
			var restore = InfoFieldVisibility.Load (InfoFieldVisibility.LibraryContext);
			restore.Date = true;
			restore.Size = true;
			restore.Save ();
		}
	}

	[UiTestMethod]
	public async Task ShowToolsCommand_HidesADisabledEditor ()
	{
		string key = FSpot.Settings.Preferences.ExtensionKey + PhotoEditorCatalog.Desaturate.Id;
		FSpot.Settings.Preferences.Set (key, false);
		try {
			var shell = new RecordingShell ();
			var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
			Assert.DoesNotContain (editor => editor.Id == PhotoEditorCatalog.Desaturate.Id, viewModel.VisibleEditors);

			await viewModel.ShowToolsCommand.ExecuteAsync (null);
			Assert.IsNotNull (shell.ShownAddins);
			var row = shell.ShownAddins.Single (addin => addin.Id == PhotoEditorCatalog.Desaturate.Id);
			Assert.IsFalse (row.Enabled);
			row.Enabled = true;
			Assert.Contains (editor => editor.Id == PhotoEditorCatalog.Desaturate.Id, viewModel.VisibleEditors);
		} finally {
			FSpot.Settings.Preferences.Set (key, true);
		}
	}

	[UiTestMethod]
	public void ActivateFolderCommand_IsTheFindDropTarget ()
	{
		var viewModel = NewViewModel ();
		var folder = new FolderNode (new FolderTreeNode ("photos", new SafeUri ("file:///photos/", true), 2, []), true, 2);
		viewModel.ActivateFolderCommand.Execute (folder);
		Assert.AreSame (folder, viewModel.ActiveFolderFilter);
	}

	[UiTestMethod]
	public void NotYet_ShowsTheUnavailableWarning ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());

		viewModel.NotYetCommand.Execute (null);

		Assert.AreEqual (Strings.ThisFeatureIsNotYetAvailable, shell.WarningMessage);
	}

	static bool HasTag (IEnumerable<TagNode> nodes, string name)
	{
		foreach (TagNode node in nodes) {
			if (node.Name == name)
				return true;
			if (HasTag (node.Children, name))
				return true;
		}
		return false;
	}

	static void EnsureMergeServices ()
	{
		var container = TinyIoC.TinyIoCContainer.Current;
		TryRegister (() => FSpot.FileSystem.ModuleController.RegisterTypes (container));
		TryRegister (() => FSpot.Imaging.ModuleController.RegisterTypes (container));
		TryRegister (() => FSpot.Thumbnail.ModuleController.RegisterTypes (container));
	}

	static void TryRegister (Action register)
	{
		try {
			register ();
		} catch (Exception ex) when (ex is TinyIoC.TinyIoCResolutionException or ArgumentException) {
		}
	}

	static MainWindowViewModel NewViewModel ()
		=> new (null, new NullShell (), new NullThumbnails ());

	static Photo CatalogPhoto ()
	{
		string dest = Path.GetTempFileName () + ".jpg";
		string src = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
		File.Copy (src, dest, true);
		FSpot.Settings.FSpotConfiguration.PhotoUri = new SafeUri (Path.GetTempPath ());
		var photo = new Photo (null, null, 1, DateTimeUtil.FromDateTime (new DateTime (2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)));
		var uri = new SafeUri (dest);
		photo.AddVersionUnsafely (Photo.OriginalVersionId, uri.GetBaseUri (), uri.GetFilename (), string.Empty, "Original", true);
		photo.AllVersionsLoaded = true;
		return photo;
	}

	static MainWindowViewModel ViewModelWithFocusedPhoto ()
	{
		var viewModel = NewViewModel ();
		var item = new PhotoItemViewModel (new StubPhoto ("/a.jpg"));
		viewModel.Photos.Add (item);
		viewModel.SelectedPhoto = item;
		return viewModel;
	}

	sealed class MergeUpdater : FSpot.Database.IUpdaterUI
	{
		public void Show () { }
		public void Pulse () { }
		public void Destroy () { }
	}

	internal class NullShell : IAvaloniaShell
	{
		public virtual void Close () { }
		public virtual void Quit () => Close ();
		public void ToggleFullscreen () { }
		public void FocusFind () { }
		public Task ShowAboutAsync () => Task.CompletedTask;
		public virtual Task ShowWarningAsync (string title, string message) => Task.CompletedTask;
		public Task<bool> ShowImportAsync () => Task.FromResult (false);
		public virtual Task<bool> ShowImportAsync (IReadOnlyList<string> paths) => Task.FromResult (false);
		public virtual Task<string?> PickFolderAsync (string title) => Task.FromResult<string?> (null);
		public virtual Task<string?> PromptAsync (string title, string prompt, string defaultValue) => Task.FromResult<string?> (null);
		public virtual Task<bool> ConfirmAsync (string title, string message, string okCaption) => Task.FromResult (false);
		public virtual Task<(uint Min, uint Max)?> PromptRatingFilterAsync (uint min, uint max) => Task.FromResult<(uint, uint)?> ((3, 5));
		public virtual Task<DateRangeChoice?> PromptDateRangeFilterAsync (DateTime? start, DateTime? end)
			=> Task.FromResult<DateRangeChoice?> (new DateRangeChoice ((new DateTime (2026, 6, 1), new DateTime (2026, 7, 1))));
		public virtual Task<IReadOnlyList<uint>?> PickImportRollsAsync (IReadOnlyList<ImportRollOption> rolls)
			=> Task.FromResult<IReadOnlyList<uint>?> ([1]);
		public virtual Task PrintPhotosAsync (IReadOnlyList<string> paths, PrintPageSetup? setup = null) => Task.CompletedTask;
		public virtual Task CopyPhotosAsync (IReadOnlyList<string> paths) => Task.CompletedTask;
		public virtual Task SetDesktopBackgroundAsync (string path) => Task.CompletedTask;
		public virtual Task<PrintPageSetup?> PromptPageSetupAsync (PrintPageSetup current) => Task.FromResult<PrintPageSetup?> (null);
		public virtual Task ShowExtensionsAsync (IList<AddinStatus> addins) => Task.CompletedTask;
		public virtual Task MailPhotosAsync (IReadOnlyList<string> paths) => Task.CompletedTask;
		public virtual void ShowSlideshow (IReadOnlyList<string> paths, int startIndex) { }
		public virtual void OpenUri (string uri) { }
		public virtual Task<double?> PromptHoursOffsetAsync () => Task.FromResult<double?> (null);
		public virtual Task<TimeAdjustChoice?> PromptAdjustTimeAsync (IReadOnlyList<(string Name, DateTime Time, string Path)> photos)
			=> Task.FromResult<TimeAdjustChoice?> (null);
	}

	sealed class RecordingShell : NullShell, IAvaloniaShell
	{
		public string? WarningTitle { get; private set; }
		public string? WarningMessage { get; private set; }
		public IReadOnlyList<string>? Printed { get; private set; }
		public IReadOnlyList<string>? Mailed { get; private set; }
		public IReadOnlyList<string>? Slideshow { get; private set; }
		public string? ExportFolder { get; set; }
		public string? OpenedUri { get; private set; }
		public IReadOnlyList<string>? Copied { get; private set; }
		public string? BackgroundPath { get; private set; }
		public PrintPageSetup? PageSetupResult { get; set; }
		public bool PrintedWithSetup { get; private set; }
		public bool ExtensionsShown { get; private set; }
		public IList<AddinStatus>? ShownAddins { get; private set; }
		public string? PromptResult { get; set; }
		public bool AskForZipScale { get; set; }
		public ZipScaleChoice? ZipScale { get; set; }
		public IReadOnlyList<string>? PickedFiles { get; set; }
		public bool ConfirmResult { get; set; }
		public bool ConfirmCalled { get; private set; }
		public double? Hours { get; set; }
		public TimeAdjustChoice? AdjustTimeResult { get; set; }
		public IReadOnlyList<string>? ImportedPaths { get; private set; }
		public bool ClosedCalled { get; private set; }
		public bool QuitCalled { get; private set; }

		public override void Close () => ClosedCalled = true;
		public override void Quit () => QuitCalled = true;

		public override Task<bool> ShowImportAsync (IReadOnlyList<string> paths)
		{
			ImportedPaths = paths;
			return Task.FromResult (false);
		}

		public string? PromptText { get; private set; }
		public string? PromptDefault { get; private set; }

		public override Task<string?> PromptAsync (string title, string prompt, string defaultValue)
		{
			PromptText = prompt;
			PromptDefault = defaultValue;
			return Task.FromResult (PromptResult);
		}

		public Task<ZipScaleChoice?> PromptZipScaleAsync ()
			=> Task.FromResult (AskForZipScale ? ZipScale : new ZipScaleChoice (false, ZipScaleChoice.DefaultLongerSide));

		public Task<IReadOnlyList<string>> PickFilesAsync (string title, bool allowMultiple)
			=> Task.FromResult (PickedFiles ?? (IReadOnlyList<string>)Array.Empty<string> ());

		public override Task<bool> ConfirmAsync (string title, string message, string okCaption)
		{
			ConfirmCalled = true;
			return Task.FromResult (ConfirmResult);
		}

		public override Task<double?> PromptHoursOffsetAsync ()
			=> Task.FromResult (Hours);

		public override Task<TimeAdjustChoice?> PromptAdjustTimeAsync (IReadOnlyList<(string Name, DateTime Time, string Path)> photos)
			=> Task.FromResult (AdjustTimeResult ?? (Hours is double hours
				? new TimeAdjustChoice { ShiftByOffset = true, Hours = hours }
				: null));

		public override Task ShowWarningAsync (string title, string message)
		{
			WarningTitle = title;
			WarningMessage = message;
			return Task.CompletedTask;
		}

		public override Task PrintPhotosAsync (IReadOnlyList<string> paths, PrintPageSetup? setup = null)
		{
			Printed = paths.ToList ();
			PrintedWithSetup = setup is not null && setup.Landscape;
			return Task.CompletedTask;
		}

		public override Task CopyPhotosAsync (IReadOnlyList<string> paths)
		{
			Copied = paths.ToList ();
			return Task.CompletedTask;
		}

		public override Task SetDesktopBackgroundAsync (string path)
		{
			BackgroundPath = path;
			return Task.CompletedTask;
		}

		public override Task<PrintPageSetup?> PromptPageSetupAsync (PrintPageSetup current)
			=> Task.FromResult (PageSetupResult);

		public override Task ShowExtensionsAsync (IList<AddinStatus> addins)
		{
			ExtensionsShown = true;
			ShownAddins = addins;
			return Task.CompletedTask;
		}

		public override Task MailPhotosAsync (IReadOnlyList<string> paths)
		{
			Mailed = paths.ToList ();
			return Task.CompletedTask;
		}

		public override void ShowSlideshow (IReadOnlyList<string> paths, int startIndex)
			=> Slideshow = paths.ToList ();

		public override void OpenUri (string uri)
			=> OpenedUri = uri;

		public override Task<string?> PickFolderAsync (string title)
			=> Task.FromResult (ExportFolder);
	}

	sealed class RecordingThumbnails : IPhotoThumbnailLoader
	{
		public int LastSize { get; private set; }

		public Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken)
		{
			LastSize = size;
			return Task.FromResult<Bitmap?> (null);
		}
	}

	sealed class CountingThumbnails : IPhotoThumbnailLoader
	{
		public int Calls { get; private set; }

		public Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken)
		{
			Calls++;
			return Task.FromResult<Bitmap?> (null);
		}
	}

	internal sealed class TokenThumbnails : IPhotoThumbnailLoader
	{
		public int Calls => Loads.Count;

		public List<(string Path, CancellationToken Token)> Loads { get; } = new ();

		public Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken)
		{
			Loads.Add ((path, cancellationToken));
			return Task.FromResult<Bitmap?> (null);
		}
	}

	// Headless WriteableBitmap.Lock discards pixels. Histogram counting calls CopyPixels.
	sealed class FilledBitmap : WriteableBitmap
	{
		readonly byte value;

		public FilledBitmap (int width, int height, byte value)
			: base (new PixelSize (width, height), new global::Avalonia.Vector (96, 96), PixelFormat.Bgra8888, global::Avalonia.Platform.AlphaFormat.Premul)
		{
			this.value = value;
		}

		public override void CopyPixels (PixelRect sourceRect, nint buffer, int bufferSize, int stride)
		{
			int rowBytes = sourceRect.Width * 4;
			unsafe {
				var start = (byte*)buffer;
				for (int y = 0; y < sourceRect.Height; y++) {
					var row = start + y * stride;
					for (int i = 0; i < rowBytes; i++)
						row[i] = value;
				}
			}
		}
	}

	sealed class BitmapThumbnails : IPhotoThumbnailLoader
	{
		public List<int> Sizes { get; } = [];

		public Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken)
		{
			Sizes.Add (size);
			var bitmap = new WriteableBitmap (new PixelSize (1, 1), new global::Avalonia.Vector (96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
			return Task.FromResult<Bitmap?> (bitmap);
		}
	}

	sealed class GateThumbnails : IPhotoThumbnailLoader
	{
		public TaskCompletionSource Started { get; } = new (TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource<Bitmap?> Release { get; } = new ();
		public List<int> Sizes { get; } = [];
		int calls;

		public Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken)
		{
			Sizes.Add (size);
			if (calls++ == 0) {
				Started.TrySetResult ();
				// The test completes Release. This method only waits on that gate.
#pragma warning disable VSTHRD003
				return Release.Task;
#pragma warning restore VSTHRD003
			}

			var bitmap = new WriteableBitmap (new PixelSize (8, 8), new global::Avalonia.Vector (96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
			return Task.FromResult<Bitmap?> (bitmap);
		}
	}



	sealed class ThrowingImport : IImportController
	{
		readonly Action onImport;

		public ThrowingImport (Action onImport)
			=> this.onImport = onImport;

		public int PhotosImported => 0;
		public IEnumerable<SafeUri> FailedImports => Array.Empty<SafeUri> ();

		public void DoImport (IDb db, IBrowsableCollection photos, IList<Tag> tagsToAttach, ImportPreferences preferences, IProgress<int> progress, CancellationToken token)
		{
			onImport ();
			throw new InvalidOperationException ("import broke");
		}
	}

	// Preview and histogram loads pass CancellationToken.None. The grid load does not.

	sealed class FrozenImport : IImportController
	{
		readonly Action mutateDuringImport;

		public FrozenImport (Action mutateDuringImport)
			=> this.mutateDuringImport = mutateDuringImport;

		public int Seen { get; private set; }
		public int Enumerated { get; private set; }
		public List<string> Names { get; } = [];
		public int PhotosImported => Enumerated;
		public IEnumerable<SafeUri> FailedImports => Array.Empty<SafeUri> ();

		public void DoImport (IDb db, IBrowsableCollection photos, IList<Tag> tagsToAttach, ImportPreferences preferences, IProgress<int> progress, CancellationToken token)
		{
			Seen = photos.Count;
			mutateDuringImport ();
			foreach (var photo in photos.Items) {
				Enumerated++;
				Names.Add (photo.DefaultVersion.Filename);
			}
		}
	}

	sealed class FailUntilAllowed : IPhotoThumbnailLoader
	{
		public int GridCalls { get; private set; }
		public bool Allow { get; set; }

		public Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken)
		{
			if (cancellationToken == CancellationToken.None)
				return Task.FromResult<Bitmap?> (TinyBitmap ());
			GridCalls++;
			if (!Allow)
				throw new InvalidOperationException ("thumb broke");
			return Task.FromResult<Bitmap?> (TinyBitmap ());
		}
	}

	sealed class CancelSecondOfFirstPhoto : IPhotoThumbnailLoader
	{
		public bool CancelA { get; set; }
		public int BLoads { get; private set; }

		public Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken)
		{
			if (cancellationToken != CancellationToken.None) {
				if (path.EndsWith ("a.jpg", StringComparison.Ordinal)) {
					if (CancelA)
						throw new OperationCanceledException ();
				} else {
					BLoads++;
				}
			}

			return Task.FromResult<Bitmap?> (TinyBitmap ());
		}
	}

	sealed class HoldAThenCount : IPhotoThumbnailLoader
	{
		public bool HoldA { get; set; }
		public int BLoads { get; private set; }
		public TaskCompletionSource<bool> EnteredA { get; } = new (TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource<Bitmap?> ReleaseA { get; } = new (TaskCreationOptions.RunContinuationsAsynchronously);

		public Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken)
		{
			if (cancellationToken == CancellationToken.None)
				return Task.FromResult<Bitmap?> (TinyBitmap ());
			if (path.EndsWith ("b.jpg", StringComparison.Ordinal)) {
				BLoads++;
				return Task.FromResult<Bitmap?> (TinyBitmap ());
			}

			if (HoldA) {
				EnteredA.TrySetResult (true);
				return ReleaseA.Task;
			}

			return Task.FromResult<Bitmap?> (TinyBitmap ());
		}
	}

	static WriteableBitmap TinyBitmap ()
		=> new (new PixelSize (1, 1), new global::Avalonia.Vector (96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
}
