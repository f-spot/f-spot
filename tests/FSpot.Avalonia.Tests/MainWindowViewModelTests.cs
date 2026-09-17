// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Media.Imaging;

using FSpot.Avalonia.Models;
using FSpot.Avalonia.Services;
using FSpot.Avalonia.ViewModels;
using FSpot.Core;
using FSpot.Editors;
using FSpot.Query;
using FSpot.Resources.Lang;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class MainWindowViewModelTests
{
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
		Assert.IsEmpty (viewModel.Photos);
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
	public void NotYet_ShowsTheUnavailableWarning ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());

		viewModel.NotYetCommand.Execute (null);

		Assert.AreEqual (Strings.ThisFeatureIsNotYetAvailable, shell.WarningMessage);
	}

	static MainWindowViewModel NewViewModel ()
		=> new (null, new NullShell (), new NullThumbnails ());

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
		public virtual Task<string?> PickFolderAsync (string title) => Task.FromResult<string?> (null);
		public Task<string?> PromptAsync (string title, string prompt, string defaultValue) => Task.FromResult<string?> (null);
		public Task<bool> ConfirmAsync (string title, string message, string okCaption) => Task.FromResult (false);
		public virtual Task<(uint Min, uint Max)?> PromptRatingRangeAsync () => Task.FromResult<(uint, uint)?> ((3, 5));
		public virtual Task<(DateTime Start, DateTime End)?> PromptDateRangeAsync ()
			=> Task.FromResult<(DateTime, DateTime)?> ((new DateTime (2026, 6, 1), new DateTime (2026, 7, 1)));
		public virtual Task<uint?> PickImportRollAsync (IReadOnlyList<(uint Id, string Label)> rolls)
			=> Task.FromResult<uint?> (1);
		public virtual Task PrintPhotosAsync (IReadOnlyList<string> paths) => Task.CompletedTask;
		public virtual Task MailPhotosAsync (IReadOnlyList<string> paths) => Task.CompletedTask;
		public virtual void ShowSlideshow (IReadOnlyList<string> paths, int startIndex) { }
	}

	sealed class RecordingShell : NullShell
	{
		public string? WarningTitle { get; private set; }
		public string? WarningMessage { get; private set; }
		public IReadOnlyList<string>? Printed { get; private set; }
		public IReadOnlyList<string>? Mailed { get; private set; }
		public IReadOnlyList<string>? Slideshow { get; private set; }
		public string? ExportFolder { get; set; }

		public override Task ShowWarningAsync (string title, string message)
		{
			WarningTitle = title;
			WarningMessage = message;
			return Task.CompletedTask;
		}

		public override Task PrintPhotosAsync (IReadOnlyList<string> paths)
		{
			Printed = paths.ToList ();
			return Task.CompletedTask;
		}

		public override Task MailPhotosAsync (IReadOnlyList<string> paths)
		{
			Mailed = paths.ToList ();
			return Task.CompletedTask;
		}

		public override void ShowSlideshow (IReadOnlyList<string> paths, int startIndex)
			=> Slideshow = paths.ToList ();

		public override Task<string?> PickFolderAsync (string title)
			=> Task.FromResult (ExportFolder);
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
		public StubPhoto (string path, DateTime? time = null, uint rating = 0, params Tag[] tags)
		{
			Name = System.IO.Path.GetFileName (path);
			DefaultVersion = new StubVersion (path);
			Versions = new[] { DefaultVersion };
			Time = time ?? DateTime.UnixEpoch;
			Rating = rating;
			Tags = tags ?? Array.Empty<Tag> ();
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
