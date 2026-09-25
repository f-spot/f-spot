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

using TinyIoC;

namespace FSpot.Avalonia.ViewModels;

public partial class ImportViewModel : ObservableObject
{
	readonly Db database;
	readonly IAvaloniaShell shell;
	readonly IPhotoThumbnailLoader thumbnails;
	readonly ThumbnailLease shownThumbnails = new ();
	readonly Dictionary<PhotoItemViewModel, int> thumbnailGeneration = new ();
	readonly Action<bool> close;
	readonly PhotoList importList = new PhotoList ();
	CancellationTokenSource? scan;
	CancellationTokenSource? import;
	IReadOnlyList<string>? droppedPaths;

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

	public ImportViewModel (Db database, IAvaloniaShell shell, IPhotoThumbnailLoader thumbnails, Action<bool> close)
	{
		this.database = database;
		this.shell = shell;
		this.thumbnails = thumbnails;
		this.close = close;
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
	public bool IsProgressVisible => IsBusy || IsScanning;
	public bool HasDetectedSources { get; private set; }

	partial void OnIsBusyChanged (bool value)
		=> OnPropertyChanged (nameof (IsProgressVisible));

	partial void OnIsScanningChanged (bool value)
		=> OnPropertyChanged (nameof (IsProgressVisible));

	partial void OnCopyFilesChanged (bool value)
	{
		Preferences.CopyFiles = value;
		if (!value)
			RemoveOriginals = false;
		OnPropertyChanged (nameof (RemoveOriginalsEnabled));
	}

	partial void OnRecurseSubdirectoriesChanged (bool value)
	{
		Preferences.RecurseSubdirectories = value;
		if (SourcePath is not null)
			_ = ScanAsync ();
	}

	partial void OnDuplicateDetectChanged (bool value)
		=> Preferences.DuplicateDetect = value;

	partial void OnMergeRawAndJpegChanged (bool value)
	{
		Preferences.MergeRawAndJpeg = value;
		if (SourcePath is not null)
			_ = ScanAsync ();
	}

	partial void OnRemoveOriginalsChanged (bool value)
		=> Preferences.RemoveOriginals = value;

	partial void OnSelectedPhotoChanged (PhotoItemViewModel? value)
	{
		if (value is not null)
			_ = LoadPreviewAsync (value);
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

		_ = ScanAsync ();
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

			var fileSystem = TinyIoCContainer.Current.Resolve<IFileSystem> ();
			var detected = DeviceImport.Sources (fileSystem);
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
		if (suppressSourceSelection || value is null)
			return;

		if (value.Kind == ImportSourceKind.ChooseFolder) {
			_ = ChooseFolderAsync ();
			return;
		}

		droppedPaths = null;
		SourcePath = value.Path;
		SourceLabel = value.Label;
		_ = ScanAsync ();
	}

	[RelayCommand]
	async Task ChooseFolderAsync ()
	{
		string? path = await shell.PickFolderAsync (Strings.ChooseFolder);
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
		if (Photos.Count == 0)
			return;

		IsBusy = true;
		ImportCommand.NotifyCanExecuteChanged ();
		StatusText = Strings.ImportingPhotos;
		import = new CancellationTokenSource ();
		var tags = ResolveAttachTags ();
		var progress = new Progress<int> (current => {
			Dispatcher.UIThread.Post (() => {
				StatusText = string.Format (Strings.ImportPhotosXOfY, current, Photos.Count);
				Progress = Photos.Count == 0 ? 0 : (double)current / Photos.Count;
			});
		});

		try {
			var importer = TinyIoCContainer.Current.Resolve<IImportController> ();
			await Task.Run (() => importer.DoImport (
				database, importList, tags, Preferences, progress, import.Token), import.Token);
			var failed = importer.FailedImports.ToList ();
			int imported = importer.PhotosImported;
			await ImportCompletion.ReportFailuresAsync (shell, failed);
			close (ImportCompletion.ShouldReloadCatalog (imported));
		} catch (OperationCanceledException) {
			close (false);
		} catch (Exception ex) {
			Logger.Log.Error (ex, "Import failed");
			StatusText = ex.Message;
			IsBusy = false;
			ImportCommand.NotifyCanExecuteChanged ();
		}
	}

	[RelayCommand]
	void Cancel ()
	{
		scan?.Cancel ();
		import?.Cancel ();
		close (false);
	}

	[RelayCommand]
	Task WarnRemoveOriginalsAsync ()
		=> shell.ShowWarningAsync (Strings.Warning, Strings.CheckBoxToRemoveImportedPhotosFromCameraAndWarning);

	async Task ScanAsync ()
	{
		if (SourcePath is null && droppedPaths is not { Count: > 0 })
			return;

		if (scan is not null)
			await scan.CancelAsync ();
		scan = new CancellationTokenSource ();
		var token = scan.Token;
		ResetListedPhotos ();
		IsScanning = true;
		StatusText = Strings.SearchingForPhotosYouCanAlreadyClickImportToContinue;
		Progress = 0;
		ImportCommand.NotifyCanExecuteChanged ();

		var container = TinyIoCContainer.Current;
		var imageFactory = container.Resolve<IImageFileFactory> ();
		var files = container.Resolve<IFileSystem> ();
		IImportSource source = droppedPaths is { Count: > 0 }
			? new MultiFileImportSource (droppedPaths.Select (path => new SafeUri (path)), imageFactory, files)
			: new FileImportSource (new SafeUri (SourcePath!), imageFactory, files);
		try {
			await Task.Run (async () => {
				foreach (var info in source.ScanPhotos (Preferences)) {
					if (token.IsCancellationRequested)
						break;
					await Dispatcher.UIThread.InvokeAsync (() => {
						if (token.IsCancellationRequested)
							return;
						try {
							importList.Add (info);
							AddPendingPhoto (new PhotoItemViewModel (info));
						} catch (Exception ex) {
							Logger.Log.Debug (ex, "Skipping unreadable file {Uri}", info.DefaultVersion.Uri);
						}
					});
				}
			}, token);
		} catch (OperationCanceledException) {
		} catch (Exception ex) {
			Logger.Log.Error (ex, "Import scan failed");
			StatusText = ex.Message;
		} finally {
			if (!token.IsCancellationRequested) {
				IsScanning = false;
				StatusText = string.Format (Photos.Count <= 1 ? Strings.XPhoto : Strings.XPhotos, Photos.Count);
				ImportCommand.NotifyCanExecuteChanged ();
			}
		}
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
		var listed = Photos.ToArray ();
		shownThumbnails.Clear ();
		thumbnailGeneration.Clear ();
		foreach (var item in listed)
			ReplaceThumbnail (item, null);
		SetPreview (null);
		Photos.Clear ();
		importList.Clear ();
	}

	internal Task RealizeThumbnailAsync (object container, PhotoItemViewModel item)
	{
		shownThumbnails.Realize (container, item);
		if (item.Thumbnail is not null || thumbnailGeneration.ContainsKey (item))
			return Task.CompletedTask;
		return LoadThumbnailAsync (item, CancellationToken.None);
	}

	internal void ReleaseThumbnail (object container)
	{
		if (shownThumbnails.Release (container) is not PhotoItemViewModel item)
			return;
		thumbnailGeneration.Remove (item);
		ReplaceThumbnail (item, null);
	}

	async Task LoadThumbnailAsync (PhotoItemViewModel item, CancellationToken token)
	{
		int generation = NextGeneration (item);
		Bitmap? bitmap = null;
		try {
			bitmap = await thumbnails.LoadAsync (item.Path, HiDpi.PixelSize (HiDpi.ImportListDip, UiScale), token);
			if (!shownThumbnails.IsHeld (item) || !thumbnailGeneration.TryGetValue (item, out int current) || current != generation) {
				bitmap?.Dispose ();
				return;
			}

			ReplaceThumbnail (item, bitmap);
		} catch (Exception ex) {
			bitmap?.Dispose ();
			Logger.Log.Debug (ex, "Failed to thumbnail import preview {Path}", item.Path);
		}
	}

	async Task LoadPreviewAsync (PhotoItemViewModel item)
	{
		Bitmap? bitmap = null;
		try {
			bitmap = await thumbnails.LoadAsync (item.Path, HiDpi.PixelSize (HiDpi.ImportPreviewDip, UiScale), CancellationToken.None);
			if (!ReferenceEquals (item, SelectedPhoto)) {
				bitmap?.Dispose ();
				return;
			}

			SetPreview (bitmap);
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
