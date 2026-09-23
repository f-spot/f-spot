//
// ImportViewModel.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.IO;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
	readonly IImageFileFactory factory;
	readonly IFileSystem fileSystem;
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

	public ImportViewModel (Db database, IAvaloniaShell shell, IPhotoThumbnailLoader thumbnails, Action<bool> close)
	{
		this.database = database;
		this.shell = shell;
		this.thumbnails = thumbnails;
		this.close = close;
		var container = TinyIoCContainer.Current;
		factory = container.Resolve<IImageFileFactory> ();
		fileSystem = container.Resolve<IFileSystem> ();
		Preferences = new ImportPreferences (true);
		CopyFiles = Preferences.CopyFiles;
		RecurseSubdirectories = Preferences.RecurseSubdirectories;
		DuplicateDetect = Preferences.DuplicateDetect;
		MergeRawAndJpeg = Preferences.MergeRawAndJpeg;
		RemoveOriginals = Preferences.RemoveOriginals;
	}

	public ImportPreferences Preferences { get; }
	public ObservableCollection<PhotoItemViewModel> Photos { get; } = [];
	public bool CanImport => Photos.Count > 0 && !IsBusy;
	public bool RemoveOriginalsEnabled => CopyFiles;
	public bool IsProgressVisible => IsBusy || IsScanning;

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
	}

	public void SeedDropped (IReadOnlyList<string> paths)
	{
		droppedPaths = paths;
		SourcePath = paths.Count == 1 ? paths[0] : null;
		SourceLabel = paths.Count == 1
			? Path.GetFileName (paths[0].TrimEnd (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
			: string.Format (Strings.XPhotos, paths.Count);
		_ = ScanAsync ();
	}

	[RelayCommand]
	async Task ChooseFolderAsync ()
	{
		string? path = await shell.PickFolderAsync (Strings.ChooseFolder);
		if (path is null)
			return;

		SourcePath = path;
		SourceLabel = Path.GetFileName (path.TrimEnd (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
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
		Photos.Clear ();
		importList.Clear ();
		IsScanning = true;
		StatusText = Strings.SearchingForPhotosYouCanAlreadyClickImportToContinue;
		Progress = 0;
		ImportCommand.NotifyCanExecuteChanged ();

		IImportSource source = droppedPaths is { Count: > 0 }
			? new MultiFileImportSource (droppedPaths.Select (path => new SafeUri (path)), factory, fileSystem)
			: new FileImportSource (new SafeUri (SourcePath!), factory, fileSystem);
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
							var item = new PhotoItemViewModel (info);
							Photos.Add (item);
							OnPropertyChanged (nameof (CanImport));
							ImportCommand.NotifyCanExecuteChanged ();
							SelectedPhoto ??= item;
							_ = LoadThumbnailAsync (item, token);
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

	async Task LoadThumbnailAsync (PhotoItemViewModel item, CancellationToken token)
	{
		try {
			item.Thumbnail = await thumbnails.LoadAsync (item.Path, HiDpi.PixelSize (HiDpi.ImportListDip, UiScale), token);
		} catch (Exception ex) {
			Logger.Log.Debug (ex, "Failed to thumbnail import preview {Path}", item.Path);
		}
	}

	async Task LoadPreviewAsync (PhotoItemViewModel item)
	{
		if (item.Thumbnail is not null)
			return;
		try {
			item.Thumbnail = await thumbnails.LoadAsync (item.Path, HiDpi.PixelSize (HiDpi.ImportPreviewDip, UiScale), CancellationToken.None);
		} catch (Exception ex) {
			Logger.Log.Debug (ex, "Failed to load import preview {Path}", item.Path);
		}
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
