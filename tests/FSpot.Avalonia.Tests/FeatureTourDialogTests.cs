// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;

using FSpot.Avalonia.Dialogs;
using FSpot.Avalonia.Imaging;
using FSpot.Avalonia.Models;
using FSpot.Avalonia.Printing;
using FSpot.Avalonia.Services;
using FSpot.Avalonia.ViewModels;
using FSpot.Cms;
using FSpot.Core;
using FSpot.Database;
using FSpot.Editors;
using FSpot.Imaging;
using FSpot.Resources.Lang;
using FSpot.Thumbnail;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using TinyIoC;

namespace FSpot.Avalonia.Tests;

// Opens the dialogs and runs the editing tools against real files. Copies are taken
// from this machine's pictures when any exist, then from the bundled sample. The
// copies are edited, tagged, and deleted; the originals are not.
[TestClass]
public sealed class FeatureTourDialogTests
{
	[UiTestMethod]
	public async Task Dialogs_OpenChangeAndClose ()
	{
		AvaloniaApp.Ensure ();
		string picture = CopySample ();
		var loader = new LedgerThumbnails ();
		try {
			var rating = new RatingFilterWindow (0, 5);
			rating.Show ();
			rating.SetMin (2);
			rating.SetMax (4);
			Click (rating, AutomationIds.DialogOk);
			Assert.AreEqual ((2u, 4u), rating.Result);

			var dates = new DateRangeWindow (new DateTime (2026, 9, 23), null, null);
			dates.Show ();
			dates.Preset = DateRangePreset.Today;
			Assert.IsTrue (dates.DatesEnabled);
			Click (dates, AutomationIds.DialogOk);
			Assert.IsNotNull (dates.Result);

			var rolls = new ImportRollsWindow ([
				new ImportRollOption (2, new DateTime (2026, 9, 2, 0, 0, 0, DateTimeKind.Utc), 4),
				new ImportRollOption (1, new DateTime (2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), 3)
			]);
			rolls.Show ();
			rolls.Mode = ImportRollMode.Between;
			rolls.Choose (0, 1);
			Assert.IsTrue (rolls.ShowsSecondRoll);
			Assert.AreEqual ("7", rolls.CountText);
			Click (rolls, AutomationIds.DialogOk);
			Assert.HasCount (2, rolls.Result!);

			var version = new VersionNameWindow (VersionNameRequest.Create, string.Empty, ["Original"]);
			version.Show ();
			version.VersionName = "Cropped";
			Assert.IsTrue (version.CanAccept);
			Click (version, AutomationIds.DialogOk);
			Assert.AreEqual ("Cropped", version.Result);

			var root = new Category (null, 1, "Root");
			var people = new Category (root, 2, "People");
			var mom = new Tag (people, 3, "Mom");
			var tags = new TagSelectionWindow (root);
			tags.Show ();
			tags.Select (Walk (tags.Nodes).First (node => node.Tag == mom));
			Click (tags, AutomationIds.DialogOk);
			Assert.AreSame (mom, tags.Result![0]);

			var created = TagWindow.ForCreate (root, people, autoIcon: true);
			created.Show ();
			created.TagName = "Kids";
			Click (created, AutomationIds.DialogOk);
			Assert.AreEqual ("Kids", created.CreateResult?.Name);

			var ratios = new SelectionRatioWindow ([new CropConstraint ("Postcard", 1.5)]);
			ratios.Show ();
			ratios.Close ();

			var extensions = new ExtensionsWindow ([new AddinStatus ("tools/sample", "Sample", true)]);
			extensions.Show ();
			extensions.Close ();

			var progress = ProgressWindow.Create (Strings.Import, 3, Strings.Cancel);
			progress.Show ();
			Assert.IsFalse (progress.Update ("a.jpg"));
			Task<bool> retry = progress.PerformRetrySkipAsync ();
			Click (progress, AutomationIds.ProgressRetry);
			Assert.IsTrue (await retry);
			progress.Close ();

			var adjust = new AdjustTimeWindow (
				[("copied.jpg", new DateTime (2024, 5, 1, 12, 0, 0), picture)], loader);
			adjust.Show ();
			await Drain ();
			Assert.IsGreaterThan (0, loader.Created);
			adjust.DifferenceBox.Text = "01:00:00";
			Click (adjust, AutomationIds.DialogOk);
			await Drain ();
			Assert.AreEqual (0, loader.Live);

			bool mailWas = LibraryPreferences.MailToEnabled;
			var preferences = new PreferencesWindow ();
			preferences.Show ();
			var mail = Find<CheckBox> (preferences, AutomationIds.PreferencesMailEnabled);
			mail.IsChecked = !mailWas;
			preferences.Close ();
			Assert.AreEqual (!mailWas, LibraryPreferences.MailToEnabled);
			LibraryPreferences.MailToEnabled = mailWas;

			var print = new PrintWindow ([
				new PrintPhoto (picture, "copied.jpg", "May 1", "12:00", string.Empty, string.Empty)
			], PrintPageSetup.Letter, listPrinters: false);
			print.Show ();
			await Drain ();
			print.Close ();

			var mailWindow = new MailWindow ([picture], loadTray: false);
			mailWindow.Show ();
			mailWindow.Close ();
		} finally {
			File.Delete (picture);
		}
	}

	[UiTestMethod]
	public async Task Tools_DesaturateAndRotateCopiesFromThisMachine ()
	{
		AvaloniaApp.Ensure ();
		EnsureEditingServices ();
		ImagingServices.Backend = new AvaloniaImagingBackend ();
		string root = Path.Combine (Path.GetTempPath (), "fspot-tools-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (root);
		var sources = SourcePhotos ().ToList ();
		var paths = new List<string> ();
		for (int i = 0; i < sources.Count; i++) {
			string dest = Path.Combine (root, i + Path.GetExtension (sources[i]));
			File.Copy (sources[i], dest);
			paths.Add (dest);
		}

		var database = new Db (
			TinyIoCContainer.Current.Resolve<IImageFileFactory> (),
			TinyIoCContainer.Current.Resolve<IThumbnailService> (),
			new QuietUpdater ());
		database.Init (Path.Combine (root, "photos.db"), true);
		uint roll = database.Rolls.Create ().Id;
		foreach (string path in paths)
			database.Photos.CreateFrom (new FilePhoto (new SafeUri (path)), true, roll);

		var loader = new LedgerThumbnails ();
		var window = new MainWindow (database, loader) { Width = 1100, Height = 720 };
		try {
			window.Show ();
			var viewModel = (MainWindowViewModel)window.DataContext!;
			window.UpdateLayout ();
			await Drain ();
			Assert.IsGreaterThan (0, viewModel.Photos.Count);

			viewModel.SelectedPhoto = viewModel.Photos[0];
			viewModel.SidebarTab = 2;
			window.UpdateLayout ();
			byte[] before = File.ReadAllBytes (paths[0]);
			Press (window, AutomationIds.ToolbarRotateRight);
			await Drain ();
			Assert.IsFalse (before.AsSpan ().SequenceEqual (File.ReadAllBytes (paths[0])));

			if (LcmsProbe.TryUse (PhotoEditorCatalog.Desaturate.Id)) {
				int versions = ((Photo)viewModel.Photos[0].Photo).Versions.Count ();
				Press (window, "Main.Sidebar.Edit.Desaturate");
				await Drain ();
				Assert.IsGreaterThan (versions, ((Photo)viewModel.Photos[0].Photo).Versions.Count ());
				Assert.IsGreaterThan (1, Directory.GetFiles (root, "*.jpg").Length);
			}

			window.Close ();
			Assert.AreEqual (0, loader.Live);
			Assert.IsNull (viewModel.Histogram);
			Assert.IsNull (viewModel.Preview);
		} finally {
			window.Close ();
			database.Dispose ();
			Directory.Delete (root, true);
		}
	}

	[UiTestMethod]
	public async Task Library_RatesTagsRemovesAndPlaysCopies ()
	{
		using var catalog = CopyCatalog.Open (2);
		var window = catalog.Window;
		var viewModel = catalog.ViewModel;
		var kept = catalog.ItemFor (catalog.Paths[0]);
		var removed = catalog.ItemFor (catalog.Paths[1]);

		viewModel.SelectPhoto (kept);
		var stars = Find<ItemsControl> (window, AutomationIds.InfoStars);
		window.UpdateLayout ();
		if (stars.GetVisualDescendants ().OfType<Button> ().Count () >= 4)
			PressStar (stars, 4);
		else
			viewModel.Info.SetStarCommand.Execute (4u);
		Assert.AreEqual (4u, catalog.Photo.Rating);

		viewModel.CreateTagCommand.Execute (null);
		await Drain ();
		var tagDialog = window.OwnedWindows.OfType<TagWindow> ().Single ();
		tagDialog.TagName = "Tour";
		Click (tagDialog, AutomationIds.DialogOk);
		await Drain ();
		var tour = FindNamed (viewModel.Tags, "Tour");
		Assert.IsNotNull (tour);
		viewModel.SelectedTag = tour;
		viewModel.AttachTagCommand.Execute (null);
		Assert.IsTrue (catalog.Photo.Tags.Any (tag => tag.Name == "Tour"));
		Assert.Contains ("Tour", viewModel.Info.Tags);

		viewModel.RemoveTagCommand.Execute (null);
		Assert.IsFalse (catalog.Photo.Tags.Any (tag => tag.Name == "Tour"));

		Press (window, AutomationIds.ToolbarSlideshow);
		await Drain ();
		var slideshow = window.OwnedWindows.OfType<SlideshowWindow> ().Single ();
		Assert.IsTrue (slideshow.IsPlaying);
		string first = slideshow.CurrentPath;
		Press (slideshow, AutomationIds.SlideshowNext);
		Assert.AreNotEqual (first, slideshow.CurrentPath);
		Press (slideshow, AutomationIds.SlideshowClose);
		await Drain ();

		Press (window, AutomationIds.ToolbarFullscreen);
		await Drain ();
		var fullscreen = window.OwnedWindows.OfType<SlideshowWindow> ().Single ();
		Assert.IsFalse (fullscreen.IsPlaying);
		Press (fullscreen, AutomationIds.SlideshowClose);
		await Drain ();

		viewModel.SelectPhoto (removed);
		int before = viewModel.Photos.Count;
		viewModel.RemoveFromCatalogCommand.Execute (null);
		await Confirm (window);
		Assert.AreEqual (before - 1, viewModel.Photos.Count);
		Assert.IsTrue (File.Exists (catalog.Paths[1]));

		viewModel.SelectPhoto (catalog.ItemFor (catalog.Paths[0]));
		viewModel.DeleteFromDriveCommand.Execute (null);
		await Confirm (window);
		Assert.AreEqual (0, viewModel.Photos.Count);
		Assert.IsFalse (File.Exists (catalog.Paths[0]));

		window.Close ();
		Assert.AreEqual (0, catalog.Loader.Live);
	}

	[UiTestMethod]
	public async Task TitledProgressWindow_CancelOnClose ()
	{
		AvaloniaApp.Ensure ();
		var window = new ProgressWindow (Strings.PreparingEmail, 2);
		Task<bool> retry = window.PerformRetrySkipAsync ();
		window.Show ();
		window.Close ();
		Assert.IsTrue (window.StopRequested.IsCancellationRequested);
		Assert.IsFalse (await retry);
	}

	[UiTestMethod]
	public async Task DeleteFromDrive_KeepsThePhotoWhenTheFileCannotBeDeleted ()
	{
		using var catalog = CopyCatalog.Open (1);
		string path = catalog.Paths[0];
		string? directory = Path.GetDirectoryName (path);
		Assert.IsNotNull (directory);
		var viewModel = catalog.ViewModel;
		viewModel.SelectPhoto (catalog.ItemFor (path));
		bool windows = OperatingSystem.IsWindows ();
		FileAttributes previousAttributes = default;
		UnixFileMode previousMode = default;
		string lockedPath = path;
		DirectoryInfo? lockedDir = null;
		if (windows) {
			previousAttributes = File.GetAttributes (path);
			File.SetAttributes (path, previousAttributes | FileAttributes.ReadOnly);
		} else {
			// photos.db lives next to the original file. Locking that folder makes
			// the failure-path commit throw "attempt to write a readonly database".
			string album = Path.Combine (directory, "album");
			Directory.CreateDirectory (album);
			lockedPath = Path.Combine (album, Path.GetFileName (path));
			File.Move (path, lockedPath);
			((Photo)viewModel.SelectedPhoto!.Photo).GetVersion (Photo.OriginalVersionId).Uri = new SafeUri (lockedPath);
			lockedDir = new DirectoryInfo (album);
			previousMode = lockedDir.UnixFileMode;
			lockedDir.UnixFileMode = UnixFileMode.UserRead | UnixFileMode.UserExecute;
		}

		try {
			viewModel.DeleteFromDriveCommand.Execute (null);
			await Confirm (catalog.Window);
			await Drain ();
			var warning = catalog.Window.OwnedWindows.OfType<MessageDialog> ().Single ();
			Assert.AreEqual (Strings.ErrorDeletingPicture, warning.HeadingBlock.Text);
			string body = warning.BodyBlock.Text ?? string.Empty;
			Assert.Contains (lockedPath, body);
			Assert.DoesNotContain ("file://", body);
			Click (warning, AutomationIds.DialogOk);
			await Drain ();
			Assert.AreEqual (1, viewModel.Photos.Count);
			Assert.IsTrue (File.Exists (lockedPath));
		} finally {
			if (windows)
				File.SetAttributes (path, previousAttributes);
			else if (lockedDir is not null)
				lockedDir.UnixFileMode = previousMode;
		}
	}

	[UiTestMethod]
	public async Task DeleteFromDrive_KeepsTheOriginalWhenOnlyTheDerivedFileIsLocked ()
	{
		using var catalog = CopyCatalog.Open (2);
		string original = catalog.Paths[0];
		string other = catalog.Paths[1];
		string? directory = Path.GetDirectoryName (original);
		Assert.IsNotNull (directory);
		string album = Path.Combine (directory, "derived");
		Directory.CreateDirectory (album);
		string derived = Path.Combine (album, "edited.jpg");
		File.Copy (original, derived);

		var viewModel = catalog.ViewModel;
		var kept = catalog.ItemFor (original);
		var photo = (Photo)kept.Photo;
		photo.AddVersion (new SafeUri (album), "edited.jpg", "Edited");
		var also = catalog.ItemFor (other);
		var grid = Find<ListBox> (catalog.Window, AutomationIds.PhotoGrid);
		var selected = grid.SelectedItems!;
		selected.Clear ();
		selected.Add (kept);
		selected.Add (also);
		kept.Selected = true;
		also.Selected = true;
		viewModel.SelectedPhoto = kept;

		bool windows = OperatingSystem.IsWindows ();
		FileAttributes previousAttributes = default;
		UnixFileMode previousMode = default;
		var lockedDir = new DirectoryInfo (album);
		if (windows) {
			previousAttributes = File.GetAttributes (derived);
			File.SetAttributes (derived, previousAttributes | FileAttributes.ReadOnly);
		} else {
			previousMode = lockedDir.UnixFileMode;
			lockedDir.UnixFileMode = UnixFileMode.UserRead | UnixFileMode.UserExecute;
		}

		try {
			viewModel.DeleteFromDriveCommand.Execute (null);
			await Confirm (catalog.Window);
			await Drain ();
			var warning = catalog.Window.OwnedWindows.OfType<MessageDialog> ().Single ();
			string body = warning.BodyBlock.Text ?? string.Empty;
			Assert.Contains (derived, body);
			Assert.DoesNotContain ("file://", body);
			Click (warning, AutomationIds.DialogOk);
			await Drain ();

			Assert.AreEqual (1, viewModel.Photos.Count);
			Assert.IsTrue (File.Exists (original));
			Assert.IsTrue (File.Exists (derived));
			Assert.IsFalse (File.Exists (other));
			Assert.IsNotNull (photo.GetVersion (Photo.OriginalVersionId));
		} finally {
			if (windows)
				File.SetAttributes (derived, previousAttributes);
			else
				lockedDir.UnixFileMode = previousMode;
		}
	}

	[UiTestMethod]
	public async Task Editors_CommitLiveAndSelectionToolsOnACopy ()
	{
		using var catalog = CopyCatalog.Open (1);
		var window = catalog.Window;
		var viewModel = catalog.ViewModel;
		viewModel.SelectPhoto (viewModel.Photos[0]);
		Press (window, AutomationIds.ToolbarEdit);
		await viewModel.ReloadPreviewAsync ();
		Assert.IsNotNull (viewModel.Preview);

		await Commit (window, viewModel, PhotoEditorCatalog.Sepia.Id);
		await Commit (window, viewModel, PhotoEditorCatalog.Flip.Id);
		await Commit (window, viewModel, PhotoEditorCatalog.AutoStretch.Id);
		await Commit (window, viewModel, PhotoEditorCatalog.BW.Id);
		await Commit (window, viewModel, PhotoEditorCatalog.SoftFocus.Id);
		await Commit (window, viewModel, PhotoEditorCatalog.Tilt.Id, adjust: context => context.TiltDegrees = 8);
		await Commit (window, viewModel, PhotoEditorCatalog.Color.Id, adjust: context => context.ColorSaturation = 20);
		await Commit (window, viewModel, PhotoEditorCatalog.Sharpen.Id);
		await Commit (window, viewModel, PhotoEditorCatalog.Resize.Id);
		var area = new FSpot.Imaging.PixelRect (1, 1, 32, 32);
		await ArmSelection (window, viewModel, PhotoEditorCatalog.Crop.Id, area);
		await ArmSelection (window, viewModel, PhotoEditorCatalog.RedEye.Id, area);
		await ArmSelection (window, viewModel, PhotoEditorCatalog.Pixelate.Id, area);
		await ArmSelection (window, viewModel, PhotoEditorCatalog.Blackout.Id, area);

		window.Close ();
		Assert.AreEqual (0, catalog.Loader.Live);
		Assert.IsNull (viewModel.Preview);
		Assert.IsNull (viewModel.EditedPreview);
	}

	[UiTestMethod]
	public async Task Browse_FindsFiltersAndWalksCopies ()
	{
		using var catalog = CopyCatalog.Open (2);
		var window = catalog.Window;
		var viewModel = catalog.ViewModel;
		int filmstrip = FSpot.Settings.Preferences.Get<int> (FSpot.Settings.Preferences.FilmstripOrientation);
		try {
			var first = viewModel.Photos[0];
			var second = viewModel.Photos[1];
			viewModel.SelectPhoto (first);
			Press (window, AutomationIds.ToolbarNext);
			Assert.AreSame (second, viewModel.SelectedPhoto);
			Press (window, AutomationIds.ToolbarPrevious);
			Assert.AreSame (first, viewModel.SelectedPhoto);

			Press (window, AutomationIds.ToolbarEdit);
			await viewModel.ReloadPreviewAsync ();
			window.UpdateLayout ();
			viewModel.ToggleLoupeCommand.Execute (null);
			Assert.IsTrue (viewModel.IsLoupeVisible);
			viewModel.MoveLoupe (20, 20, 640, 480);
			Assert.IsNotNull (viewModel.LoupeSample);
			viewModel.ToggleLoupeCommand.Execute (null);
			Assert.IsFalse (viewModel.IsLoupeVisible);

			bool side = viewModel.FilmstripOnSide;
			viewModel.ToggleFilmstripSideCommand.Execute (null);
			Assert.AreNotEqual (side, viewModel.FilmstripOnSide);
			var strip = Find<Border> (window, AutomationIds.Filmstrip);
			var film = strip.GetVisualDescendants ().OfType<ListBox> ().First ();
			film.SelectedItem = second;
			Assert.AreSame (second, viewModel.SelectedPhoto);
			viewModel.ToggleFilmstripSideCommand.Execute (null);

			var description = Find<TextBox> (window, AutomationIds.PhotoDescription);
			description.Text = "Picnic";
			Assert.AreEqual ("Picnic", catalog.Photo.Description);

			Press (window, AutomationIds.ToolbarBrowse);
			viewModel.ShowFindCommand.Execute (null);
			viewModel.FindText = "Picnic";
			Assert.AreEqual (1, viewModel.Photos.Count);
			viewModel.FindText = "zzzz-no-such-photo";
			Assert.IsEmpty (viewModel.Photos);
			Assert.IsTrue (viewModel.ShowFindBarNoMatches);
			viewModel.ClearVisibleFiltersCommand.Execute (null);
			Assert.AreEqual (2, viewModel.Photos.Count);

			var picnic = viewModel.Photos.First (item => item.Photo.Description == "Picnic");
			viewModel.SelectPhoto (picnic);
			viewModel.CreateTagCommand.Execute (null);
			await Drain ();
			var tagDialog = window.OwnedWindows.OfType<TagWindow> ().Single ();
			tagDialog.TagName = "Places";
			Click (tagDialog, AutomationIds.DialogOk);
			await Drain ();
			var places = FindNamed (viewModel.Tags, "Places");
			Assert.IsNotNull (places);
			viewModel.SelectedTag = places;
			viewModel.AttachTagCommand.Execute (null);
			viewModel.ActivateTagCommand.Execute (places);
			Assert.AreEqual (1, viewModel.Photos.Count);
			Assert.AreEqual ("Picnic", viewModel.Photos[0].Photo.Description);
			viewModel.ClearTagFilterCommand.Execute (null);
			Assert.AreEqual (2, viewModel.Photos.Count);

			viewModel.FindUntagged = true;
			Assert.AreEqual (1, viewModel.Photos.Count);
			Assert.AreNotEqual ("Picnic", viewModel.Photos[0].Photo.Description);
			viewModel.FindUntagged = false;

			string other = catalog.ImportAnother ("side");
			viewModel.LoadCatalog ();
			Assert.AreEqual (3, viewModel.Photos.Count);
			var home = PhotoFolder (viewModel.Folders, Path.GetDirectoryName (catalog.Paths[0])!).Leaf;
			var sideFolder = PhotoFolder (viewModel.Folders, Path.GetDirectoryName (other)!).Leaf;
			Assert.IsNotNull (home);
			Assert.IsNotNull (sideFolder);
			viewModel.ActivateFolderCommand.Execute (sideFolder);
			Assert.AreEqual (1, viewModel.Photos.Count);
			viewModel.ActivateFolderCommand.Execute (home);
			Assert.AreEqual (3, viewModel.Photos.Count);
			viewModel.ClearFolderFilterCommand.Execute (null);
			Assert.AreEqual (3, viewModel.Photos.Count);

			var month = viewModel.TimelineBuckets.First (bucket => bucket.Count > 0);
			viewModel.SelectTimelineCommand.Execute (month);
			Assert.AreSame (month, viewModel.SelectedTimeline);
			Assert.AreEqual (month.Count, viewModel.Photos.Count);
			viewModel.SelectTimelineCommand.Execute (month);
			Assert.IsNull (viewModel.SelectedTimeline);

			Press (window, AutomationIds.ToolbarSlideshow);
			await Drain ();
			var slideshow = window.OwnedWindows.OfType<SlideshowWindow> ().Single ();
			Press (slideshow, AutomationIds.SlideshowPlayPause);
			Assert.IsFalse (slideshow.IsPlaying);
			var info = Find<ToggleButton> (slideshow, AutomationIds.SlideshowInfo);
			info.IsChecked = true;
			info.RaiseEvent (new RoutedEventArgs (Button.ClickEvent, info));
			Assert.IsTrue (Find<Control> (slideshow, AutomationIds.SlideshowInfoOverlay).IsVisible);
			string shown = slideshow.CurrentPath;
			byte[] before = File.ReadAllBytes (shown);
			Press (slideshow, AutomationIds.SlideshowRotateRight);
			await Drain ();
			Assert.IsFalse (before.AsSpan ().SequenceEqual (File.ReadAllBytes (shown)));
			Press (slideshow, AutomationIds.SlideshowClose);
			await Drain ();

			viewModel.SelectPhoto (viewModel.Photos[0]);
			DateTime time = catalog.Photo.Time;
			viewModel.AdjustTimeCommand.Execute (null);
			await Drain ();
			var adjust = window.OwnedWindows.OfType<AdjustTimeWindow> ().Single ();
			adjust.DifferenceBox.Text = "01:00:00";
			Click (adjust, AutomationIds.DialogOk);
			await Drain ();
			Assert.AreEqual (time.AddHours (1), catalog.Photo.Time);

			window.Close ();
			Assert.AreEqual (0, catalog.Loader.Live);
		} finally {
			FSpot.Settings.Preferences.Set (FSpot.Settings.Preferences.FilmstripOrientation, filmstrip);
		}
	}

	[UiTestMethod]
	public async Task Versions_CreateRenameSwitchDetachAndDeleteACopy ()
	{
		using var catalog = CopyCatalog.Open (1);
		var window = catalog.Window;
		var viewModel = catalog.ViewModel;
		viewModel.SelectPhoto (viewModel.Photos[0]);
		window.UpdateLayout ();

		await NameVersion (window, viewModel, create: true, "Cropped");
		Assert.AreEqual (2, catalog.Photo.Versions.Count ());
		Assert.AreEqual ("Cropped", catalog.Photo.DefaultVersion.Name);

		SelectVersion (window, Strings.Original);
		Assert.AreEqual (Strings.Original, catalog.Photo.DefaultVersion.Name);
		SelectVersion (window, "Cropped");
		Assert.AreEqual ("Cropped", catalog.Photo.DefaultVersion.Name);

		await NameVersion (window, viewModel, create: false, "Final");
		Assert.AreEqual ("Final", catalog.Photo.DefaultVersion.Name);
		Assert.AreEqual ("Final", viewModel.Info.SelectedVersion);

		await NameVersion (window, viewModel, create: true, "Extra");
		Assert.AreEqual (3, catalog.Photo.Versions.Count ());
		viewModel.DeleteVersionCommand.Execute (null);
		await Confirm (window);
		Assert.AreEqual (2, catalog.Photo.Versions.Count ());
		Assert.IsFalse (catalog.Photo.Versions.Any (version => version.Name == "Extra"));

		SelectVersion (window, "Final");
		var parent = catalog.Photo;
		int photos = viewModel.Photos.Count;
		viewModel.DetachVersionCommand.Execute (null);
		await Confirm (window);
		Assert.AreEqual (photos + 1, viewModel.Photos.Count);
		Assert.IsFalse (parent.Versions.Any (version => version.Name == "Final"));
		Assert.AreEqual (1, parent.Versions.Count ());

		window.Close ();
		Assert.AreEqual (0, catalog.Loader.Live);
	}

	static async Task NameVersion (Window window, MainWindowViewModel viewModel, bool create, string name)
	{
		if (create)
			viewModel.CreateVersionCommand.Execute (null);
		else
			viewModel.RenameVersionCommand.Execute (null);
		await Drain ();
		var dialog = window.OwnedWindows.OfType<VersionNameWindow> ().Single ();
		dialog.VersionName = name;
		Click (dialog, AutomationIds.DialogOk);
		await Drain ();
	}

	static void SelectVersion (Window window, string name)
	{
		var versions = Find<ComboBox> (window, AutomationIds.SidebarInfoVersion);
		versions.SelectedItem = name;
	}

	static async Task ArmSelection (Window window, MainWindowViewModel viewModel, string editorId, FSpot.Imaging.PixelRect area)
	{
		Press (window, AutomationIds.SidebarEditEditor (editorId));
		window.UpdateLayout ();
		await Drain ();
		viewModel.SetEditorSelection (area);
		Assert.IsTrue (viewModel.CommitEditorCommand.CanExecute (null), editorId);
		viewModel.CancelEditorCommand.Execute (null);
		Assert.IsNull (viewModel.EditorSession, editorId);
	}

	static async Task Commit (Window window, MainWindowViewModel viewModel, string editorId, Action<PhotoEditorContext>? adjust = null)
	{
		if (!LcmsProbe.TryUse (editorId))
			return;

		int versions = ((Photo)viewModel.SelectedPhoto!.Photo).Versions.Count ();
		window.UpdateLayout ();
		// Headless saves an empty derived JPEG, so each tool starts from the original file.
		if (viewModel.Info.SelectedVersion != Strings.Original && viewModel.Info.Versions.Contains (Strings.Original))
			SelectVersion (window, Strings.Original);
		Press (window, AutomationIds.SidebarEditEditor (editorId));
		await Drain ();
		if (adjust is not null && viewModel.EditorSession is not null)
			adjust (viewModel.EditorSession.Context);
		if (viewModel.EditorSession is not null) {
			await viewModel.LivePreviewTask;
			if (PhotoEditorSession.HasLivePreview (viewModel.EditorSession.Editor))
				Assert.IsNotNull (viewModel.EditedPreview, editorId);
			Assert.IsTrue (viewModel.CommitEditorCommand.CanExecute (null), editorId);
			viewModel.CommitEditorCommand.Execute (null);
			await Drain ();
			string open = string.Join (" | ", window.OwnedWindows.OfType<MessageDialog> ().Select (dialog => dialog.BodyBlock.Text));
			Assert.IsNull (viewModel.EditorSession, editorId + " " + open);
		}

		Assert.IsGreaterThan (versions, ((Photo)viewModel.SelectedPhoto.Photo).Versions.Count (), editorId);
	}

	static async Task Confirm (Window owner)
	{
		await Drain ();
		var dialog = owner.OwnedWindows.OfType<MessageDialog> ().Single ();
		Click (dialog, AutomationIds.DialogOk);
		await Drain ();
	}

	static void PressStar (ItemsControl stars, uint rating)
	{
		var button = stars.GetVisualDescendants ().OfType<Button> ()
			.First (candidate => candidate.CommandParameter is uint value && value == rating);
		button.Command!.Execute (button.CommandParameter);
	}

	static (FolderNode? Parent, FolderNode? Leaf) PhotoFolder (IEnumerable<FolderNode> nodes, string directory)
	{
		string wanted = Path.GetFullPath (directory).TrimEnd (Path.DirectorySeparatorChar);
		FolderNode? parent = null;
		FolderNode? leaf = null;
		void Walk (IEnumerable<FolderNode> level, FolderNode? from)
		{
			foreach (var node in level) {
				if (SamePath (node.Uri, wanted))
					(parent, leaf) = (from, node);
				Walk (node.Children, node);
			}
		}

		Walk (nodes, null);
		return (parent, leaf);
	}

	static bool SamePath (Hyena.SafeUri uri, string directory)
	{
		string path = uri.AbsolutePath ?? string.Empty;
		if (path.Length == 0)
			return false;
		try {
			path = Path.GetFullPath (path).TrimEnd (Path.DirectorySeparatorChar);
		} catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) {
			return false;
		}

		return string.Equals (path, directory, StringComparison.OrdinalIgnoreCase);
	}

	static TagNode? FindNamed (IEnumerable<TagNode> nodes, string name)
	{
		foreach (var node in nodes) {
			if (node.Tag.Name == name)
				return node;
			var child = FindNamed (node.Children, name);
			if (child is not null)
				return child;
		}

		return null;
	}

	sealed class CopyCatalog : IDisposable
	{
		readonly Db database;
		readonly string root;

		CopyCatalog (Db database, string root, MainWindow window, string[] paths, LedgerThumbnails loader)
		{
			this.database = database;
			this.root = root;
			Window = window;
			Paths = paths;
			Loader = loader;
		}

		public MainWindow Window { get; }
		public MainWindowViewModel ViewModel => (MainWindowViewModel)Window.DataContext!;
		public string[] Paths { get; }
		public LedgerThumbnails Loader { get; }
		public Photo Photo => (Photo)ViewModel.SelectedPhoto!.Photo;

		public static CopyCatalog Open (int copies)
		{
			AvaloniaApp.Ensure ();
			EnsureEditingServices ();
			LcmsNativeLibrary.Register ();
			ImagingServices.Backend = new AvaloniaImagingBackend ();
			string root = Path.Combine (Path.GetTempPath (), "fspot-tour-" + Guid.NewGuid ().ToString ("N"));
			Directory.CreateDirectory (root);
			string sample = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
			var paths = new string[copies];
			for (int i = 0; i < copies; i++) {
				paths[i] = Path.Combine (root, i + ".jpg");
				File.Copy (sample, paths[i]);
			}

			var database = new Db (
				TinyIoCContainer.Current.Resolve<IImageFileFactory> (),
				TinyIoCContainer.Current.Resolve<IThumbnailService> (),
				new QuietUpdater ());
			database.Init (Path.Combine (root, "photos.db"), true);
			uint roll = database.Rolls.Create ().Id;
			foreach (string path in paths)
				database.Photos.CreateFrom (new FilePhoto (new SafeUri (path)), true, roll);

			var loader = new LedgerThumbnails ();
			var window = new MainWindow (database, loader) { Width = 1100, Height = 720 };
			window.Show ();
			window.UpdateLayout ();
			return new CopyCatalog (database, root, window, paths, loader);
		}

		public PhotoItemViewModel ItemFor (string path)
			=> ViewModel.Photos.First (item => item.Path == path);

		public string ImportAnother (string childDirectory)
		{
			string dir = Path.Combine (root, childDirectory);
			Directory.CreateDirectory (dir);
			string path = Path.Combine (dir, "other.jpg");
			File.Copy (Paths[0], path);
			uint roll = database.Rolls.Create ().Id;
			database.Photos.CreateFrom (new FilePhoto (new SafeUri (path)), true, roll);
			return path;
		}

		public void Dispose ()
		{
			if (Window.IsVisible)
				Window.Close ();
			database.Dispose ();
			Directory.Delete (root, true);
		}
	}

	static void EnsureEditingServices ()
	{
		var container = TinyIoCContainer.Current;
		TryRegister (() => FSpot.FileSystem.ModuleController.RegisterTypes (container));
		TryRegister (() => FSpot.Imaging.ModuleController.RegisterTypes (container));
		TryRegister (() => FSpot.Thumbnail.ModuleController.RegisterTypes (container));
	}

	static IEnumerable<TagNode> Walk (IEnumerable<TagNode> nodes)
	{
		foreach (var node in nodes) {
			yield return node;
			foreach (var child in Walk (node.Children))
				yield return child;
		}
	}

	static void TryRegister (Action register)
	{
		try {
			register ();
		} catch (Exception ex) when (ex is TinyIoCResolutionException or ArgumentException) {
		}
	}

	static IEnumerable<string> SourcePhotos ()
	{
		yield return Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
		foreach (string folder in new[] {
			Environment.GetFolderPath (Environment.SpecialFolder.MyPictures),
			Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), "Photos")
		}) {
			if (!Directory.Exists (folder))
				continue;
			string? found = null;
			try {
				found = Directory.EnumerateFiles (folder, "*.jpg").FirstOrDefault ()
					?? Directory.EnumerateDirectories (folder).SelectMany (dir => {
						try {
							return Directory.EnumerateFiles (dir, "*.jpg");
						} catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
							return [];
						}
					}).FirstOrDefault ();
			} catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
			}
			if (found is not null)
				yield return found;
		}
	}

	static string CopySample ()
	{
		string source = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
		string dest = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ("N") + ".jpg");
		File.Copy (source, dest);
		return dest;
	}

	static async Task Drain ()
	{
		await Dispatcher.UIThread.InvokeAsync (() => { }, DispatcherPriority.Background);
		await Dispatcher.UIThread.InvokeAsync (() => { }, DispatcherPriority.Background);
	}

	static void Press (Window window, string automationId)
	{
		var button = window.GetVisualDescendants ().OfType<Button> ()
			.First (candidate => AutomationProperties.GetAutomationId (candidate) == automationId);
		if (button.Command?.CanExecute (button.CommandParameter) == true)
			button.Command.Execute (button.CommandParameter);
		else
			button.RaiseEvent (new RoutedEventArgs (Button.ClickEvent, button));
	}

	static void Click (Window window, string automationId)
	{
		var button = window.GetVisualDescendants ().OfType<Button> ()
			.First (candidate => AutomationProperties.GetAutomationId (candidate) == automationId);
		button.RaiseEvent (new RoutedEventArgs (Button.ClickEvent, button));
	}

	static T Find<T> (Window window, string automationId) where T : Control
		=> window.GetVisualDescendants ().OfType<T> ()
			.First (control => AutomationProperties.GetAutomationId (control) == automationId);

	sealed class QuietUpdater : IUpdaterUI
	{
		public void Show () { }
		public void Pulse () { }
		public void Destroy () { }
	}

	sealed class LedgerThumbnails : IPhotoThumbnailLoader
	{
		public List<Bitmap> All { get; } = [];

		public int Created => All.Count;

		public int Live => All.Count (bitmap => !IsDisposed (bitmap));

		public Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken)
		{
			int edge = Math.Max (1, size);
			var bitmap = new WriteableBitmap (
				new PixelSize (edge, edge),
				new global::Avalonia.Vector (96, 96),
				PixelFormat.Bgra8888,
				AlphaFormat.Premul);
			All.Add (bitmap);
			return Task.FromResult<Bitmap?> (bitmap);
		}

		static bool IsDisposed (Bitmap bitmap)
		{
			try {
				_ = bitmap.PixelSize;
				return false;
			} catch (ObjectDisposedException) {
				return true;
			}
		}
	}
}
