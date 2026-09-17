using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

using FSpot.Avalonia.Models;
using FSpot.Avalonia.Services;
using FSpot.Avalonia.ViewModels;
using FSpot.Database;
using FSpot.Resources.Lang;

namespace FSpot.Avalonia;

public partial class MainWindow : Window, IAvaloniaShell
{
	public MainWindow () : this (App.Database)
	{
	}

	public MainWindow (Db? database)
	{
		var viewModel = new MainWindowViewModel (database, this, new AvaloniaThumbnailLoader ());
		DataContext = viewModel;
		InitializeComponent ();
		viewModel.LoadCatalog ();
	}

	void OnPreviewPointerMoved (object? sender, PointerEventArgs e)
	{
		if (DataContext is not MainWindowViewModel viewModel || !viewModel.IsLoupeVisible)
			return;
		var point = e.GetPosition (PreviewImage);
		viewModel.MoveLoupe (point.X, point.Y, PreviewImage.Bounds.Width, PreviewImage.Bounds.Height);
	}

	protected override void OnKeyDown (KeyEventArgs e)
	{
		var focused = TopLevel.GetTopLevel (this)?.FocusManager?.GetFocusedElement () ?? e.Source;
		if (Input.ShortcutGuards.IsUnadornedLoupeKey (e.Key, e.KeyModifiers)) {
			if (Input.ShortcutGuards.AllowsUnadornedKey (focused)
					&& DataContext is MainWindowViewModel viewModel) {
				viewModel.ToggleLoupeCommand.Execute (null);
				e.Handled = true;
			}
			return;
		}
		base.OnKeyDown (e);
	}

	public async Task<(uint Min, uint Max)?> PromptRatingRangeAsync ()
	{
		string? text = await PromptAsync (Strings.Find, "Rating 0-5 (min-max)", "0-5");
		if (string.IsNullOrWhiteSpace (text))
			return null;
		var parts = text.Split ('-', '–', ',');
		if (parts.Length >= 2
				&& uint.TryParse (parts[0].Trim (), out uint min)
				&& uint.TryParse (parts[1].Trim (), out uint max))
			return (min, max);
		if (uint.TryParse (text.Trim (), out uint only))
			return (only, 5);
		return null;
	}

	public async Task<(DateTime Start, DateTime End)?> PromptDateRangeAsync ()
	{
		string? text = await PromptAsync (Strings.Find, "Dates YYYY-MM-DD:YYYY-MM-DD", string.Empty);
		if (string.IsNullOrWhiteSpace (text))
			return null;
		var parts = text.Split (':', '–');
		if (parts.Length >= 2
				&& DateTime.TryParse (parts[0].Trim (), out var start)
				&& DateTime.TryParse (parts[1].Trim (), out var end))
			return (start, end);
		return null;
	}

	public async Task<uint?> PickImportRollAsync (IReadOnlyList<(uint Id, string Label)> rolls)
	{
		string hint = rolls.Count == 0
			? "Roll id"
			: string.Join (", ", rolls.Select (roll => $"{roll.Id}:{roll.Label}"));
		string? text = await PromptAsync (Strings.ImportRoll, hint, rolls.Count > 0 ? rolls[0].Id.ToString () : "1");
		return uint.TryParse (text, out uint id) ? id : null;
	}

	public Task PrintPhotosAsync (IReadOnlyList<string> paths)
		=> ShowWarningAsync (Strings.Print, string.Join (Environment.NewLine, paths));

	public Task MailPhotosAsync (IReadOnlyList<string> paths)
		=> ShowWarningAsync (Strings.SendMail, string.Join (Environment.NewLine, paths));

	public void ShowSlideshow (IReadOnlyList<string> paths, int startIndex)
	{
		if (paths.Count == 0)
			return;
		var window = new SlideshowWindow (paths, startIndex);
		window.Show (this);
	}

	public void ToggleFullscreen ()
		=> WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;

	public void FocusFind ()
		=> Dispatcher.UIThread.Post (() => FindBox.Focus ());

	public async Task ShowAboutAsync ()
		=> await new AboutWindow ().ShowDialog (this);

	public async Task ShowWarningAsync (string title, string message)
	{
		var dialog = new Window {
			Title = title,
			Width = 460,
			Height = 200,
			WindowStartupLocation = WindowStartupLocation.CenterOwner,
			CanResize = false
		};
		SetId (dialog, AutomationIds.DialogWindow, title);
		var messageBlock = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap };
		SetId (messageBlock, AutomationIds.DialogMessage, message);
		var close = new Button {
			Content = "OK",
			HorizontalAlignment = HorizontalAlignment.Right
		};
		SetId (close, AutomationIds.DialogOk, "OK");
		close.Click += (_, _) => dialog.Close ();
		dialog.Content = new StackPanel {
			Margin = new Thickness (16),
			Spacing = 12,
			Children = {
				messageBlock,
				close
			}
		};
		await dialog.ShowDialog (this);
	}

	public async Task<bool> ShowImportAsync ()
	{
		if (App.Database is null)
			return false;

		ImportWindow? window = null;
		var viewModel = new ImportViewModel (App.Database, this, new AvaloniaThumbnailLoader (), result => window?.Close (result));
		window = new ImportWindow (viewModel);
		return await window.ShowDialog<bool> (this);
	}

	public async Task<string?> PromptAsync (string title, string prompt, string defaultValue)
	{
		var promptLabel = new TextBlock { Text = prompt };
		SetId (promptLabel, AutomationIds.DialogPrompt, prompt);
		var box = new TextBox { Text = defaultValue };
		SetId (box, AutomationIds.DialogInput, prompt);
		var ok = new Button { Content = "OK", IsDefault = true };
		SetId (ok, AutomationIds.DialogOk, "OK");
		var cancel = new Button { Content = "Cancel", IsCancel = true };
		SetId (cancel, AutomationIds.DialogCancel, "Cancel");
		string? result = null;
		var dialog = new Window {
			Title = title,
			Width = 420,
			Height = 160,
			WindowStartupLocation = WindowStartupLocation.CenterOwner,
			CanResize = false
		};
		SetId (dialog, AutomationIds.DialogWindow, title);
		ok.Click += (_, _) => {
			result = box.Text;
			dialog.Close ();
		};
		cancel.Click += (_, _) => dialog.Close ();
		dialog.Content = new StackPanel {
			Margin = new Thickness (16),
			Spacing = 10,
			Children = {
				promptLabel,
				box,
				new StackPanel {
					Orientation = Orientation.Horizontal,
					HorizontalAlignment = HorizontalAlignment.Right,
					Spacing = 8,
					Children = { cancel, ok }
				}
			}
		};
		await dialog.ShowDialog (this);
		return result;
	}

	public async Task<bool> ConfirmAsync (string title, string message, string okCaption)
	{
		var ok = new Button { Content = okCaption, IsDefault = true };
		SetId (ok, AutomationIds.DialogOk, okCaption);
		var cancel = new Button { Content = "Cancel", IsCancel = true };
		SetId (cancel, AutomationIds.DialogCancel, "Cancel");
		var confirmed = false;
		var dialog = new Window {
			Title = title,
			Width = 460,
			Height = 180,
			WindowStartupLocation = WindowStartupLocation.CenterOwner,
			CanResize = false
		};
		SetId (dialog, AutomationIds.DialogWindow, title);
		var messageBlock = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap };
		SetId (messageBlock, AutomationIds.DialogMessage, message);
		ok.Click += (_, _) => {
			confirmed = true;
			dialog.Close ();
		};
		cancel.Click += (_, _) => dialog.Close ();
		dialog.Content = new StackPanel {
			Margin = new Thickness (16),
			Spacing = 12,
			Children = {
				messageBlock,
				new StackPanel {
					Orientation = Orientation.Horizontal,
					HorizontalAlignment = HorizontalAlignment.Right,
					Spacing = 8,
					Children = { cancel, ok }
				}
			}
		};
		await dialog.ShowDialog (this);
		return confirmed;
	}

	public async Task<string?> PickFolderAsync (string title)
	{
		var folders = await StorageProvider.OpenFolderPickerAsync (new FolderPickerOpenOptions {
			Title = title,
			AllowMultiple = false
		});
		return folders.Count == 0 ? null : folders[0].Path.LocalPath;
	}

	static void SetId (StyledElement target, string id, string name)
	{
		AutomationProperties.SetAutomationId (target, id);
		AutomationProperties.SetName (target, name);
	}

	void OnPhotoDoubleTapped (object? sender, TappedEventArgs e)
	{
		if (DataContext is MainWindowViewModel viewModel
				&& (e.Source as Control)?.DataContext is PhotoItemViewModel item)
			viewModel.OpenPhotoCommand.Execute (item);
	}

	void OnTagPointerPressed (object? sender, PointerPressedEventArgs e)
	{
		if (!e.GetCurrentPoint ((Visual)sender!).Properties.IsLeftButtonPressed)
			return;
		if (sender is not Control { DataContext: TagNode node })
			return;
		var data = new DataTransfer ();
		data.Add (DataTransferItem.Create (DragFormats.TagId, node.Tag.Id.ToString ()));
		_ = DragDrop.DoDragDropAsync (e, data, DragDropEffects.Copy);
	}

	void OnPhotoPointerPressed (object? sender, PointerPressedEventArgs e)
	{
		if (!e.GetCurrentPoint ((Visual)sender!).Properties.IsLeftButtonPressed)
			return;
		var data = new DataTransfer ();
		data.Add (DataTransferItem.Create (DragFormats.Photos, "1"));
		_ = DragDrop.DoDragDropAsync (e, data, DragDropEffects.Copy);
	}

	void OnPhotoDragOver (object? sender, DragEventArgs e)
		=> e.DragEffects = e.DataTransfer.Contains (DragFormats.TagId) ? DragDropEffects.Copy : DragDropEffects.None;

	void OnPhotoDrop (object? sender, DragEventArgs e)
	{
		if (DataContext is not MainWindowViewModel viewModel)
			return;
		var target = (e.Source as Control)?.DataContext as PhotoItemViewModel;
		viewModel.AttachDroppedTag (TryGetString (e.DataTransfer, DragFormats.TagId), target);
	}

	void OnTagTreeDragOver (object? sender, DragEventArgs e)
		=> e.DragEffects = e.DataTransfer.Contains (DragFormats.Photos) ? DragDropEffects.Copy : DragDropEffects.None;

	void OnTagTreeDrop (object? sender, DragEventArgs e)
	{
		if (DataContext is not MainWindowViewModel viewModel || !e.DataTransfer.Contains (DragFormats.Photos))
			return;
		var node = (e.Source as Control)?.DataContext as TagNode ?? viewModel.SelectedTag;
		viewModel.AttachSelectionToTag (node);
	}

	static string? TryGetString (IDataTransfer? transfer, DataFormat<string> format)
	{
		if (transfer is null || !transfer.Contains (format))
			return null;
		foreach (var item in transfer.GetItems (format)) {
			if (item.TryGetValue (format) is string value)
				return value;
		}

		return null;
	}

	void OnTagNodeTapped (object? sender, TappedEventArgs e)
	{
		if (DataContext is MainWindowViewModel viewModel
				&& sender is Control { DataContext: TagNode node }) {
			viewModel.SelectTagCommand.Execute (node);
			e.Handled = true;
		}
	}

	void OnTagNodeDoubleTapped (object? sender, TappedEventArgs e)
	{
		if (DataContext is MainWindowViewModel viewModel
				&& sender is Control { DataContext: TagNode node }) {
			viewModel.ActivateTagCommand.Execute (node);
			e.Handled = true;
		}
	}

	void OnFolderNodeTapped (object? sender, TappedEventArgs e)
	{
		if (DataContext is MainWindowViewModel viewModel
				&& sender is Control { DataContext: FolderNode node }) {
			viewModel.SelectFolderCommand.Execute (node);
			e.Handled = true;
		}
	}

	void OnFolderNodeDoubleTapped (object? sender, TappedEventArgs e)
	{
		if (DataContext is MainWindowViewModel viewModel
				&& sender is Control { DataContext: FolderNode node }) {
			viewModel.ActivateFolderCommand.Execute (node);
			e.Handled = true;
		}
	}

	void OnTagsTreePointerPressed (object? sender, PointerPressedEventArgs e)
	{
		if (e.Source is Control { DataContext: TagNode } || DataContext is not MainWindowViewModel viewModel)
			return;
		viewModel.SelectTagCommand.Execute (null);
	}

	void OnFoldersTreePointerPressed (object? sender, PointerPressedEventArgs e)
	{
		if (e.Source is Control { DataContext: FolderNode } || DataContext is not MainWindowViewModel viewModel)
			return;
		viewModel.SelectFolderCommand.Execute (null);
	}
}
