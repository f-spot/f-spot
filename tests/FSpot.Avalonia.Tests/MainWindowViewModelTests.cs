// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;

using FSpot.Avalonia.Input;

using FSpot;
using FSpot.Avalonia.Models;
using FSpot.Avalonia.Services;
using FSpot.Avalonia.ViewModels;
using FSpot.Core;
using FSpot.Editors;
using FSpot.Query;
using FSpot.Resources.Lang;
using FSpot.Utils;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class MainWindowViewModelTests
{
	public TestContext TestContext { get; set; } = null!;

	[TestMethod]
	public void TileSize_FollowsZoom ()
	{
		var viewModel = NewViewModel ();
		viewModel.Zoom = 96;
		Assert.AreEqual (96, viewModel.TileSize);
		Assert.AreEqual (96 + 24, viewModel.FilmstripPaneHeight);
		viewModel.Zoom = 160;
		Assert.AreEqual (160, viewModel.TileSize);
	}

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
	public void ShowToolsCommand_OpensExtensions ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		viewModel.ShowToolsCommand.Execute (null);
		Assert.IsTrue (shell.ExtensionsShown);
		Assert.IsNotNull (shell.ShownAddins);
		Assert.IsGreaterThan (0, shell.ShownAddins.Count);
	}

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
	public void SelectTag_HighlightsWithoutTogglingOff ()
	{
		var viewModel = NewViewModel ();
		var tag = new TagNode (new Tag (null, 1, "Vacation"));

		viewModel.SelectTagCommand.Execute (tag);
		viewModel.SelectTagCommand.Execute (tag);

		Assert.AreSame (tag, viewModel.SelectedTag);
	}

	[TestMethod]
	public void SelectTag_NullClearsTheHighlight ()
	{
		var viewModel = NewViewModel ();
		var tag = new TagNode (new Tag (null, 1, "Vacation"));
		viewModel.SelectTagCommand.Execute (tag);

		viewModel.SelectTagCommand.Execute (null);

		Assert.IsNull (viewModel.SelectedTag);
	}

	[TestMethod]
	public void DeleteTagCommand_CanExecute_ForARightClickedTagWithoutAPriorSelection ()
	{
		var viewModel = NewViewModel ();
		var tag = new TagNode (new Tag (null, 1, "Vacation"));

		Assert.IsNull (viewModel.SelectedTag);
		Assert.IsFalse (viewModel.DeleteTagCommand.CanExecute (null));
		Assert.IsTrue (viewModel.DeleteTagCommand.CanExecute (tag));
	}

	[TestMethod]
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

	[TestMethod]
	public void BeginRenameTag_SelectsTheTagSoDeleteCanRun ()
	{
		var viewModel = NewViewModel ();
		var tag = new TagNode (new Tag (null, 1, "Vacation"));

		viewModel.BeginRenameTagCommand.Execute (tag);

		Assert.AreSame (tag, viewModel.SelectedTag);
		Assert.IsTrue (tag.IsRenaming);
		Assert.IsTrue (viewModel.DeleteTagCommand.CanExecute (null));
	}

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
	public void SelectFolder_HighlightsWithoutTogglingOff ()
	{
		var viewModel = NewViewModel ();
		var folder = new FolderNode (new FolderTreeNode ("photos", new SafeUri ("file:///photos/", true), 2, []), true);

		viewModel.SelectFolderCommand.Execute (folder);
		viewModel.SelectFolderCommand.Execute (folder);

		Assert.AreSame (folder, viewModel.SelectedFolder);
		Assert.IsNull (viewModel.ActiveFolderFilter);
	}

	[TestMethod]
	public void ActivateFolder_SetsTheFindChip ()
	{
		var viewModel = NewViewModel ();
		var folder = new FolderNode (new FolderTreeNode ("photos", new SafeUri ("file:///photos/", true), 2, []), true);

		viewModel.ActivateFolderCommand.Execute (folder);

		Assert.AreSame (folder, viewModel.ActiveFolderFilter);
		Assert.IsTrue (viewModel.HasLibraryFilter);
	}

	[TestMethod]
	public void EnteringPhotoView_SwitchesTheSidebarToEdit ()
	{
		var viewModel = NewViewModel ();
		Assert.AreEqual (0, viewModel.SidebarTab);

		viewModel.EditCommand.Execute (null);

		Assert.AreEqual (2, viewModel.SidebarTab);
		Assert.AreEqual (LibraryViewMode.PhotoView, viewModel.ViewMode);
		Assert.AreEqual (Strings.Edit, viewModel.SidebarPageTitle);
	}

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
	public void EmptyStates_AreHiddenWhenThereIsNoDatabase ()
	{
		var viewModel = NewViewModel ();

		Assert.IsFalse (viewModel.ShowEmptyLibrary);
		Assert.IsFalse (viewModel.ShowNoMatches);
	}

	[TestMethod]
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

	[TestMethod]
	public void HideSidebar_ThenToggleSidebar_ShowsItAgain ()
	{
		var viewModel = NewViewModel ();
		Assert.IsTrue (viewModel.DisplaySidebar);

		viewModel.HideSidebarCommand.Execute (null);
		Assert.IsFalse (viewModel.DisplaySidebar);

		viewModel.ToggleSidebarCommand.Execute (null);
		Assert.IsTrue (viewModel.DisplaySidebar);
	}

	[TestMethod]
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

	[TestMethod]
	public void RotateCommands_CannotExecute_WithoutAFocusedOrSelectedPhoto ()
	{
		var viewModel = NewViewModel ();
		viewModel.Photos.Add (new PhotoItemViewModel (new StubPhoto ("/a.jpg")));

		Assert.IsFalse (viewModel.RotateLeftCommand.CanExecute (null));
		Assert.IsFalse (viewModel.RotateRightCommand.CanExecute (null));
	}

	[TestMethod]
	public void ToggleLoupe_ShowsThenHides ()
	{
		var viewModel = NewViewModel ();
		Assert.IsFalse (viewModel.IsLoupeVisible);

		viewModel.ToggleLoupeCommand.Execute (null);
		Assert.IsTrue (viewModel.IsLoupeVisible);

		viewModel.ToggleLoupeCommand.Execute (null);
		Assert.IsFalse (viewModel.IsLoupeVisible);
	}

	[TestMethod]
	public void ShowLoupe_RequiresEditModeAndAPreview ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();
		viewModel.ToggleLoupeCommand.Execute (null);
		Assert.IsFalse (viewModel.ShowLoupe);

		viewModel.EditCommand.Execute (null);
		Assert.IsFalse (viewModel.ShowLoupe);
	}

	[TestMethod]
	public void ToggleSidebar_HidesWhenVisible ()
	{
		var viewModel = NewViewModel ();
		viewModel.ToggleSidebarCommand.Execute (null);
		Assert.IsFalse (viewModel.DisplaySidebar);
	}

	[TestMethod]
	public void VisibleEditors_InBrowse_AreTheMultiPhotoTools ()
	{
		var viewModel = NewViewModel ();
		var ids = viewModel.VisibleEditors.Select (editor => editor.Id).ToList ();

		Assert.Contains (PhotoEditorCatalog.Desaturate.Id, ids);
		Assert.Contains (PhotoEditorCatalog.Flip.Id, ids);
		Assert.DoesNotContain (PhotoEditorCatalog.Crop.Id, ids);
		Assert.DoesNotContain (PhotoEditorCatalog.RedEye.Id, ids);
	}

	[TestMethod]
	public void VisibleEditors_InPhotoView_IncludeSelectionTools ()
	{
		var viewModel = NewViewModel ();
		viewModel.EditCommand.Execute (null);
		var ids = viewModel.VisibleEditors.Select (editor => editor.Id).ToList ();

		Assert.Contains (PhotoEditorCatalog.Crop.Id, ids);
		Assert.Contains (PhotoEditorCatalog.Color.Id, ids);
		Assert.HasCount (PhotoEditorCatalog.All.Count, ids);
	}

	[TestMethod]
	public void ApplyEditor_Tilt_StartsASettingsSessionThatCanCommit ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();

		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.Tilt);

		Assert.AreEqual (PhotoEditorCatalog.Tilt.Id, viewModel.EditorSession?.Editor.Id);
		Assert.IsTrue (viewModel.ShowTiltSettings);
		Assert.IsTrue (viewModel.CommitEditorCommand.CanExecute (null));
	}

	[TestMethod]
	public void ApplyEditor_Sharpen_StartsASettingsSession ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();

		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.Sharpen);

		Assert.AreEqual (PhotoEditorCatalog.Sharpen.Id, viewModel.EditorSession?.Editor.Id);
		Assert.IsTrue (viewModel.ShowSharpenSettings);
		Assert.AreEqual (0.5, viewModel.EditorSession?.Context.SharpenAmount);
	}

	[TestMethod]
	public void ApplyEditor_SoftFocus_StartsASettingsSession ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();

		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.SoftFocus);

		Assert.AreEqual (PhotoEditorCatalog.SoftFocus.Id, viewModel.EditorSession?.Editor.Id);
		Assert.IsTrue (viewModel.ShowSoftFocusSettings);
		Assert.AreEqual (0.5, viewModel.EditorSession?.Context.SoftFocusRadius);
	}

	[TestMethod]
	public void ApplyEditor_Crop_StartsASelectionSession ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();

		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.Crop);

		Assert.AreEqual (PhotoEditorCatalog.Crop.Id, viewModel.EditorSession?.Editor.Id);
		Assert.IsTrue (viewModel.ShowEditorSelection);
		Assert.IsFalse (viewModel.CommitEditorCommand.CanExecute (null));
	}

	[TestMethod]
	public void ApplyEditor_Color_StartsASettingsSessionThatCanCommit ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();

		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.Color);

		Assert.AreEqual (PhotoEditorCatalog.Color.Id, viewModel.EditorSession?.Editor.Id);
		Assert.IsTrue (viewModel.ShowColorSettings);
		Assert.IsTrue (viewModel.CommitEditorCommand.CanExecute (null));
	}

	[TestMethod]
	public void SetEditorSelection_EnablesCropCommit ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();
		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.Crop);

		viewModel.SetEditorSelection (new FSpot.Imaging.PixelRect (4, 8, 16, 16));

		Assert.IsTrue (viewModel.CommitEditorCommand.CanExecute (null));
		Assert.AreEqual (16, viewModel.EditorSession?.Selection.Width);
	}

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
	public void CancelEditor_ClearsTheSession ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();
		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.Resize);

		viewModel.CancelEditorCommand.Execute (null);

		Assert.IsNull (viewModel.EditorSession);
		Assert.IsFalse (viewModel.ShowEditorSettings);
	}

	[TestMethod]
	public void ApplyEditor_Crop_WithoutAPhoto_Warns ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());

		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.Crop);

		Assert.IsNull (viewModel.EditorSession);
		Assert.AreEqual (Strings.NoSelectionAvailable, shell.WarningMessage);
	}

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
	public void CatalogPhotoItem_UsesTheCatalogIdAsAutomationId ()
	{
		var photo = new Photo (null, null, 42, DateTimeUtil.FromDateTime (new DateTime (2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)));
		photo.AddVersionUnsafely (Photo.OriginalVersionId, new SafeUri ("file:///photos/x.jpg"), "x.jpg", string.Empty, "Original", true);
		var item = new PhotoItemViewModel (photo);

		Assert.AreEqual ("photo.42", item.AutomationId);
		Assert.AreEqual (PhotoDateCaption.Format (photo, wide: false), item.DateCaption);
	}

	[TestMethod]
	public void PhotoItem_SkipsNullTagsWhenFormattingCaptions ()
	{
		var tag = new Tag (null, 1, "Vacation");
		var item = new PhotoItemViewModel (new StubPhoto ("/a.jpg", tags: [null, tag]));
		Assert.AreEqual ("Vacation", item.TagsCaption);
	}

	[TestMethod]
	public void FilePhotoItem_FallsBackToAStablePathAutomationId ()
	{
		var item = new PhotoItemViewModel (new StubPhoto ("/library/trip/img.jpg"));
		Assert.StartsWith ("photo.", item.AutomationId);
		Assert.DoesNotContain ("42", item.AutomationId);
		Assert.Contains ("img.jpg", item.AutomationId);
	}

	[TestMethod]
	public void FindByDate_UsesThePlaceholderChipFormat ()
	{
		var viewModel = NewViewModel ();
		viewModel.SeedCatalog (new StubPhoto ("/june.jpg", new DateTime (2026, 6, 15)));
		viewModel.FindByDateCommand.Execute (null);

		Assert.AreEqual (QueryChipFormat.Date (viewModel.FindStart, viewModel.FindEnd), viewModel.FindDateLabel);
	}

	[TestMethod]
	public void TimelineBucketToolTip_UsesTheMonthCountFormat ()
	{
		var viewModel = NewViewModel ();
		viewModel.SeedCatalog (new StubPhoto ("/june.jpg", new DateTime (2026, 6, 15)));
		var bucket = viewModel.TimelineBuckets.Single ();
		Assert.AreEqual (QueryChipFormat.TimelineMonth (bucket.Start, bucket.Count), bucket.ToolTip);
	}

	[TestMethod]
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

	[TestMethod]
	public void PreviewInterpolation_FollowsViewerInterpolation ()
	{
		var viewModel = NewViewModel ();
		viewModel.ViewerInterpolation = true;
		Assert.AreEqual (global::Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality, viewModel.PreviewInterpolation);
		viewModel.ViewerInterpolation = false;
		Assert.AreEqual (global::Avalonia.Media.Imaging.BitmapInterpolationMode.None, viewModel.PreviewInterpolation);
	}

	[TestMethod]
	public void HelpContentsCommand_OpensTheGtkHelpUri ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		viewModel.HelpContentsCommand.Execute (null);
		Assert.AreEqual (HelpContents.Current, shell.OpenedUri);
	}

	[TestMethod]
	public void ImportFromCameraCommand_WarnsWhenNoCamerasAreDetected ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		viewModel.ImportFromCameraCommand.Execute (null);
		Assert.AreEqual (Strings.NoCamerasDetected, shell.WarningMessage);
	}

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
	public void RefreshThumbnailCommand_ClearsThePreviewCache ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();
		viewModel.RefreshThumbnailCommand.Execute (null);
		Assert.IsNull (viewModel.Photos[0].Thumbnail);
	}

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
	public void RatingKey_ReadsDigitsAndSkipsATextBox ()
	{
		Assert.AreEqual (0u, RatingKey.From (Key.D0, KeyModifiers.None, null));
		Assert.AreEqual (5u, RatingKey.From (Key.NumPad5, KeyModifiers.None, null));
		Assert.IsNull (RatingKey.From (Key.D3, KeyModifiers.Control, null));
		Assert.IsNull (RatingKey.From (Key.A, KeyModifiers.None, null));
	}

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
	public void EditBarDescription_IsWrittenOntoThePhoto ()
	{
		var viewModel = NewViewModel ();
		var photo = CatalogPhoto ();
		viewModel.SeedCatalog (photo);
		viewModel.SelectPhoto (viewModel.Photos[0]);

		viewModel.Info.Description = "Picnic";

		Assert.AreEqual ("Picnic", photo.Description);
	}

	[TestMethod]
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

	[TestMethod]
	[DoNotParallelize]
	public void Histogram_IgnoresALoadThatFinishesAfterSelectionMoves ()
	{
		AvaloniaApp.Ensure ();
		using var thumb = new WriteableBitmap (new PixelSize (4, 4), new global::Avalonia.Vector (96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
		var late = new WriteableBitmap (new PixelSize (2, 2), new global::Avalonia.Vector (96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
		var previous = SynchronizationContext.Current;
		SynchronizationContext.SetSynchronizationContext (null);
		try {
			var gate = new GateThumbnails ();
			var viewModel = new MainWindowViewModel (null, new RecordingShell (), gate);
			var first = new PhotoItemViewModel (new StubPhoto ("/photos/first.jpg"));
			var second = new PhotoItemViewModel (new StubPhoto ("/photos/second.jpg"));
			second.Thumbnail = thumb;
			viewModel.Photos.Add (first);
			viewModel.Photos.Add (second);

			viewModel.SelectedPhoto = first;
			Assert.IsTrue (gate.Started.Task.IsCompleted);
			viewModel.SelectedPhoto = second;
			var current = viewModel.Histogram;
			Assert.IsNotNull (current);

			gate.Release.SetResult (late);

			Assert.AreSame (current, viewModel.Histogram);
		} finally {
			SynchronizationContext.SetSynchronizationContext (previous);
		}
	}

	[TestMethod]
	public void InfoVisibility_LoadsTheSavedDateFlag ()
	{
		var saved = InfoFieldVisibility.Load ();
		saved.Date = false;
		saved.Size = false;
		saved.Save ();
		try {
			var viewModel = NewViewModel ();
			Assert.IsFalse (viewModel.InfoVisibility.Date);
			Assert.IsFalse (viewModel.InfoVisibility.Size);
			viewModel.ToggleInfoFieldCommand.Execute ("Size");
			Assert.IsTrue (viewModel.InfoVisibility.Size);
			Assert.IsTrue (InfoFieldVisibility.Load ().Size);
		} finally {
			var restore = InfoFieldVisibility.Load ();
			restore.Date = true;
			restore.Size = true;
			restore.Save ();
		}
	}

	[TestMethod]
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

	[TestMethod]
	public void ActivateFolderCommand_IsTheFindDropTarget ()
	{
		var viewModel = NewViewModel ();
		var folder = new FolderNode (new FolderTreeNode ("photos", new SafeUri ("file:///photos/", true), 2, []), true, 2);
		viewModel.ActivateFolderCommand.Execute (folder);
		Assert.AreSame (folder, viewModel.ActiveFolderFilter);
	}

	[TestMethod]
	public void NotYet_ShowsTheUnavailableWarning ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());

		viewModel.NotYetCommand.Execute (null);

		Assert.AreEqual (Strings.ThisFeatureIsNotYetAvailable, shell.WarningMessage);
	}

	static MainWindowViewModel NewViewModel ()
		=> new (null, new NullShell (), new NullThumbnails ());

	static Photo CatalogPhoto ()
	{
		string dest = Path.GetTempFileName () + ".jpg";
		string src = Path.Combine (RepoRoot (), "tests", "FSpot.Tests", "TestData", "taglib-sample.jpg");
		File.Copy (src, dest, true);
		FSpot.Settings.FSpotConfiguration.PhotoUri = new SafeUri (Path.GetTempPath ());
		var photo = new Photo (null, null, 1, DateTimeUtil.FromDateTime (new DateTime (2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)));
		var uri = new SafeUri (dest);
		photo.AddVersionUnsafely (Photo.OriginalVersionId, uri.GetBaseUri (), uri.GetFilename (), string.Empty, "Original", true);
		photo.AllVersionsLoaded = true;
		return photo;
	}

	static string RepoRoot ()
	{
		for (var dir = new DirectoryInfo (AppContext.BaseDirectory); dir != null; dir = dir.Parent) {
			if (File.Exists (Path.Combine (dir.FullName, "F-Spot.slnx")))
				return dir.FullName;
		}
		Assert.Fail ("F-Spot.slnx not found");
		return null!;
	}

	static MainWindowViewModel ViewModelWithFocusedPhoto ()
	{
		var viewModel = NewViewModel ();
		var item = new PhotoItemViewModel (new StubPhoto ("/a.jpg"));
		viewModel.Photos.Add (item);
		viewModel.SelectedPhoto = item;
		return viewModel;
	}

	class NullShell : IAvaloniaShell
	{
		public void Close () { }
		public void ToggleFullscreen () { }
		public void FocusFind () { }
		public Task ShowAboutAsync () => Task.CompletedTask;
		public virtual Task ShowWarningAsync (string title, string message) => Task.CompletedTask;
		public Task<bool> ShowImportAsync () => Task.FromResult (false);
		public virtual Task<bool> ShowImportAsync (IReadOnlyList<string> paths) => Task.FromResult (false);
		public virtual Task<string?> PickFolderAsync (string title) => Task.FromResult<string?> (null);
		public virtual Task<string?> PromptAsync (string title, string prompt, string defaultValue) => Task.FromResult<string?> (null);
		public virtual Task<bool> ConfirmAsync (string title, string message, string okCaption) => Task.FromResult (false);
		public virtual Task<(uint Min, uint Max)?> PromptRatingRangeAsync () => Task.FromResult<(uint, uint)?> ((3, 5));
		public virtual Task<(DateTime Start, DateTime End)?> PromptDateRangeAsync ()
			=> Task.FromResult<(DateTime, DateTime)?> ((new DateTime (2026, 6, 1), new DateTime (2026, 7, 1)));
		public virtual Task<uint?> PickImportRollAsync (IReadOnlyList<(uint Id, string Label)> rolls)
			=> Task.FromResult<uint?> (1);
		public virtual Task PrintPhotosAsync (IReadOnlyList<string> paths, PrintPageSetup? setup = null) => Task.CompletedTask;
		public virtual Task CopyPhotosAsync (IReadOnlyList<string> paths) => Task.CompletedTask;
		public virtual Task SetDesktopBackgroundAsync (string path) => Task.CompletedTask;
		public virtual Task<PrintPageSetup?> PromptPageSetupAsync (PrintPageSetup current) => Task.FromResult<PrintPageSetup?> (null);
		public virtual Task ShowExtensionsAsync (IList<AddinStatus> addins) => Task.CompletedTask;
		public virtual Task MailPhotosAsync (IReadOnlyList<string> paths) => Task.CompletedTask;
		public virtual void ShowSlideshow (IReadOnlyList<string> paths, int startIndex) { }
		public virtual void OpenUri (string uri) { }
		public virtual Task<double?> PromptHoursOffsetAsync () => Task.FromResult<double?> (null);
		public virtual Task<TimeAdjustChoice?> PromptAdjustTimeAsync (IReadOnlyList<(string Name, DateTime Time)> photos)
			=> Task.FromResult<TimeAdjustChoice?> (null);
	}

	sealed class RecordingShell : NullShell
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
		public bool ConfirmResult { get; set; }
		public bool ConfirmCalled { get; private set; }
		public double? Hours { get; set; }
		public IReadOnlyList<string>? ImportedPaths { get; private set; }

		public override Task<bool> ShowImportAsync (IReadOnlyList<string> paths)
		{
			ImportedPaths = paths;
			return Task.FromResult (false);
		}

		public override Task<string?> PromptAsync (string title, string prompt, string defaultValue)
			=> Task.FromResult (PromptResult);

		public override Task<bool> ConfirmAsync (string title, string message, string okCaption)
		{
			ConfirmCalled = true;
			return Task.FromResult (ConfirmResult);
		}

		public override Task<double?> PromptHoursOffsetAsync ()
			=> Task.FromResult (Hours);

		public override Task<TimeAdjustChoice?> PromptAdjustTimeAsync (IReadOnlyList<(string Name, DateTime Time)> photos)
			=> Task.FromResult<TimeAdjustChoice?> (Hours is double hours
				? new TimeAdjustChoice { ShiftByOffset = true, Hours = hours }
				: null);

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

	sealed class NullThumbnails : IPhotoThumbnailLoader
	{
		public Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken)
			=> Task.FromResult<Bitmap?> (null);
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

		public Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken)
		{
			Started.TrySetResult ();
			// The test completes Release. This method only waits on that gate.
#pragma warning disable VSTHRD003
			return Release.Task;
#pragma warning restore VSTHRD003
		}
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
		public StubPhoto (string path, DateTime? time = null, uint rating = 0, params Tag?[] tags)
		{
			Name = System.IO.Path.GetFileName (path);
			DefaultVersion = new StubVersion (path);
			Versions = new[] { DefaultVersion };
			Time = time ?? DateTime.UnixEpoch;
			Rating = rating;
			// FormatTags skips null entries. IPhoto.Tags cannot say an element may be null.
			Tags = tags is null ? Array.Empty<Tag> () : Array.ConvertAll (tags, static tag => tag!);
		}

		public DateTime Time { get; }
		public Tag[] Tags { get; }
		public string Description => string.Empty;
		public string Name { get; }
		public uint Rating { get; }
		public IPhotoVersion DefaultVersion { get; }
		public IEnumerable<IPhotoVersion> Versions { get; }
	}
}
