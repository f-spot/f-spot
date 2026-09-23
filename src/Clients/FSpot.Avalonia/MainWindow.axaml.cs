using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;

using FSpot;
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
		viewModel.PropertyChanged += (_, e) => {
			if (e.PropertyName is nameof (MainWindowViewModel.ViewerInterpolation)
					or nameof (MainWindowViewModel.PreviewInterpolation))
				PreviewInterpolation.Apply (PreviewImage, viewModel.ViewerInterpolation);
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

	Point? panOrigin;

	void OnPreviewPointerMoved (object? sender, PointerEventArgs e)
	{
		if (DataContext is not MainWindowViewModel viewModel)
			return;
		var point = e.GetPosition (PreviewImage);
		if (panOrigin is Point origin) {
			viewModel.PanPreviewBy (point.X - origin.X, point.Y - origin.Y);
			panOrigin = point;
		}
		if (!viewModel.IsLoupeVisible)
			return;
		viewModel.MoveLoupe (point.X, point.Y, PreviewImage.Bounds.Width, PreviewImage.Bounds.Height);
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
	{
		string paper = setup?.Caption ?? string.Empty;
		string body = string.IsNullOrEmpty (paper)
			? string.Join (Environment.NewLine, paths)
			: paper + Environment.NewLine + string.Join (Environment.NewLine, paths);
		return ShowWarningAsync (Strings.Print, body);
	}

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
		string? paper = await PromptAsync (Strings.PageSetup2, Strings.PageSetupPaperPrompt,
			current.PaperWidthMm >= 210 && current.PaperWidthMm <= 211 ? "A4" : "Letter");
		if (string.IsNullOrWhiteSpace (paper))
			return null;
		string? orientation = await PromptAsync (Strings.PageSetup2, Strings.PageSetupOrientationPrompt,
			current.Landscape ? "Landscape" : "Portrait");
		return PrintPageSetup.FromPrompt (paper, orientation);
	}

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

	void OnPhotoDoubleTapped (object? sender, TappedEventArgs e)
	{
		if (DataContext is MainWindowViewModel viewModel
				&& (e.Source as Control)?.DataContext is PhotoItemViewModel item)
			viewModel.OpenPhotoCommand.Execute (item);
	}

	void OnTagPointerPressed (object? sender, PointerPressedEventArgs e)
	{
		if (sender is not Control { DataContext: TagNode node })
			return;
		if (e.GetCurrentPoint ((Visual)sender).Properties.IsRightButtonPressed) {
			if (DataContext is MainWindowViewModel viewModel)
				viewModel.SelectTagCommand.Execute (node);
			return;
		}
		if (!e.GetCurrentPoint ((Visual)sender).Properties.IsLeftButtonPressed)
			return;
		var data = new DataTransfer ();
		data.Add (DataTransferItem.Create (DragFormats.TagId, node.Tag.Id.ToString ()));
		_ = DragDrop.DoDragDropAsync (e, data, DragDropEffects.Copy);
	}

	bool filmstripChoosing;

	void OnFilmstripPointerPressed (object? sender, PointerPressedEventArgs e)
		=> filmstripChoosing = true;

	void OnFilmstripPointerReleased (object? sender, PointerReleasedEventArgs e)
		=> filmstripChoosing = false;

	void OnFilmstripKeyDown (object? sender, KeyEventArgs e)
		=> filmstripChoosing = true;

	void OnFilmstripKeyUp (object? sender, KeyEventArgs e)
		=> filmstripChoosing = false;

	void OnFilmstripSelectionChanged (object? sender, SelectionChangedEventArgs e)
	{
		if (!filmstripChoosing)
			return;
		if (DataContext is MainWindowViewModel viewModel
				&& sender is ListBox { SelectedItem: PhotoItemViewModel item })
			viewModel.SelectFromFilmstrip (item);
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
		=> e.DragEffects = AcceptsLibraryDrop (e.DataTransfer) ? DragDropEffects.Copy : DragDropEffects.None;

	void OnPhotoDrop (object? sender, DragEventArgs e)
		=> DropOnLibrary (e);

	void OnLibraryDragOver (object? sender, DragEventArgs e)
		=> e.DragEffects = e.DataTransfer.Contains (DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;

	void OnLibraryDrop (object? sender, DragEventArgs e)
		=> DropOnLibrary (e);

	static bool AcceptsLibraryDrop (IDataTransfer transfer)
		=> transfer.Contains (DataFormat.File) || transfer.Contains (DragFormats.TagId);

	void DropOnLibrary (DragEventArgs e)
	{
		if (DataContext is not MainWindowViewModel viewModel)
			return;
		var paths = DroppedPaths (e.DataTransfer);
		if (paths.Count > 0) {
			_ = viewModel.ImportDroppedAsync (paths);
			return;
		}
		var target = (e.Source as Control)?.DataContext as PhotoItemViewModel;
		viewModel.AttachDroppedTag (TryGetString (e.DataTransfer, DragFormats.TagId), target);
	}

	static List<string> DroppedPaths (IDataTransfer transfer)
	{
		var paths = new List<string> ();
		if (!transfer.Contains (DataFormat.File))
			return paths;
		foreach (var item in transfer.GetItems (DataFormat.File)) {
			if (item.TryGetValue (DataFormat.File) is not IStorageItem storage)
				continue;
			string? path = storage.TryGetLocalPath ();
			if (!string.IsNullOrWhiteSpace (path))
				paths.Add (path);
		}
		return paths;
	}

	void OnTagTreeDragOver (object? sender, DragEventArgs e)
	{
		e.DragEffects = e.DataTransfer.Contains (DragFormats.Photos) || e.DataTransfer.Contains (DragFormats.TagId)
			? DragDropEffects.Copy
			: DragDropEffects.None;
	}

	void OnTagTreeDrop (object? sender, DragEventArgs e)
	{
		if (DataContext is not MainWindowViewModel viewModel)
			return;
		var node = (e.Source as Control)?.DataContext as TagNode ?? viewModel.SelectedTag;
		if (e.DataTransfer.Contains (DragFormats.TagId) && node is not null) {
			string? id = TryGetString (e.DataTransfer, DragFormats.TagId);
			var child = id is null ? null : FindTagNodeById (viewModel.Tags, uint.Parse (id));
			if (child is not null && !ReferenceEquals (child, node))
				viewModel.ReparentTagCommand.Execute (((TagNode Child, TagNode Parent)?) (child, node));
			return;
		}
		if (e.DataTransfer.Contains (DragFormats.Photos))
			viewModel.AttachSelectionToTag (node);
	}

	static TagNode? FindTagNodeById (IEnumerable<TagNode> nodes, uint id)
	{
		foreach (var node in nodes) {
			if (node.Tag.Id == id)
				return node;
			var child = FindTagNodeById (node.Children, id);
			if (child is not null)
				return child;
		}
		return null;
	}

	void OnFindBarDragOver (object? sender, DragEventArgs e)
		=> e.DragEffects = e.DataTransfer.Contains (DragFormats.FolderUri) ? DragDropEffects.Copy : DragDropEffects.None;

	void OnTagRenameLostFocus (object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
	{
		if (DataContext is MainWindowViewModel viewModel && sender is Control { DataContext: TagNode node })
			viewModel.CommitRenameTagCommand.Execute (node);
	}

	void OnTagRenameKeyDown (object? sender, KeyEventArgs e)
	{
		if (e.Key != Key.Enter && e.Key != Key.Escape)
			return;
		if (DataContext is not MainWindowViewModel viewModel || sender is not Control { DataContext: TagNode node })
			return;
		if (e.Key == Key.Enter)
			viewModel.CommitRenameTagCommand.Execute (node);
		else {
			node.Name = node.Tag.Name;
			node.IsRenaming = false;
		}
		e.Handled = true;
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

	void OnFolderPointerPressed (object? sender, PointerPressedEventArgs e)
	{
		if (sender is not Control { DataContext: FolderNode node })
			return;
		if (e.GetCurrentPoint ((Visual)sender).Properties.IsRightButtonPressed) {
			if (DataContext is MainWindowViewModel viewModel)
				viewModel.SelectFolderCommand.Execute (node);
			return;
		}
		if (!e.GetCurrentPoint ((Visual)sender).Properties.IsLeftButtonPressed)
			return;
		var data = new DataTransfer ();
		data.Add (DataTransferItem.Create (DragFormats.FolderUri, node.Uri.AbsoluteUri));
		_ = DragDrop.DoDragDropAsync (e, data, DragDropEffects.Copy);
	}

	void OnFindBarDrop (object? sender, DragEventArgs e)
	{
		if (DataContext is not MainWindowViewModel viewModel)
			return;
		string? uri = TryGetString (e.DataTransfer, DragFormats.FolderUri);
		if (string.IsNullOrEmpty (uri))
			return;
		var match = viewModel.Folders.SelectMany (WalkFolders).FirstOrDefault (folder => folder.Uri.AbsoluteUri == uri);
		if (match is not null)
			viewModel.ActivateFolderCommand.Execute (match);
	}

	static IEnumerable<FolderNode> WalkFolders (FolderNode node)
	{
		yield return node;
		foreach (var child in node.Children)
			foreach (var nested in WalkFolders (child))
				yield return nested;
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
		if (DataContext is not MainWindowViewModel viewModel)
			return;
		if (FindAncestorDataContext<TagNode> (e.Source as Visual) is not null)
			return;
		viewModel.SelectTagCommand.Execute (null);
	}

	void OnFoldersTreePointerPressed (object? sender, PointerPressedEventArgs e)
	{
		if (DataContext is not MainWindowViewModel viewModel)
			return;
		if (FindAncestorDataContext<FolderNode> (e.Source as Visual) is not null)
			return;
		viewModel.SelectFolderCommand.Execute (null);
	}

	static T? FindAncestorDataContext<T> (Visual? start) where T : class
	{
		for (var visual = start; visual is not null; visual = visual.GetVisualParent ()) {
			if (visual is Control { DataContext: T value })
				return value;
		}
		return null;
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

	public async Task<double?> PromptHoursOffsetAsync ()
	{
		string? text = await PromptAsync (Strings.AdjustTime, Strings.HoursOffset, "0");
		return double.TryParse (text, out double hours) ? hours : null;
	}

	void OnPreviewPointerWheelChanged (object? sender, PointerWheelEventArgs e)
	{
		if (DataContext is not MainWindowViewModel viewModel)
			return;
		viewModel.ZoomPreviewCommand.Execute (e.Delta.Y > 0 ? 1.1 : 0.9);
		e.Handled = true;
	}

	void OnPreviewPointerPressed (object? sender, PointerPressedEventArgs e)
	{
		if (e.KeyModifiers.HasFlag (KeyModifiers.Control) && e.GetCurrentPoint (PreviewImage).Properties.IsLeftButtonPressed) {
			panOrigin = e.GetPosition (PreviewImage);
			e.Pointer.Capture (PreviewImage);
			e.Handled = true;
		}
	}

	void OnPreviewPointerReleased (object? sender, PointerReleasedEventArgs e)
	{
		if (panOrigin is null)
			return;
		panOrigin = null;
		e.Pointer.Capture (null);
	}
}
