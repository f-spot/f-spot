// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.ComponentModel;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

using FSpot.Avalonia.Imaging;
using FSpot.Avalonia.Services;
using FSpot.Avalonia.ViewModels;

namespace FSpot.Avalonia;

// The GTK SingleView window: a menu, a toolbar, the folder tray in a side pane, one
// zoomable photo and a status line with the zoom slider.
public partial class ViewerWindow : Window, IViewerShell
{
	const int TrayThumbnailSize = 128;
	const double SidebarWidth = 189;

	readonly ViewerViewModel viewModel;
	readonly IPhotoThumbnailLoader thumbnails = new AvaloniaThumbnailLoader ();
	readonly CancellationTokenSource closing = new ();
	ViewerItemViewModel? shownItem;
	Bitmap? photo;
	int photoLoad;
	GridLength sidebarLength = new (SidebarWidth);

	public ViewerWindow () : this (Array.Empty<string> ())
	{
	}

	public ViewerWindow (IReadOnlyList<string> paths, Func<string, bool>? canLoad = null)
	{
		viewModel = new ViewerViewModel (this, canLoad);
		DataContext = viewModel;
		InitializeComponent ();

		var (width, height) = ViewerSettings.Size;
		if (width > 0 && height > 0) {
			Width = width;
			Height = height;
		}
		if (ViewerSettings.Maximized)
			WindowState = WindowState.Maximized;

		viewModel.Items.CollectionChanged += (_, e) => {
			if (e.NewItems is not null) {
				foreach (ViewerItemViewModel item in e.NewItems)
					Watch (item);
			}
		};
		viewModel.PropertyChanged += OnViewModelChanged;
		ViewerSettings.Changed += OnViewerSettingsChanged;
		ImageScroller.SizeChanged += (_, _) => UpdateFit ();
		AddHandler (DragDrop.DragOverEvent, OnDragOver);
		AddHandler (DragDrop.DropEvent, OnDrop);
		Closing += (_, _) => SavePreferences ();
		Closed += (_, _) => {
			ViewerSettings.Changed -= OnViewerSettingsChanged;
			closing.Cancel ();
			ShowPhoto (null);
		};

		ApplyViewerSettings ();
		viewModel.Initialize (paths);
		ApplySidebarColumn ();
	}

	public ViewerViewModel ViewModel => viewModel;

	void Watch (ViewerItemViewModel item)
	{
		item.PropertyChanged += (_, e) => {
			if (e.PropertyName != nameof (ViewerItemViewModel.Revision))
				return;
			_ = LoadThumbnailAsync (item);
			if (ReferenceEquals (item, viewModel.CurrentItem))
				_ = LoadPhotoAsync (item);
		};
		_ = LoadThumbnailAsync (item);
	}

	async Task LoadThumbnailAsync (ViewerItemViewModel item)
	{
		try {
			var bitmap = await thumbnails.LoadAsync (item.Path, TrayThumbnailSize, closing.Token);
			var previous = item.Thumbnail;
			item.Thumbnail = bitmap;
			ReleasedBitmap.DisposePrevious (previous, bitmap);
		} catch (OperationCanceledException) {
		} catch (Exception ex) {
			Logger.Log.Debug (ex, "Cannot load a tray thumbnail for {Path}", item.Path);
		}
	}

	void OnViewModelChanged (object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName) {
		case nameof (ViewerViewModel.CurrentItem):
			if (!ReferenceEquals (shownItem, viewModel.CurrentItem))
				_ = LoadPhotoAsync (viewModel.CurrentItem);
			break;
		case nameof (ViewerViewModel.Zoom):
			ApplyZoom ();
			break;
		case nameof (ViewerViewModel.SidebarVisible):
			ApplySidebarColumn ();
			break;
		case nameof (ViewerViewModel.SelectedIndex):
			if (viewModel.SelectedIndex >= 0)
				FolderTray.ScrollIntoView (viewModel.SelectedIndex);
			break;
		}
	}

	async Task LoadPhotoAsync (ViewerItemViewModel? item)
	{
		shownItem = item;
		int load = ++photoLoad;
		if (item is null) {
			ShowPhoto (null);
			return;
		}

		Bitmap? next = null;
		try {
			next = await Task.Run (() => AvaloniaPhotoDecoder.DecodeFile (item.Path, EditorPreview.MaxPixels), closing.Token);
		} catch (OperationCanceledException) {
			return;
		} catch (Exception ex) {
			Logger.Log.Debug (ex, "Cannot load {Path}", item.Path);
		}

		if (load != photoLoad || closing.IsCancellationRequested) {
			next?.Dispose ();
			return;
		}
		ShowPhoto (next);
		viewModel.ResetZoom (FitZoom ());
		ApplyZoom ();
	}

	void ShowPhoto (Bitmap? next)
	{
		PhotoImage.Source = next;
		var previous = photo;
		photo = next;
		ReleasedBitmap.DisposePrevious (previous, next);
	}

	double FitZoom ()
	{
		if (photo is null)
			return ViewerZoom.FallbackMin;
		var viewport = ImageScroller.Bounds.Size;
		return ViewerZoom.MinFor (photo.Size.Width, photo.Size.Height, viewport.Width, viewport.Height);
	}

	void UpdateFit ()
	{
		if (photo is null)
			return;
		viewModel.UpdateMinZoom (FitZoom ());
		ApplyZoom ();
	}

	void ApplyZoom ()
	{
		if (photo is null) {
			ImageFrame.Width = ImageFrame.Height = double.NaN;
			return;
		}
		ImageFrame.Width = photo.Size.Width * viewModel.Zoom;
		ImageFrame.Height = photo.Size.Height * viewModel.Zoom;
	}

	// The side pane keeps the width it was dragged to while hidden, as a GtkPaned does.
	void ApplySidebarColumn ()
	{
		var column = BodyGrid.ColumnDefinitions[0];
		if (viewModel.SidebarVisible) {
			column.Width = sidebarLength;
		} else {
			if (column.Width.Value > 0)
				sidebarLength = column.Width;
			column.Width = new GridLength (0);
		}
	}

	void OnViewerSettingsChanged (object? sender, EventArgs e)
		=> ApplyViewerSettings ();

	// LoadPreference for ViewerInterpolation, ViewerTransparency and ViewerTransColor.
	void ApplyViewerSettings ()
	{
		PreviewInterpolation.Apply (PhotoImage, ViewerSettings.Interpolation);
		ImageFrame.Background = TransparencyBrush (ViewerSettings.Transparency, ViewerSettings.TransparentColor);
	}

	public static IBrush? TransparencyBrush (ViewerTransparency mode, string color)
	{
		switch (mode) {
		case ViewerTransparency.CheckPattern:
			return CheckPatternBrush ();
		case ViewerTransparency.CustomColor:
			ViewerSettings.TryParseColor (color, out byte r, out byte g, out byte b);
			return new SolidColorBrush (Color.FromRgb (r, g, b));
		default:
			return null;
		}
	}

	// CheckPattern.Dark: 8 pixel checks of black and #555555.
	static DrawingBrush CheckPatternBrush ()
	{
		const double check = 8;
		var dark = new SolidColorBrush (Color.FromRgb (0x00, 0x00, 0x00));
		var light = new SolidColorBrush (Color.FromRgb (0x55, 0x55, 0x55));
		var group = new DrawingGroup ();
		group.Children.Add (new GeometryDrawing { Brush = dark, Geometry = new RectangleGeometry (new Rect (0, 0, check * 2, check * 2)) });
		group.Children.Add (new GeometryDrawing { Brush = light, Geometry = new RectangleGeometry (new Rect (check, 0, check, check)) });
		group.Children.Add (new GeometryDrawing { Brush = light, Geometry = new RectangleGeometry (new Rect (0, check, check, check)) });
		return new DrawingBrush (group) {
			TileMode = TileMode.Tile,
			DestinationRect = new RelativeRect (0, 0, check * 2, check * 2, RelativeUnit.Absolute)
		};
	}

	void SavePreferences ()
	{
		bool maximized = WindowState == WindowState.Maximized;
		viewModel.SavePreferences ((int)Bounds.Width, (int)Bounds.Height, maximized);
	}

	// HandleDragDataReceived: files dropped from outside F-Spot are added to the window;
	// drags that start inside F-Spot are ignored.
	static bool AcceptsDrop (IDataTransfer transfer)
		=> transfer.Contains (DataFormat.File) && !transfer.Contains (DragFormats.Photos);

	void OnDragOver (object? sender, DragEventArgs e)
		=> e.DragEffects = AcceptsDrop (e.DataTransfer) ? DragDropEffects.Copy : DragDropEffects.None;

	void OnDrop (object? sender, DragEventArgs e)
	{
		if (!AcceptsDrop (e.DataTransfer))
			return;
		var paths = new List<string> ();
		foreach (var item in e.DataTransfer.GetItems (DataFormat.File)) {
			if (item.TryGetValue (DataFormat.File) is IStorageItem storage && storage.TryGetLocalPath () is { Length: > 0 } path)
				paths.Add (path);
		}
		viewModel.LoadPaths (paths);
		e.Handled = true;
	}

	protected override void OnKeyDown (KeyEventArgs e)
	{
		if (e.Key == Key.Escape && viewModel.IsFullScreen) {
			ToggleFullscreen ();
			e.Handled = true;
			return;
		}
		base.OnKeyDown (e);
	}

	public void OpenNewWindow (IReadOnlyList<string> paths)
		=> new ViewerWindow (paths).Show ();

	public async Task<IReadOnlyList<string>> PickFilesAsync (string title, bool allowMultiple)
	{
		var files = await StorageProvider.OpenFilePickerAsync (new FilePickerOpenOptions {
			Title = title,
			AllowMultiple = allowMultiple,
			SuggestedStartLocation = await StartLocationAsync ()
		});
		return files.Select (file => file.TryGetLocalPath ()).OfType<string> ().ToList ();
	}

	public async Task<string?> PickFolderAsync (string title)
	{
		var folders = await StorageProvider.OpenFolderPickerAsync (new FolderPickerOpenOptions {
			Title = title,
			AllowMultiple = false,
			SuggestedStartLocation = await StartLocationAsync ()
		});
		return folders.Count == 0 ? null : folders[0].TryGetLocalPath ();
	}

	// chooser.SetUri (uri): the choosers open where the window's photos are.
	async Task<IStorageFolder?> StartLocationAsync ()
	{
		string? current = viewModel.CurrentUri;
		if (string.IsNullOrEmpty (current))
			return null;
		string? folder = Directory.Exists (current) ? current : Path.GetDirectoryName (current);
		if (string.IsNullOrEmpty (folder))
			return null;
		try {
			return await StorageProvider.TryGetFolderFromPathAsync (folder);
		} catch (Exception ex) {
			Logger.Log.Debug (ex, "Cannot open {Folder} in the chooser", folder);
			return null;
		}
	}

	public void ToggleFullscreen ()
	{
		WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
		viewModel.IsFullScreen = WindowState == WindowState.FullScreen;
		Dispatcher.UIThread.Post (UpdateFit, DispatcherPriority.Background);
	}

	public void ShowSlideshow (IReadOnlyList<string> paths, int startIndex)
	{
		if (paths.Count == 0)
			return;
		new SlideshowWindow (paths, startIndex).Show (this);
	}

	public Task ShowPreferencesAsync ()
	{
		ViewerPreferencesWindow.ShowUp (this);
		return Task.CompletedTask;
	}

	public Task ShowAboutAsync ()
		=> new AboutWindow ().ShowDialog (this);

	public Task ShowWarningAsync (string title, string message)
		=> MessageDialog.ShowAsync (this, title, message, MessageDialogKind.Warning);

	public Task SetDesktopBackgroundAsync (string path)
	{
		DesktopBackground.Set (path);
		return Task.CompletedTask;
	}
}
