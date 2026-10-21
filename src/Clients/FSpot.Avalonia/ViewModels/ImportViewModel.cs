//
// ImportViewModel.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.IO;

using Avalonia.Media.Imaging;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FSpot.Avalonia.Imaging;
using FSpot.Avalonia.Services;
using FSpot.Core;
using FSpot.Database;
using FSpot.FileSystem;
using FSpot.Imaging;
using FSpot.Import;
using FSpot.Resources.Lang;
using FSpot.Utils;

using Hyena;

namespace FSpot.Avalonia.ViewModels;

public partial class ImportViewModel : ObservableObject
{
	readonly Db database;
	readonly IAvaloniaShell shell;
	readonly IPhotoThumbnailLoader thumbnails;
	readonly ThumbnailLease shownThumbnails = new ();
	readonly ThumbnailLoads thumbnailLoads = new ();
	readonly Dictionary<PhotoItemViewModel, int> thumbnailGeneration = new ();
	readonly Action<bool> close;
	readonly IFileSystem? fileSystem;
	readonly IImageFileFactory? images;
	readonly Func<IImportController>? importFactory;
	readonly PhotoList importList = new PhotoList ();
	CancellationTokenSource? scan;
	Task? scanTask;
	CancellationTokenSource? import;
	CancellationTokenSource previewLoad = new ();
	bool importAccepted;
	IReadOnlyList<string>? droppedPaths;
	bool closed;

	internal Func<IImportController>? Importer { get; set; }

	// Tests hold the scan open. Production scans go through the file source.
	internal Func<CancellationToken, IEnumerable<IPhoto>>? ScanOverride { get; set; }

	[ObservableProperty]
	string sourceLabel = Strings.ChooseFolder;

	[ObservableProperty]
	string? sourcePath;

	[ObservableProperty]
	ImportSourceOption? selectedSourceOption;

	bool suppressSourceSelection;

	[ObservableProperty]
	PhotoItemViewModel? selectedPhoto;

	[ObservableProperty]
	bool copyFiles;

	[ObservableProperty]
	bool recurseSubdirectories;

	[ObservableProperty]
	bool duplicateDetect;

	[ObservableProperty]
	bool mergeRawAndJpeg;

	[ObservableProperty]
	bool removeOriginals;

	[ObservableProperty]
	string attachTagsText = string.Empty;

	[ObservableProperty]
	string statusText = string.Empty;

	[ObservableProperty]
	double progress;

	[ObservableProperty]
	bool isBusy;

	[ObservableProperty]
	bool isScanning;

	[ObservableProperty]
	double uiScale = 1;

	[ObservableProperty]
	Bitmap? preview;

	public ImportViewModel (Db database, IAvaloniaShell shell, IPhotoThumbnailLoader thumbnails, Action<bool> close, IFileSystem? fileSystem = null, IImageFileFactory? images = null, Func<IImportController>? import = null)
	{
		this.database = database;
		this.shell = shell;
		this.thumbnails = thumbnails;
		this.close = close;
		this.fileSystem = fileSystem;
		this.images = images;
		this.importFactory = import;
		Preferences = new ImportPreferences (true);
		CopyFiles = Preferences.CopyFiles;
		RecurseSubdirectories = Preferences.RecurseSubdirectories;
		DuplicateDetect = Preferences.DuplicateDetect;
		MergeRawAndJpeg = Preferences.MergeRawAndJpeg;
		RemoveOriginals = Preferences.RemoveOriginals;
		RefreshSourceOptions ();
	}

	public ImportPreferences Preferences { get; }
	public ObservableCollection<PhotoItemViewModel> Photos { get; } = [];
	public ObservableCollection<ImportSourceOption> SourceOptions { get; } = [];
	public bool CanImport => Photos.Count > 0 && !IsBusy;
	public bool RemoveOriginalsEnabled => CopyFiles;
	public bool CanEditRemoveOriginals => RemoveOriginalsEnabled && !IsBusy;
	public bool IsProgressVisible => IsBusy || IsScanning;
	public bool HasDetectedSources { get; private set; }

	partial void OnIsBusyChanged (bool value)
	{
		OnPropertyChanged (nameof (IsProgressVisible));
		OnPropertyChanged (nameof (CanEditRemoveOriginals));
	}

	partial void OnIsScanningChanged (bool value)
		=> OnPropertyChanged (nameof (IsProgressVisible));

	partial void OnCopyFilesChanged (bool value)
	{
		Preferences.CopyFiles = value;
		if (!value)
			RemoveOriginals = false;
		OnPropertyChanged (nameof (RemoveOriginalsEnabled));
		OnPropertyChanged (nameof (CanEditRemoveOriginals));
	}

	partial void OnRecurseSubdirectoriesChanged (bool value)
	{
		Preferences.RecurseSubdirectories = value;
		if (!IsBusy && SourcePath is not null)
			UiTasks.Forget (ScanAsync ());
	}

	partial void OnDuplicateDetectChanged (bool value)
		=> Preferences.DuplicateDetect = value;

	partial void OnMergeRawAndJpegChanged (bool value)
	{
		Preferences.MergeRawAndJpeg = value;
		if (!IsBusy && SourcePath is not null)
			UiTasks.Forget (ScanAsync ());
	}

	partial void OnRemoveOriginalsChanged (bool value)
		=> Preferences.RemoveOriginals = value;

	partial void OnSelectedPhotoChanged (PhotoItemViewModel? value)
	{
		if (value is not null)
			UiTasks.Forget (LoadPreviewAsync (value));
		else
			SetPreview (null);
	}

	public void SeedDropped (IReadOnlyList<string> paths)
	{
		droppedPaths = paths;
		SourcePath = paths.Count == 1 ? paths[0] : null;
		SourceLabel = paths.Count == 1
			? Path.GetFileName (paths[0].TrimEnd (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
			: string.Format (Strings.XPhotos, paths.Count);

		// The dropped files/folders aren't one of the picker's regular
		// sources; show them as a one-off entry so the combo still reflects
		// what's about to be scanned.
		suppressSourceSelection = true;
		var dropped = ImportSourceOption.ForSource (SourceLabel, SourcePath ?? string.Empty);
		SourceOptions.Insert (0, dropped);
		SelectedSourceOption = dropped;
		suppressSourceSelection = false;

		UiTasks.Forget (ScanAsync ());
	}

	// Rebuilds the "Import from:" picker: "Choose Folder...", then any
	// detected camera/card/disk, then folders picked earlier this session.
	// Matches ImportDialog.PopulateSourceCombo in FSpot.Gtk.
	void RefreshSourceOptions (ImportSourceOption? toSelect = null)
	{
		suppressSourceSelection = true;
		try {
			SourceOptions.Clear ();
			var chooseFolder = ImportSourceOption.ChooseFolder (Strings.ChooseFolder);
			SourceOptions.Add (chooseFolder);

			var detected = fileSystem is null ? [] : DeviceImport.Sources (fileSystem);
			HasDetectedSources = detected.Count > 0;
			foreach (var source in detected)
				SourceOptions.Add (ImportSourceOption.ForSource (source.Name, source.Root.LocalPath));

			foreach (var history in ImportSourceHistory.All)
				SourceOptions.Add (history);

			OnPropertyChanged (nameof (HasDetectedSources));
			SelectedSourceOption = toSelect ?? chooseFolder;
		} finally {
			suppressSourceSelection = false;
		}
	}

	partial void OnSelectedSourceOptionChanged (ImportSourceOption? value)
	{
		if (suppressSourceSelection || value is null || IsBusy || importAccepted)
			return;

		if (value.Kind == ImportSourceKind.ChooseFolder) {
			UiTasks.Forget (ChooseFolderAsync ());
			return;
		}

		droppedPaths = null;
		SourcePath = value.Path;
		SourceLabel = value.Label;
		UiTasks.Forget (ScanAsync ());
	}

	[RelayCommand]
	async Task ChooseFolderAsync ()
	{
		if (IsBusy || importAccepted)
			return;

		string? path;
		try {
			path = await shell.PickFolderAsync (Strings.ChooseFolder);
		} catch (Exception ex) {
			Logger.Log.Error (ex, "Failed to choose an import folder");
			return;
		}
		if (path is null) {
			// Nothing was picked; fall back to whatever source was active
			// rather than leaving "Choose Folder..." selected with no source.
			RefreshSourceOptions (SourceOptions.FirstOrDefault (option => option.Path == SourcePath));
			return;
		}

		string label = Path.GetFileName (path.TrimEnd (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
		var option = ImportSourceHistory.Remember (label, path);
		RefreshSourceOptions (option);
		SourcePath = path;
		SourceLabel = label;
		await ScanAsync ();
	}

	[RelayCommand (CanExecute = nameof (CanImport))]
	async Task ImportAsync ()
	{
		// Finish the scan first. A click while photos are still arriving used
		// to import only the files found so far. A later source change must
		// not replace that set, and the status must stop offering Import.
		if (importAccepted)
			return;
		importAccepted = true;
		StatusText = Strings.ImportingPhotos;
		while (scanTask is Task pending && !pending.IsCompleted) {
			try {
				await pending;
			} catch (OperationCanceledException) {
			}
		}

		if (closed || Photos.Count == 0) {
			importAccepted = false;
			if (!closed)
				StatusText = string.Format (Photos.Count <= 1 ? Strings.XPhoto : Strings.XPhotos, Photos.Count);
			return;
		}

		// The scan keeps appending to importList on the UI thread. DoImport
		// enumerates on a pool thread, so it must see a copy.
		var snapshot = new PhotoList (importList.Items.ToArray ());
		if (snapshot.Count == 0) {
			importAccepted = false;
			return;
		}

		IsBusy = true;
		ImportCommand.NotifyCanExecuteChanged ();
		StatusText = Strings.ImportingPhotos;
		var importCts = new CancellationTokenSource ();
		import = importCts;
		var token = importCts.Token;
		int total = snapshot.Count;
		var tags = ResolveAttachTags ();
		var progress = new Progress<int> (current => {
			Dispatcher.UIThread.Post (() => {
				StatusText = string.Format (Strings.ImportPhotosXOfY, current, total);
				Progress = total == 0 ? 0 : (double)current / total;
			});
		});

		try {
			var importer = Importer?.Invoke () ?? importFactory?.Invoke ()
				?? throw new InvalidOperationException ("Import is not available.");
			await Task.Run (() => importer.DoImport (
				database, snapshot, tags, Preferences, progress, token), token);
			var failed = importer.FailedImports.ToList ();
			int imported = importer.PhotosImported;
			await ImportCompletion.ReportFailuresAsync (shell, failed);
			Finish (ImportCompletion.ShouldReloadCatalog (imported));
		} catch (OperationCanceledException) {
			Finish (false);
		} catch (Exception ex) {
			importAccepted = false;
			Logger.Log.Error (ex, "Import failed");
			StatusText = ex.Message;
			IsBusy = false;
			ImportCommand.NotifyCanExecuteChanged ();
		} finally {
			if (ReferenceEquals (import, importCts))
				import = null;
			importCts.Dispose ();
		}
	}

	// The window is already closing. Cancel the scan and the import, drop
	// thumbnails, and do not ask the window to close a second time.
	internal void CancelInFlight ()
	{
		closed = true;
		scan?.Cancel ();
		import?.Cancel ();
		CancelPreview ();
		ReleaseListedThumbnails ();
	}

	void CancelPreview ()
		=> previewLoad.Cancel ();

	void Finish (bool reload)
	{
		if (closed)
			return;
		closed = true;
		close (reload);
	}

	[RelayCommand]
	void Cancel ()
	{
		scan?.Cancel ();
		import?.Cancel ();
		ReleaseListedThumbnails ();
		Finish (false);
	}

	[RelayCommand]
	Task WarnRemoveOriginalsAsync ()
		=> shell.ShowWarningAsync (Strings.Warning, Strings.CheckBoxToRemoveImportedPhotosFromCameraAndWarning);

	internal async Task ScanAsync ()
	{
		if (IsBusy || importAccepted)
			return;
		if (ScanOverride is null && SourcePath is null && droppedPaths is not { Count: > 0 })
			return;

		// The task is stored before the first await so Import can see a scan
		// that has already started.
		var previous = scan;
		var local = new CancellationTokenSource ();
		scan = local;
		var running = ScanCoreAsync (local, previous);
		scanTask = running;
		try {
			await running;
		} finally {
			if (ReferenceEquals (scanTask, running))
				scanTask = null;
		}
	}

	async Task ScanCoreAsync (CancellationTokenSource local, CancellationTokenSource? previous)
	{
		if (previous is not null)
			await previous.CancelAsync ();

		var token = local.Token;
		try {
			ResetListedPhotos ();
			IsScanning = true;
			if (!importAccepted)
				StatusText = Strings.SearchingForPhotosYouCanAlreadyClickImportToContinue;
			Progress = 0;
			ImportCommand.NotifyCanExecuteChanged ();

			IEnumerable<IPhoto>? photos = null;
			if (ScanOverride is not null) {
				photos = ScanOverride (token);
			} else if ((images ?? database?.Images) is { } imageFactory && fileSystem is not null) {
				IImportSource source = droppedPaths is { Count: > 0 }
					? new MultiFileImportSource (droppedPaths.Select (path => new SafeUri (path)), imageFactory, fileSystem)
					: new FileImportSource (new SafeUri (SourcePath!), imageFactory, fileSystem);
				photos = source.ScanPhotos (Preferences);
			} else {
				IsScanning = false;
				return;
			}

			try {
				await Task.Run (async () => {
					foreach (var info in photos) {
						if (token.IsCancellationRequested)
							break;
						await Dispatcher.UIThread.InvokeAsync (() => {
							if (token.IsCancellationRequested)
								return;
							try {
								AddScanned (info);
							} catch (Exception ex) {
								Logger.Log.Debug (ex, "Skipping unreadable file {Uri}", info.DefaultVersion.Uri);
							}
						});
					}
				}, token);
			} catch (OperationCanceledException) {
			} catch (Exception ex) {
				Logger.Log.Error (ex, "Import scan failed");
				if (!importAccepted)
					StatusText = ex.Message;
			}

			if (!token.IsCancellationRequested) {
				IsScanning = false;
				if (!importAccepted)
					StatusText = string.Format (Photos.Count <= 1 ? Strings.XPhoto : Strings.XPhotos, Photos.Count);
				ImportCommand.NotifyCanExecuteChanged ();
			}
		} finally {
			if (ReferenceEquals (scan, local))
				scan = null;
			local.Dispose ();
		}
	}

	internal void AddScanned (IPhoto photo)
	{
		importList.Add (photo);
		AddPendingPhoto (new PhotoItemViewModel (photo));
	}

	internal void AddPendingPhoto (PhotoItemViewModel item)
	{
		Photos.Add (item);
		OnPropertyChanged (nameof (CanImport));
		ImportCommand.NotifyCanExecuteChanged ();
		SelectedPhoto ??= item;
	}

	void ResetListedPhotos ()
	{
		ReleaseListedThumbnails ();
		Photos.Clear ();
		importList.Clear ();
	}

	void ReleaseListedThumbnails ()
	{
		var listed = Photos.ToArray ();
		thumbnailLoads.EndAll ();
		shownThumbnails.Clear ();
		thumbnailGeneration.Clear ();
		foreach (var item in listed)
			ReplaceThumbnail (item, null);
		SetPreview (null);
	}

	internal Task RealizeThumbnailAsync (object container, PhotoItemViewModel item)
	{
		shownThumbnails.Realize (container, item);
		if (item.Thumbnail is not null || thumbnailGeneration.ContainsKey (item))
			return Task.CompletedTask;
		return LoadThumbnailAsync (item);
	}

	internal void ReleaseThumbnail (object container)
	{
		if (shownThumbnails.Release (container) is not PhotoItemViewModel item)
			return;
		thumbnailGeneration.Remove (item);
		thumbnailLoads.End (item);
		ReplaceThumbnail (item, null);
	}

	async Task LoadThumbnailAsync (PhotoItemViewModel item)
	{
		int generation = NextGeneration (item);
		var token = thumbnailLoads.Begin (item);
		Bitmap? bitmap = null;
		try {
			bitmap = await thumbnails.LoadAsync (item.Path, HiDpi.PixelSize (HiDpi.ImportListDip, UiScale), token);
			token.ThrowIfCancellationRequested ();
			if (!shownThumbnails.IsHeld (item) || !thumbnailGeneration.TryGetValue (item, out int current) || current != generation) {
				bitmap?.Dispose ();
				return;
			}

			ReplaceThumbnail (item, bitmap);
		} catch (OperationCanceledException) {
			bitmap?.Dispose ();
		} catch (Exception ex) {
			bitmap?.Dispose ();
			Logger.Log.Debug (ex, "Failed to thumbnail import preview {Path}", item.Path);
			if (thumbnailGeneration.TryGetValue (item, out int current) && current == generation)
				thumbnailGeneration.Remove (item);
			thumbnailLoads.EndIfCurrent (item, token);
		}
	}

	internal async Task RefreshDisplayAsync ()
	{
		foreach (var held in shownThumbnails.Held) {
			if (held is PhotoItemViewModel item)
				await LoadThumbnailAsync (item);
		}

		if (SelectedPhoto is { } selected)
			await LoadPreviewAsync (selected);
	}

	async Task LoadPreviewAsync (PhotoItemViewModel item)
	{
		CancelPreview ();
		var load = new CancellationTokenSource ();
		previewLoad = load;
		Bitmap? bitmap = null;
		try {
			bitmap = await thumbnails.LoadAsync (item.Path, HiDpi.PixelSize (HiDpi.ImportPreviewDip, UiScale), load.Token);
			if (closed || load.IsCancellationRequested || !ReferenceEquals (item, SelectedPhoto)) {
				bitmap?.Dispose ();
				return;
			}

			SetPreview (bitmap);
		} catch (OperationCanceledException) {
			bitmap?.Dispose ();
		} catch (Exception ex) {
			bitmap?.Dispose ();
			Logger.Log.Debug (ex, "Failed to load import preview {Path}", item.Path);
		}
	}

	int NextGeneration (PhotoItemViewModel item)
	{
		thumbnailGeneration.TryGetValue (item, out int generation);
		generation++;
		thumbnailGeneration[item] = generation;
		return generation;
	}

	static void ReplaceThumbnail (PhotoItemViewModel item, Bitmap? bitmap)
	{
		var previous = item.Thumbnail;
		item.Thumbnail = bitmap;
		ReleasedBitmap.DisposePrevious (previous, bitmap);
	}

	void SetPreview (Bitmap? next)
	{
		var previous = Preview;
		Preview = next;
		ReleasedBitmap.DisposePrevious (previous, next);
	}

	List<Tag> ResolveAttachTags ()
	{
		var tags = new List<Tag> ();
		var names = AttachTagsText.Split ([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (names.Length == 0)
			return tags;

		database.BeginTransaction ();
		var store = database.Tags;
		var imported = store.GetTagByName (Strings.ImportedTags) as Category
			?? store.CreateCategory (null, Strings.ImportedTags, false);
		foreach (string name in names) {
			var tag = store.GetTagByName (name) ?? store.CreateCategory (imported, name, false);
			store.Commit (tag);
			tags.Add (tag);
		}
		database.CommitTransaction ();
		return tags;
	}
}
