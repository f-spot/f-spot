//
// IAvaloniaShell.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Services;

public interface IAvaloniaShell
{
	void Close ();
	void ToggleFullscreen ();
	void FocusFind ();
	Task ShowAboutAsync ();
	Task ShowWarningAsync (string title, string message);
	Task<bool> ShowImportAsync ();
	Task<string?> PickFolderAsync (string title);
	Task<string?> PromptAsync (string title, string prompt, string defaultValue);
	Task<bool> ConfirmAsync (string title, string message, string okCaption);
	Task<(uint Min, uint Max)?> PromptRatingRangeAsync ();
	Task<(DateTime Start, DateTime End)?> PromptDateRangeAsync ();
	Task<uint?> PickImportRollAsync (IReadOnlyList<(uint Id, string Label)> rolls);
	Task PrintPhotosAsync (IReadOnlyList<string> paths);
	Task MailPhotosAsync (IReadOnlyList<string> paths);
	void ShowSlideshow (IReadOnlyList<string> paths, int startIndex);
}
