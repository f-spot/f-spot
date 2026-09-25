// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.IO;

using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FSpot.Avalonia.Services;
using FSpot.Core;
using FSpot.Imaging;
using FSpot.Resources.Lang;
using FSpot.Thumbnail;

using Hyena;

using TinyIoC;

namespace FSpot.Avalonia.ViewModels;

public partial class ViewerItemViewModel : ObservableObject
{
	[ObservableProperty]
	Bitmap? thumbnail;

	[ObservableProperty]
	int revision;

	public ViewerItemViewModel (string path)
	{
		Path = path;
		FileName = System.IO.Path.GetFileName (path);
	}

	public string Path { get; }
	public string FileName { get; }
}

// The GTK SingleView: a window over a loose list of files (its UriCollection), with a
// folder tray, one photo view and no catalog behind it.
public partial class ViewerViewModel : ObservableObject
{
	readonly IViewerShell shell;
	readonly Func<string, bool> canLoad;
	string? uri;

	[ObservableProperty]
	[NotifyPropertyChangedFor (nameof (CurrentItem))]
	[NotifyCanExecuteChangedFor (nameof (RotateLeftCommand), nameof (RotateRightCommand), nameof (SetAsBackgroundCommand), nameof (ExportCommand))]
	int selectedIndex = -1;

	[ObservableProperty]
	[NotifyPropertyChangedFor (nameof (SidebarVisible))]
	bool showSidebar;

	[ObservableProperty]
	[NotifyPropertyChangedFor (nameof (ToolbarVisible))]
	bool showToolbar = true;

	[ObservableProperty]
	bool displayFileNames = true;

	[ObservableProperty]
	string statusText = string.Empty;

	[ObservableProperty]
	[NotifyPropertyChangedFor (nameof (NormalizedZoom))]
	double zoom = 1.0;

	[ObservableProperty]
	[NotifyPropertyChangedFor (nameof (NormalizedZoom))]
	double minZoom = ViewerZoom.FallbackMin;

	[ObservableProperty]
	[NotifyPropertyChangedFor (nameof (SidebarVisible), nameof (ToolbarVisible))]
	bool isFullScreen;

	// Fullscreen shows the photo alone, as the GTK FullScreenView did.
	public bool SidebarVisible => ShowSidebar && !IsFullScreen;
	public bool ToolbarVisible => ShowToolbar && !IsFullScreen;

	public ViewerViewModel (IViewerShell shell, Func<string, bool>? canLoad = null)
	{
		this.shell = shell;
		this.canLoad = canLoad ?? DefaultCanLoad;
		Items.CollectionChanged += (_, _) => OnItemsChanged ();
	}

	public ObservableCollection<ViewerItemViewModel> Items { get; } = new ();

	public ViewerItemViewModel? CurrentItem
		=> SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;

	public bool HasItems => Items.Count > 0;

	public string? CurrentUri => uri;

	public double NormalizedZoom {
		get => ViewerZoom.Normalize (Zoom, MinZoom);
		set {
			double zoom = ViewerZoom.FromNormalized (value, MinZoom);
			if (Math.Abs (zoom - Zoom) > 1e-9)
				Zoom = zoom;
		}
	}

	// The constructor half of SingleView: the first uri is remembered for New Window and
	// the file choosers, the side pane opens only when there is more than one photo, and
	// the toolbar and file-name toggles come from the Viewer* preferences.
	public void Initialize (IReadOnlyList<string> paths)
	{
		uri = paths.Count > 0 ? paths[0] : null;
		ShowToolbar = ViewerSettings.ShowToolbar;
		LoadPaths (paths);
		ShowSidebar = Items.Count > 1;
		DisplayFileNames = ViewerSettings.ShowFilenames;
		UpdateStatus ();
	}

	// UriCollection.LoadItems: a loadable file is added, a folder contributes the
	// loadable files directly inside it, anything else is skipped.
	public void LoadPaths (IEnumerable<string> paths)
	{
		foreach (string path in ExpandPaths (paths, canLoad))
			Items.Add (new ViewerItemViewModel (path));
	}

	public static IReadOnlyList<string> ExpandPaths (IEnumerable<string> paths, Func<string, bool> canLoad)
	{
		var result = new List<string> ();
		foreach (string raw in paths) {
			string? path = LocalPath (raw);
			if (path is null)
				continue;
			if (Directory.Exists (path)) {
				IEnumerable<string> files;
				try {
					files = Directory.EnumerateFiles (path)
						.Order (StringComparer.OrdinalIgnoreCase)
						.ToList ();
				} catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
					Logger.Log.Debug (ex, "Cannot list {Path}", path);
					continue;
				}
				result.AddRange (files.Where (canLoad));
				continue;
			}
			if (File.Exists (path) && canLoad (path))
				result.Add (path);
		}
		return result;
	}

	static string? LocalPath (string? raw)
	{
		if (string.IsNullOrWhiteSpace (raw))
			return null;
		if (Uri.TryCreate (raw, UriKind.Absolute, out var parsed) && parsed.IsFile)
			return parsed.LocalPath;
		return raw.Contains ("://", StringComparison.Ordinal) ? null : Path.GetFullPath (raw);
	}

	static bool DefaultCanLoad (string path)
	{
		try {
			return TinyIoCContainer.Current.Resolve<IImageFileFactory> ().HasLoader (new SafeUri (path));
		} catch (Exception ex) {
			Logger.Log.Debug (ex, "Cannot classify {Path}", path);
			return false;
		}
	}

	// SingleView.CurrentUri: Open and Open Folder replace what the window shows.
	public void Replace (string path)
	{
		uri = path;
		SelectedIndex = -1;
		Items.Clear ();
		LoadPaths (new[] { path });
	}

	// HandleCollectionChanged: select the first photo when nothing is, open the side
	// pane once there is more than one, and refresh the status label.
	void OnItemsChanged ()
	{
		if (Items.Count > 0 && (SelectedIndex < 0 || SelectedIndex >= Items.Count))
			SelectedIndex = 0;
		else if (Items.Count == 0)
			SelectedIndex = -1;
		if (Items.Count > 1)
			ShowSidebar = true;
		OnPropertyChanged (nameof (HasItems));
		OnPropertyChanged (nameof (CurrentItem));
		RotateLeftCommand.NotifyCanExecuteChanged ();
		RotateRightCommand.NotifyCanExecuteChanged ();
		UpdateStatus ();
	}

	partial void OnSelectedIndexChanged (int value)
		=> UpdateStatus ();

	partial void OnDisplayFileNamesChanged (bool value)
		=> UpdateStatus ();

	public void UpdateStatus ()
		=> StatusText = FormatStatus (DisplayFileNames ? CurrentItem?.Path : null, Items.Count);

	// UpdateStatusLabel: "name  -  N photos" with file names shown, "N photos" without.
	public static string FormatStatus (string? currentPath, int count)
	{
		string photos = string.Format (count <= 1 ? Strings.XPhoto : Strings.XPhotos, count);
		return currentPath is null ? photos : Path.GetFileName (currentPath) + "  -  " + photos;
	}

	// The fit-to-window zoom for the photo just shown; a new photo starts fitted, as
	// the GTK ImageView reset to MIN_ZOOM on every pixbuf change.
	public void ResetZoom (double min)
	{
		MinZoom = min;
		Zoom = min;
	}

	public void UpdateMinZoom (double min)
	{
		bool fitted = Math.Abs (Zoom - MinZoom) < 1e-9;
		MinZoom = min;
		Zoom = fitted ? min : ViewerZoom.Clamp (Zoom, min);
	}

	[RelayCommand]
	void ZoomIn ()
		=> Zoom = ViewerZoom.Clamp (Zoom * ViewerZoom.Factor, MinZoom);

	[RelayCommand]
	void ZoomOut ()
		=> Zoom = ViewerZoom.Clamp (Zoom / ViewerZoom.Factor, MinZoom);

	[RelayCommand]
	void NewWindow ()
		=> shell.OpenNewWindow (uri is null ? Array.Empty<string> () : new[] { uri });

	[RelayCommand]
	async Task OpenAsync ()
	{
		var files = await shell.PickFilesAsync (Strings.Open, false);
		if (files.Count > 0)
			Replace (files[0]);
	}

	[RelayCommand]
	async Task OpenFolderAsync ()
	{
		string? folder = await shell.PickFolderAsync (Strings.SelectFolder);
		if (!string.IsNullOrWhiteSpace (folder))
			Replace (folder);
	}

	[RelayCommand (CanExecute = nameof (HasCurrent))]
	async Task ExportAsync ()
	{
		if (CurrentItem is not { } item)
			return;
		string? folder = await shell.PickFolderAsync (Strings.SelectExportFolder);
		if (string.IsNullOrWhiteSpace (folder))
			return;
		PhotoFileActions.CopyToFolder (new[] { item.Path }, folder);
	}

	[RelayCommand]
	void CloseWindow ()
		=> shell.Close ();

	bool HasCurrent () => CurrentItem is not null;

	[RelayCommand (CanExecute = nameof (HasCurrent))]
	Task RotateLeftAsync ()
		=> RotateAsync (RotateDirection.Counterclockwise);

	[RelayCommand (CanExecute = nameof (HasCurrent))]
	Task RotateRightAsync ()
		=> RotateAsync (RotateDirection.Clockwise);

	async Task RotateAsync (RotateDirection direction)
	{
		if (CurrentItem is not { } item)
			return;
		try {
			var photo = new FilePhoto (new SafeUri (item.Path));
			var op = new RotateMultiple (new IPhoto[] { photo }, direction,
				TinyIoCContainer.Current.Resolve<IThumbnailService> ());
			while (op.Step ()) { }
		} catch (Exception ex) {
			Logger.Log.Debug (ex, "Failed to rotate {Path}", item.Path);
			await shell.ShowWarningAsync (Strings.ErrorWhileRotatingPhoto, ex.Message);
			return;
		}
		// MarkChanged (FullInvalidate): the photo and its tray thumbnail are reloaded.
		item.Revision++;
	}

	[RelayCommand (CanExecute = nameof (HasCurrent))]
	Task SetAsBackgroundAsync ()
		=> CurrentItem is { } item ? shell.SetDesktopBackgroundAsync (item.Path) : Task.CompletedTask;

	[RelayCommand]
	Task PreferencesAsync ()
		=> shell.ShowPreferencesAsync ();

	[RelayCommand]
	void Fullscreen ()
		=> shell.ToggleFullscreen ();

	[RelayCommand]
	void Slideshow ()
	{
		if (Items.Count == 0)
			return;
		shell.ShowSlideshow (Items.Select (item => item.Path).ToList (), Math.Max (0, SelectedIndex));
	}

	[RelayCommand]
	void ToggleToolbar ()
		=> ShowToolbar = !ShowToolbar;

	[RelayCommand]
	void ToggleSidebar ()
		=> ShowSidebar = !ShowSidebar;

	[RelayCommand]
	void ToggleFileNames ()
		=> DisplayFileNames = !DisplayFileNames;

	[RelayCommand]
	void HideSidebar ()
		=> ShowSidebar = false;

	[RelayCommand]
	Task AboutAsync ()
		=> shell.ShowAboutAsync ();

	// SavePreferences: the size is only kept while the window is not maximized.
	public void SavePreferences (int width, int height, bool maximized)
	{
		ViewerSettings.Maximized = maximized;
		if (!maximized)
			ViewerSettings.Size = (width, height);
		ViewerSettings.ShowToolbar = ShowToolbar;
		ViewerSettings.ShowFilenames = DisplayFileNames;
	}
}
