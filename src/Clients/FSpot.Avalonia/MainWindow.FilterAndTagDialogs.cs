// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using FSpot.Avalonia.Dialogs;
using FSpot.Avalonia.Models;
using FSpot.Avalonia.ViewModels;
using FSpot.Core;

namespace FSpot.Avalonia;

// The GTK filter and tag dialogs, shown for the view model through IAvaloniaShell.
public partial class MainWindow
{
	public async Task<ZipScaleChoice?> PromptZipScaleAsync ()
	{
		var dialog = new ZipScaleWindow ();
		await dialog.ShowDialog (this);
		return dialog.Result;
	}

	public async Task<(uint Min, uint Max)?> PromptRatingFilterAsync (uint min, uint max)
	{
		var dialog = new RatingFilterWindow (min, max);
		await dialog.ShowDialog (this);
		return dialog.Result;
	}

	public async Task<DateRangeChoice?> PromptDateRangeFilterAsync (DateTime? start, DateTime? end)
	{
		var dialog = new DateRangeWindow (DateTime.Today, start, end);
		await dialog.ShowDialog (this);
		return dialog.Result;
	}

	public async Task<IReadOnlyList<uint>?> PickImportRollsAsync (IReadOnlyList<ImportRollOption> rolls)
	{
		var dialog = new ImportRollsWindow (rolls);
		await dialog.ShowDialog (this);
		return dialog.Result;
	}

	public async Task<CreateTagChoice?> PromptCreateTagAsync (Category root, Category parent, bool autoIcon)
	{
		var dialog = TagWindow.ForCreate (root, parent, autoIcon);
		await dialog.ShowDialog (this);
		return dialog.CreateResult;
	}

	public async Task<EditTagChoice?> PromptEditTagAsync (Category root, TagIconSource tag)
	{
		var dialog = TagWindow.ForEdit (root, tag);
		await dialog.ShowDialog (this);
		return dialog.EditResult;
	}

	public async Task<string?> PromptVersionNameAsync (VersionNameRequest request, string currentName, IReadOnlyCollection<string> existingNames)
	{
		var dialog = new VersionNameWindow (request, currentName, existingNames);
		await dialog.ShowDialog (this);
		return dialog.Result;
	}

	public async Task<IReadOnlyList<Tag>?> PickTagsAsync (Category root)
	{
		var dialog = new TagSelectionWindow (root);
		await dialog.ShowDialog (this);
		return dialog.Result;
	}

	/// <summary>
	/// Mirrors the sidebar tag tree's multi-selection into the view model (for Merge Tags),
	/// and keeps a Ctrl/Cmd/Shift-click or a right-click on a highlighted tag from
	/// collapsing the selection to one tag.
	/// </summary>
	void WireTagHighlight (MainWindowViewModel viewModel)
	{
		SidebarTagTree.SelectionChanged += (_, _)
			=> viewModel.SetHighlightedTags (SidebarTagTree.SelectedItems.OfType<TagNode> ().ToList ());
		SidebarTagTree.AddHandler (PointerPressedEvent, (_, e) => {
			var point = e.GetCurrentPoint (SidebarTagTree);
			bool modified = (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta | KeyModifiers.Shift)) != 0;
			bool onHighlighted = (e.Source as StyledElement)?.DataContext is TagNode node
				&& SidebarTagTree.SelectedItems.Contains (node);
			viewModel.ExtendingTagSelection = modified || (point.Properties.IsRightButtonPressed && onHighlighted);
		}, RoutingStrategies.Tunnel, handledEventsToo: true);
	}
}
