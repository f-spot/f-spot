using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

using FSpot;
using FSpot.Avalonia.Mail;
using FSpot.Avalonia.Printing;
using FSpot.Avalonia.Services;
using FSpot.Avalonia.ViewModels;
using FSpot.Core;
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
		var sidebarColumn = BodyGrid.ColumnDefinitions[0];
		sidebarColumn.Width = viewModel.SidebarLength;
		sidebarColumn.PropertyChanged += (_, e) => {
			if (e.Property == ColumnDefinition.WidthProperty && sidebarColumn.Width != viewModel.SidebarLength)
				viewModel.SidebarLength = sidebarColumn.Width;
		};
		viewModel.PropertyChanged += (_, e) => {
			if (e.PropertyName is nameof (MainWindowViewModel.ViewerInterpolation)
					or nameof (MainWindowViewModel.PreviewInterpolation))
				PreviewInterpolation.Apply (PreviewImage, viewModel.ViewerInterpolation);
			if (e.PropertyName == nameof (MainWindowViewModel.SidebarLength)
					&& sidebarColumn.Width != viewModel.SidebarLength)
				sidebarColumn.Width = viewModel.SidebarLength;
		};
		PreviewInterpolation.Apply (PreviewImage, viewModel.ViewerInterpolation);
		Opened += (_, _) => {
			ApplyUiScale ();
			EnsureMenuInAutomationTree ();
			GtkMnemonic.ApplyHotKey (ToolbarImportButton, FSpot.Resources.Lang.Strings.Import2);
			GtkMnemonic.ApplyHotKey (EmptyImportButton, FSpot.Resources.Lang.Strings.Import2);
		};
		ScalingChanged += (_, _) => ApplyUiScale ();
		viewModel.LoadCatalog ();
	}

	void ApplyUiScale ()
	{
		if (DataContext is MainWindowViewModel viewModel)
			viewModel.UiScale = RenderScaling;
	}

	protected override void OnKeyDown (KeyEventArgs e)
	{
		var focused = TopLevel.GetTopLevel (this)?.FocusManager?.GetFocusedElement () ?? e.Source;
		if (Input.RatingKey.From (e.Key, e.KeyModifiers, focused) is uint rating
				&& DataContext is MainWindowViewModel rated) {
			rated.RateFocused (rating);
			e.Handled = true;
			return;
		}
		if (e.Key == Key.T && Input.ShortcutGuards.AllowsUnadornedKey (focused)
				&& DataContext is MainWindowViewModel typing) {
			typing.OpenTagTypingCommand.Execute (null);
			Dispatcher.UIThread.Post (() => TagTypingBox?.Focus ());
			e.Handled = true;
			return;
		}
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
		string? text = await PromptAsync (Strings.Find, Strings.RatingRangePrompt, "0-5");
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
		string? text = await PromptAsync (Strings.Find, PromptCopy.DateRange, string.Empty);
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
		string hint = PromptCopy.RollHint (rolls);
		string? text = await PromptAsync (Strings.ImportRoll, hint, rolls.Count > 0 ? rolls[0].Id.ToString () : "1");
		return uint.TryParse (text, out uint id) ? id : null;
	}

	public Task PrintPhotosAsync (IReadOnlyList<string> paths, PrintPageSetup? setup = null)
		=> PrintWindowHost.ShowAsync (this, paths.Select (path => new PrintPhoto (path, Path.GetFileName (path), string.Empty, string.Empty, string.Empty, string.Empty)).ToList (), setup ?? PrintPageSetup.Letter);

	public Task<PrintPageSetup?> PrintPhotoItemsAsync (IReadOnlyList<IPhoto> photos, PrintPageSetup setup)
		=> PrintWindowHost.ShowAsync (this, PrintWindowHost.Collect (photos), setup);

	public async Task CopyPhotosAsync (IReadOnlyList<string> paths)
	{
		if (Clipboard is null || paths.Count == 0)
			return;
		var data = new DataTransfer ();
		data.Add (DataTransferItem.Create (DataFormat.Text, string.Join (Environment.NewLine, paths)));
		await Clipboard.SetDataAsync (data);
	}

	public Task SetDesktopBackgroundAsync (string path)
	{
		DesktopBackground.Set (path);
		return Task.CompletedTask;
	}

	public async Task<PrintPageSetup?> PromptPageSetupAsync (PrintPageSetup current)
	{
		var dialog = new PageSetupWindow (current);
		await dialog.ShowDialog (this);
		return dialog.Result;
	}

	public Task MailPhotosAsync (IReadOnlyList<string> paths)
		=> MailSender.SendAsync (this, paths);

	public void ShowSlideshow (IReadOnlyList<string> paths, int startIndex)
	{
		if (paths.Count == 0)
			return;
		var window = new SlideshowWindow (paths, startIndex);
		window.Show (this);
	}

	public void ToggleFullscreen ()
	{
		WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
		if (DataContext is MainWindowViewModel viewModel)
			viewModel.IsFullScreen = WindowState == WindowState.FullScreen;
	}

	public void FocusFind ()
		=> Dispatcher.UIThread.Post (() => FindBox.Focus ());

	public async Task ShowAboutAsync ()
		=> await new AboutWindow ().ShowDialog (this);

	public Task ShowWarningAsync (string title, string message)
		=> MessageDialog.ShowAsync (this, title, message, MessageDialogKind.Warning);

	public Task<bool> ShowImportAsync ()
		=> ShowImportAsync (Array.Empty<string> ());

	public async Task<bool> ShowImportAsync (IReadOnlyList<string> paths)
	{
		if (App.Database is null)
			return false;

		ImportWindow? window = null;
		var viewModel = new ImportViewModel (App.Database, this, new AvaloniaThumbnailLoader (), result => window?.Close (result));
		if (paths.Count > 0)
			viewModel.SeedDropped (paths);
		window = new ImportWindow (viewModel);
		return await window.ShowDialog<bool> (this);
	}

	public Task<string?> PromptAsync (string title, string prompt, string defaultValue)
		=> MessageDialog.PromptAsync (this, title, prompt, defaultValue);

	public Task<bool> ConfirmAsync (string title, string message, string okCaption)
		=> MessageDialog.ConfirmAsync (this, title, message, okCaption);

	public Task<string?> PickFolderAsync (string title)
		=> new FolderPickerWindow (title, BrowseFolderAsync).ShowDialog<string?> (this);

	async Task<string?> BrowseFolderAsync (string title)
	{
		var folders = await StorageProvider.OpenFolderPickerAsync (new FolderPickerOpenOptions {
			Title = title,
			AllowMultiple = false
		});
		return folders.Count == 0 ? null : folders[0].Path.LocalPath;
	}

	void EnsureMenuInAutomationTree ()
	{
		NativeMenuMirror.Fill (InWindowMenu, NativeMenu.GetMenu (this));
		if (InWindowMenu.Items.Count == 0)
			return;
		InWindowMenu.IsVisible = true;
		NativeMenuBar.IsVisible = false;
	}

	public void OpenUri (string uri)
	{
		if (string.IsNullOrWhiteSpace (uri))
			return;
		try {
			System.Diagnostics.Process.Start (new System.Diagnostics.ProcessStartInfo (uri) { UseShellExecute = true });
		} catch (Exception ex) {
			_ = ShowWarningAsync (Strings.Contents, ex.Message);
		}
	}

	public Task ShowPreferencesAsync ()
	{
		var window = new PreferencesWindow ();
		return window.ShowDialog (this);
	}

	public Task ShowExtensionsAsync (IList<AddinStatus> addins)
	{
		var window = new ExtensionsWindow (addins);
		return window.ShowDialog (this);
	}

	public async Task<TimeAdjustChoice?> PromptAdjustTimeAsync (IReadOnlyList<(string Name, DateTime Time, string Path)> photos)
	{
		var dialog = new AdjustTimeWindow (photos);
		await dialog.ShowDialog (this);
		return dialog.Choice;
	}

	public async Task<double?> PromptHoursOffsetAsync ()
	{
		string? text = await PromptAsync (Strings.AdjustTime, Strings.HoursOffset, "0");
		return double.TryParse (text, out double hours) ? hours : null;
	}
}
