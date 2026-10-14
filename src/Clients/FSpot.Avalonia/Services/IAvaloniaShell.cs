//
// IAvaloniaShell.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot;
using FSpot.Avalonia.Dialogs;
using FSpot.Core;
using FSpot.Resources.Lang;

using Hyena;

namespace FSpot.Avalonia.Services;

public interface IAvaloniaShell
{
	// Closes just this window. Bound to Ctrl+W / Photo > Close.
	void Close ();
	// Quits the whole application. Bound to Ctrl+Q / Photo > Quit. Distinct
	// from Close() so a future multi-window shell (e.g. the standalone
	// viewer) can close one window without ending the app.
	void Quit () => Close ();
	void ToggleFullscreen ();
	void FocusFind ();
	Task ShowAboutAsync ();
	Task ShowWarningAsync (string title, string message);
	// Falls back to a plain warning (still listing every failed file) for a
	// shell that doesn't override it, e.g. the NullShell test double.
	Task ShowImportFailuresAsync (IReadOnlyList<SafeUri> failed)
		=> failed.Count == 0 ? Task.CompletedTask : ShowWarningAsync (Strings.ImportFailures, ImportCompletion.FormatFailures (failed));
	Task<bool> ShowImportAsync ();
	Task<bool> ShowImportAsync (IReadOnlyList<string> paths);
	Task<string?> PickFolderAsync (string title);
	Task<IReadOnlyList<string>> PickFilesAsync (string title, bool allowMultiple)
		=> Task.FromResult<IReadOnlyList<string>> (Array.Empty<string> ());
	Task<string?> PromptAsync (string title, string prompt, string defaultValue);
	Task<bool> ConfirmAsync (string title, string message, string okCaption);

	// The GTK filter and tag dialogs. The tag and version defaults fall back to the plain
	// name prompt so a shell without dialogs of its own still works.
	Task<(uint Min, uint Max)?> PromptRatingFilterAsync (uint min, uint max)
		=> Task.FromResult<(uint, uint)?> (null);

	Task<DateRangeChoice?> PromptDateRangeFilterAsync (DateTime? start, DateTime? end)
		=> Task.FromResult<DateRangeChoice?> (null);

	Task<IReadOnlyList<uint>?> PickImportRollsAsync (IReadOnlyList<ImportRollOption> rolls)
		=> Task.FromResult<IReadOnlyList<uint>?> (null);

	async Task<CreateTagChoice?> PromptCreateTagAsync (Category root, Category parent, bool autoIcon)
	{
		string? name = await PromptAsync (Strings.CreateNewTag, Strings.NameOfNewTagColon, string.Empty);
		return string.IsNullOrWhiteSpace (name) ? null : new CreateTagChoice (name.Trim (), parent, autoIcon);
	}

	async Task<EditTagChoice?> PromptEditTagAsync (Category root, TagIconSource tag)
	{
		string? name = await PromptAsync (Strings.EditTag, Strings.NameColon, tag.Tag.Name);
		return string.IsNullOrWhiteSpace (name) ? null : new EditTagChoice (name.Trim (), tag.Tag.Category, null);
	}

	async Task<string?> PromptVersionNameAsync (VersionNameRequest request, string currentName, IReadOnlyCollection<string> existingNames)
		=> await PromptAsync (VersionNameRules.Title (request), VersionNameRules.Prompt (request),
			request == VersionNameRequest.Rename ? currentName : string.Empty);

	Task<IReadOnlyList<Tag>?> PickTagsAsync (Category root)
		=> Task.FromResult<IReadOnlyList<Tag>?> (null);

	Task PrintPhotosAsync (IReadOnlyList<string> paths, PrintPageSetup? setup = null);
	Task MailPhotosAsync (IReadOnlyList<string> paths);
	// The print dialog; returns the page setup it ended with, or null when unchanged.
	async Task<PrintPageSetup?> PrintPhotoItemsAsync (IReadOnlyList<IPhoto> photos, PrintPageSetup setup)
	{
		await PrintPhotosAsync (PhotoFileActions.SelectedPaths (photos), setup);
		return null;
	}
	Task CopyPhotosAsync (IReadOnlyList<string> paths) => Task.CompletedTask;
	Task SetDesktopBackgroundAsync (string path) => Task.CompletedTask;
	Task<PrintPageSetup?> PromptPageSetupAsync (PrintPageSetup current) => Task.FromResult<PrintPageSetup?> (null);
	void ShowSlideshow (IReadOnlyList<string> paths, int startIndex);
	void ShowFullScreen (IReadOnlyList<string> paths, int startIndex) { }
	void OpenUri (string uri) { }
	Task ShowPreferencesAsync () => Task.CompletedTask;
	Task ShowExtensionsAsync (IList<AddinStatus> addins) => Task.CompletedTask;
	Task<double?> PromptHoursOffsetAsync () => Task.FromResult<double?> (null);
	Task<IReadOnlyList<FSpot.Editors.CropConstraint>?> EditCustomCropRatiosAsync (IReadOnlyList<FSpot.Editors.CropConstraint> custom)
		=> Task.FromResult<IReadOnlyList<FSpot.Editors.CropConstraint>?> (null);
	Task<TimeAdjustChoice?> PromptAdjustTimeAsync (IReadOnlyList<(string Name, DateTime Time, string Path)> photos)
		=> Task.FromResult<TimeAdjustChoice?> (null);
	// A modeless progress window for a long batch operation, matching
	// FSpot.Gtk's ProgressDialog/ThreadProgressDialog: falls back to a no-op
	// handle for a shell that doesn't show real UI.
	IProgressHandle ShowProgress (string title, int total, ProgressCancelKind cancelKind = ProgressCancelKind.None)
		=> NullProgressHandle.Instance;
}
