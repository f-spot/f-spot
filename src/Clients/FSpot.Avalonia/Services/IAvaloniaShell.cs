//
// IAvaloniaShell.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot;

namespace FSpot.Avalonia.Services;

public interface IAvaloniaShell
{
	void Close ();
	void ToggleFullscreen ();
	void FocusFind ();
	Task ShowAboutAsync ();
	Task ShowWarningAsync (string title, string message);
	Task<bool> ShowImportAsync ();
	Task<bool> ShowImportAsync (IReadOnlyList<string> paths);
	Task<string?> PickFolderAsync (string title);
	Task<string?> PromptAsync (string title, string prompt, string defaultValue);
	Task<bool> ConfirmAsync (string title, string message, string okCaption);
	Task<(uint Min, uint Max)?> PromptRatingRangeAsync ();
	Task<(DateTime Start, DateTime End)?> PromptDateRangeAsync ();
	Task<uint?> PickImportRollAsync (IReadOnlyList<(uint Id, string Label)> rolls);
	Task PrintPhotosAsync (IReadOnlyList<string> paths, PrintPageSetup? setup = null);
	Task MailPhotosAsync (IReadOnlyList<string> paths);
	Task CopyPhotosAsync (IReadOnlyList<string> paths) => Task.CompletedTask;
	Task SetDesktopBackgroundAsync (string path) => Task.CompletedTask;
	Task<PrintPageSetup?> PromptPageSetupAsync (PrintPageSetup current) => Task.FromResult<PrintPageSetup?> (null);
	void ShowSlideshow (IReadOnlyList<string> paths, int startIndex);
	void OpenUri (string uri) { }
	Task ShowPreferencesAsync () => Task.CompletedTask;
	Task ShowExtensionsAsync (IList<AddinStatus> addins) => Task.CompletedTask;
	Task<double?> PromptHoursOffsetAsync () => Task.FromResult<double?> (null);
	Task<TimeAdjustChoice?> PromptAdjustTimeAsync (IReadOnlyList<(string Name, DateTime Time)> photos)
		=> Task.FromResult<TimeAdjustChoice?> (null);
}
