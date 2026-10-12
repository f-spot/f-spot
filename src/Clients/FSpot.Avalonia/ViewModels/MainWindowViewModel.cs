//
// MainWindowViewModel.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.IO;

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FSpot;
using FSpot.Avalonia.Dialogs;
using FSpot.Avalonia.Imaging;
using FSpot.Avalonia.Models;
using FSpot.Avalonia.Services;
using FSpot.Core;
using FSpot.Database;
using FSpot.Editors;
using FSpot.Imaging;
using FSpot.Import;
using FSpot.Query;
using FSpot.Resources.Lang;
using FSpot.Settings;
using FSpot.Slideshow;
using FSpot.Thumbnail;
using FSpot.Utils;

using Hyena;

using TinyIoC;

namespace FSpot.Avalonia.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
	readonly Db? database;
	readonly IAvaloniaShell shell;
	readonly IPhotoThumbnailLoader thumbnails;
	readonly ThumbnailLease shownThumbnails = new ();
	readonly Dictionary<PhotoItemViewModel, int> thumbnailGeneration = new ();
	CancellationTokenSource thumbnailLoads = new ();
	Task previewTask = Task.CompletedTask;

	[ObservableProperty]
	LibraryViewMode viewMode = LibraryViewMode.IconView;

	[ObservableProperty]
	PhotoItemViewModel? selectedPhoto;

	[ObservableProperty]
	TagNode? selectedTag;

	[ObservableProperty]
	FolderNode? selectedFolder;

	[ObservableProperty]
	TagNode? activeTagFilter;

	[ObservableProperty]
	FolderNode? activeFolderFilter;

	[ObservableProperty]
	int sidebarTab;

	int browseSidebarTab;

	[ObservableProperty]
	Bitmap? preview;

	[ObservableProperty]
	double zoom = 128;

	[ObservableProperty]
	string statusText = string.Empty;

	[ObservableProperty]
	bool reverseOrder;

	[ObservableProperty]
	bool findUntagged;

	[ObservableProperty]
	bool displaySidebar = true;

	[ObservableProperty]
	bool displayTimeline = true;

	[ObservableProperty]
	bool displayFilmstrip = true;

	[ObservableProperty]
	bool displayToolbar = true;

	[ObservableProperty]
	bool displayDates = true;

	[ObservableProperty]
	bool displayTags = true;

	[ObservableProperty]
	bool displayRatings = true;

	[ObservableProperty]
	bool isFindBarVisible;

	[ObservableProperty]
	string findText = string.Empty;

	[ObservableProperty]
	TimelineBucket? selectedTimeline;

	[ObservableProperty]
	Bitmap? histogram;

	[ObservableProperty]
	PhotoEditorSession? editorSession;

	[ObservableProperty]
	bool isLoupeVisible;

	[ObservableProperty]
	double loupeX;

	[ObservableProperty]
	double loupeY;

	[ObservableProperty]
	IImage? loupeSample;

	[ObservableProperty]
	string windowTitle = Strings.FSpot;

	[ObservableProperty]
	bool findMatchAllTags;

	[ObservableProperty]
	uint? findMinRating;

	[ObservableProperty]
	uint? findMaxRating;

	[ObservableProperty]
	DateTime? findStart;

	[ObservableProperty]
	DateTime? findEnd;

	[ObservableProperty]
	double previewZoom = 1;

	[ObservableProperty]
	double previewPanX;

	[ObservableProperty]
	double previewPanY;

	[ObservableProperty]
	bool viewerInterpolation = true;

	[ObservableProperty]
	bool isMailEnabled;

	[ObservableProperty]
	double uiScale = 1;

	public BitmapInterpolationMode PreviewInterpolation
		=> ViewerInterpolation ? BitmapInterpolationMode.HighQuality : BitmapInterpolationMode.None;

	partial void OnViewerInterpolationChanged (bool value)
		=> OnPropertyChanged (nameof (PreviewInterpolation));

	// GTK keeps the info box fields per view context (library and photo view).
	public InfoFieldVisibility InfoVisibility => IsEditing ? editInfoVisibility : libraryInfoVisibility;
	public ObservableCollection<AddinStatus> Extensions { get; } = [];

	DateTime? timelineRangeStart;
	DateTime? timelineRangeEnd;

	readonly List<PhotoItemViewModel> catalogPhotos = [];
	int histogramGeneration;
	readonly List<uint> findRollIds = [];

	public MainWindowViewModel (Db? database, IAvaloniaShell shell, IPhotoThumbnailLoader thumbnails)
	{
		this.database = database;
		this.shell = shell;
		this.thumbnails = thumbnails;
		DisplaySidebar = LibraryPreferences.ShowSidebar;
		DisplayTimeline = LibraryPreferences.ShowTimeline;
		DisplayFilmstrip = LibraryPreferences.ShowFilmstrip;
		DisplayToolbar = LibraryPreferences.ShowToolbar;
		DisplayDates = LibraryPreferences.ShowDates;
		DisplayTags = LibraryPreferences.ShowTags;
		DisplayRatings = LibraryPreferences.ShowRatings;
		ReverseOrder = LibraryPreferences.GroupAdaptorOrderAsc;
		ShowHoverPreviewEnabled = LibraryPreferences.ShowHoverPreview;
		ViewerInterpolation = LibraryPreferences.ViewerInterpolation;
		IsMailEnabled = LibraryPreferences.MailToEnabled;
		FilmstripOnSide = Preferences.Get<int> (Preferences.FilmstripOrientation) == 1;
		Tag.TagIconSize = (IconSize)Preferences.Get<int> (Preferences.TagIconSize);
		double storedZoom = LibraryPreferences.Zoom;
		if (storedZoom >= 64 && storedZoom <= 256)
			Zoom = storedZoom;
		sidebarPixels = Math.Clamp (LibraryPreferences.SidebarPosition, 120, 600);
		SidebarTab = Math.Clamp (LibraryPreferences.SidebarPageLibrary, 0, 1);
		Info.VersionChosen += ApplyVersion;
		Info.RatingChosen += ApplyRating;
		Info.DescriptionChosen += ApplyDescription;
		WatchMenuExtensions ();
	}

	// Mirrors GTK's MainWindow.Close(), which snapshots the same toggles,
	// zoom, sidebar width/page, and sort order into Preferences before the
	// window goes away. Window position/size/maximized are saved separately,
	// by MainWindowStateStore, since the ViewModel doesn't own the Window.
	public void SaveWindowState ()
	{
		LibraryPreferences.ShowSidebar = DisplaySidebar;
		LibraryPreferences.ShowTimeline = DisplayTimeline;
		LibraryPreferences.ShowFilmstrip = DisplayFilmstrip;
		LibraryPreferences.ShowToolbar = DisplayToolbar;
		LibraryPreferences.ShowTags = DisplayTags;
		LibraryPreferences.ShowDates = DisplayDates;
		LibraryPreferences.ShowRatings = DisplayRatings;
		LibraryPreferences.GroupAdaptorOrderAsc = ReverseOrder;
		LibraryPreferences.Zoom = Zoom;
		LibraryPreferences.SidebarPosition = (int)sidebarPixels;
		LibraryPreferences.SidebarPageLibrary = browseSidebarTab;
	}

	double sidebarPixels = 220;

	public GridLength SidebarLength {
		get => DisplaySidebar ? new GridLength (sidebarPixels) : new GridLength (0);
		set {
			if (value.GridUnitType == GridUnitType.Pixel && value.Value >= 120)
				sidebarPixels = value.Value;
			OnPropertyChanged ();
		}
	}

	partial void OnDisplaySidebarChanged (bool value)
		=> OnPropertyChanged (nameof (SidebarLength));

	public ObservableCollection<PhotoItemViewModel> Photos { get; } = [];
	public ObservableCollection<TagNode> Tags { get; } = [];
	public ObservableCollection<FolderNode> Folders { get; } = [];
	public ObservableCollection<TimelineBucket> TimelineBuckets { get; } = [];
	public PhotoInfoViewModel Info { get; } = new ();

	public bool IsBrowsing => ViewMode == LibraryViewMode.IconView;
	public bool IsEditing => ViewMode == LibraryViewMode.PhotoView;
	public bool CanUseTimeline => IsBrowsing;
	public bool CanUseFilmstrip => IsEditing;
	public IReadOnlyList<PhotoEditorDescriptor> VisibleEditors =>
		PhotoEditorCatalog.VisibleIn (ViewMode).Where (editor => EditorEnabled (editor.Id)).ToList ();
	public bool HasEditorSession => EditorSession is not null;
	public bool ShowEditorList => IsEditPage && EditorSession is null;
	public bool ShowEditorSelection => EditorSession?.Editor.NeedsSelection == true;
	public bool ShowEditorSettings => EditorSession?.Editor.HasSettings == true;
	public bool ShowColorSettings => EditorSession?.Editor.Id == PhotoEditorCatalog.Color.Id;
	public bool ShowBwSettings => EditorSession?.Editor.Id == PhotoEditorCatalog.BW.Id;
	public bool ShowResizeSettings => EditorSession?.Editor.Id == PhotoEditorCatalog.Resize.Id;
	public bool ShowTiltSettings => EditorSession?.Editor.Id == PhotoEditorCatalog.Tilt.Id;
	public bool ShowSoftFocusSettings => EditorSession?.Editor.Id == PhotoEditorCatalog.SoftFocus.Id;
	public bool ShowSharpenSettings => EditorSession?.Editor.Id == PhotoEditorCatalog.Sharpen.Id;
	public int PreviewPixelWidth => Preview?.PixelSize.Width ?? 0;
	public int PreviewPixelHeight => Preview?.PixelSize.Height ?? 0;
	public bool ShowLoupe => IsLoupeVisible && IsEditing && Preview is not null;
	public bool ShowTimeline => IsBrowsing && DisplayTimeline;
	public bool ShowFilmstrip => IsEditing && DisplayFilmstrip;
	public bool ShowTagBar => IsEditing;
	public bool ShowInfoBox => SelectedPhoto is not null;
	public bool HasLibraryFilter => ActiveTagFilter is not null || ActiveFolderFilter is not null || FindUntagged
		|| FindTags.Count > 0 || HasFindRating || HasFindDate || HasFindRoll || HasTagQuery || HasTermQuery;
	public bool HasFindRating => FindMinRating is not null || FindMaxRating is not null;
	public bool HasFindDate => FindStart is not null || FindEnd is not null;
	public bool HasFindRoll => findRollIds.Count > 0;
	public string FindRatingLabel => HasFindRating
		? QueryChipFormat.Rating (FindMinRating, FindMaxRating)
		: string.Empty;
	public string FindDateLabel => HasFindDate
		? QueryChipFormat.Date (FindStart, FindEnd)
		: string.Empty;
	public string FindRollLabel => HasFindRoll
		? QueryChipFormat.Roll (string.Join (", ", findRollIds))
		: string.Empty;
	public bool ShowQueryBarNoMatches => ShowNoMatches && HasLibraryFilter;
	public bool ShowFindBarNoMatches => ShowNoMatches && IsFindBarVisible;
	public ObservableCollection<TagNode> FindTags { get; } = [];
	public bool ShowEmptyLibrary =>
		database is not null && IsBrowsing && catalogPhotos.Count == 0
		&& !HasLibraryFilter && string.IsNullOrWhiteSpace (FindText) && SelectedTimeline is null
		&& timelineRangeStart is null && timelineLimitStart is null;
	public bool ShowNoMatches =>
		IsBrowsing && Photos.Count == 0 && !ShowEmptyLibrary
		&& (catalogPhotos.Count > 0 || HasLibraryFilter || !string.IsNullOrWhiteSpace (FindText)
			|| SelectedTimeline is not null || timelineRangeStart is not null || timelineLimitStart is not null);
	public bool ShowPhotoGrid => IsBrowsing && Photos.Count > 0;
	public bool IsTagsPage => SidebarTab == 0;
	public bool IsFoldersPage => SidebarTab == 1;
	public bool IsEditPage => SidebarTab == 2;
	public string SidebarPageTitle => SidebarTab switch {
		1 => Strings.Folders,
		2 => Strings.Edit,
		_ => Strings.Tags
	};
	public double TileSize => Zoom;
	public double GridCellWidth => TileSize + 8;
	public double GridCellHeight => TileSize + 96;
	public double FilmstripPaneHeight => Zoom + 24;
	public bool FilmstripOnSide { get; private set; }
	public global::Avalonia.Controls.Dock FilmstripEdge => FilmstripOnSide ? global::Avalonia.Controls.Dock.Right : global::Avalonia.Controls.Dock.Top;
	public global::Avalonia.Layout.Orientation FilmstripFlow => FilmstripOnSide ? global::Avalonia.Layout.Orientation.Vertical : global::Avalonia.Layout.Orientation.Horizontal;
	public double FilmstripBreadth => FilmstripOnSide ? 96 : double.NaN;
	public double FilmstripSpan => FilmstripOnSide ? double.NaN : 84;
	public bool TagIconsVisible => TagIconEdge > 0;
	public double TagIconEdge => (int)Tag.TagIconSize;

	[ObservableProperty]
	bool isTagTypingOpen;

	[ObservableProperty]
	string tagTypingText = string.Empty;

	[ObservableProperty]
	bool isFullScreen;

	[ObservableProperty]
	bool showHoverPreviewEnabled;

	[ObservableProperty]
	bool showHoverPreview;

	[ObservableProperty]
	Bitmap? hoverThumbnail;

	[ObservableProperty]
	string hoverDetail = string.Empty;

	[ObservableProperty]
	int hoverEdge = 480;

	int hoverGeneration;
	public PrintPageSetup PageSetup { get; private set; } = PrintPageSetup.Letter;
	public string CountText =>
		SelectedPhoto is null
			? string.Empty
			: string.Format (Strings.PhotoOutOfPhotos, Photos.IndexOf (SelectedPhoto) + 1, Photos.Count);

	public void LoadCatalog ()
	{
		if (database is null)
			return;

		ReloadTags ();
		ReloadPhotos ();
		ReloadFolders ();
	}

	void ReloadTags ()
	{
		if (database is null)
			return;

		var selected = SelectedTag?.Tag;
		Tags.Clear ();
		foreach (var tag in database.Tags.RootCategory.Children)
			Tags.Add (new TagNode (tag));

		if (selected is not null)
			SelectedTag = FindTagNode (Tags, selected);
		SyncTagQueryWithTags ();
	}

	static TagNode? FindTagNode (IEnumerable<TagNode> nodes, Tag tag)
	{
		foreach (var node in nodes) {
			if (node.Tag.Id == tag.Id)
				return node;
			var child = FindTagNode (node.Children, tag);
			if (child is not null)
				return child;
		}

		return null;
	}

	void ReloadFolders ()
	{
		if (database is null)
			return;

		var selected = SelectedFolder?.Uri;
		Folders.Clear ();
		foreach (var node in FolderTree.Build (database.Photos.ListFolderCounts ()))
			Folders.Add (new FolderNode (node, true, catalogPhotos.Count));

		SelectedFolder = selected is null ? null : FindFolderNode (Folders, selected);
	}

	static FolderNode? FindFolderNode (IEnumerable<FolderNode> nodes, SafeUri uri)
	{
		foreach (var node in nodes) {
			if (node.Uri.Equals (uri))
				return node;
			var child = FindFolderNode (node.Children, uri);
			if (child is not null)
				return child;
		}

		return null;
	}

	void ReloadPhotos ()
	{
		if (database is null) {
			ApplyLocalFilters ();
			NotifyFilterBar ();
			return;
		}

		var conditions = new List<IQueryCondition> {
			ReverseOrder ? OrderByTime.OrderByTimeAsc : OrderByTime.OrderByTimeDesc
		};
		if (ActiveTagFilter is not null)
			conditions.Add (new TagTerm (ActiveTagFilter.Tag));
		if (ActiveFolderFilter is not null)
			conditions.Add (new FolderSet { Folders = new[] { ActiveFolderFilter.Uri } });
		if (FindUntagged)
			conditions.Add (new UntaggedCondition ());
		if (FindMinRating is uint minRating)
			conditions.Add (new RatingRange (minRating, FindMaxRating ?? 5));
		if (FindStart is DateTime start && FindEnd is DateTime end)
			conditions.Add (new DateRange (start, end));
		if (findRollIds.Count > 0) {
			var rolls = new List<Roll> ();
			foreach (uint id in findRollIds)
				rolls.Add (new Roll (id, 0));
			conditions.Add (new RollSet (rolls));
		}

		DropAllThumbnails ();
		catalogPhotos.Clear ();
		foreach (var photo in database.Photos.Query ([.. conditions]))
			catalogPhotos.Add (new PhotoItemViewModel (photo));

		RebuildTimeline ();
		ApplyLocalFilters ();
	}

	void RebuildTimeline ()
	{
		var selectedStart = SelectedTimeline?.Start;
		var months = TimelineMonths.Span (catalogPhotos.Select (photo => photo.Photo));
		int maxCount = Math.Max (1, months.Count == 0 ? 1 : months.Max (month => month.Count));

		// The strip rebuilds its months here, and its limit handles go back to the whole range with them.
		ClearTimelineLimits ();
		TimelineBuckets.Clear ();
		foreach (var month in months) {
			TimelineBuckets.Add (new TimelineBucket {
				MonthLabel = month.Start.ToString ("MMM"),
				YearLabel = month.Start.Month == 1 || month.Start == months[0].Start
					? month.Start.Year.ToString ()
					: null,
				Start = month.Start,
				End = month.End,
				Count = month.Count,
				RelativeHeight = month.Count / (double)maxCount,
				Selected = selectedStart == month.Start
			});
		}

		if (SelectedTimeline is not null && TimelineBuckets.All (bucket => bucket.Start != SelectedTimeline.Start))
			SelectedTimeline = null;
		HighlightTimelineRange ();
	}

	void ApplyLocalFilters ()
	{
		DateTime? start = timelineRangeStart ?? SelectedTimeline?.Start ?? FindStart;
		DateTime? end = timelineRangeEnd ?? SelectedTimeline?.End ?? FindEnd;
		if (timelineRangeStart is DateTime rangeStart && FindStart is DateTime findStart)
			start = rangeStart > findStart ? rangeStart : findStart;
		if (timelineRangeEnd is DateTime rangeEnd && FindEnd is DateTime findEnd)
			end = rangeEnd < findEnd ? rangeEnd : findEnd;
		(start, end) = ApplyTimelineLimits (start, end);

		var tags = new List<Tag> ();
		if (ActiveTagFilter is not null)
			tags.Add (ActiveTagFilter.Tag);
		foreach (var node in FindTags) {
			if (tags.All (tag => tag.Id != node.Tag.Id))
				tags.Add (node.Tag);
		}

		var find = new LibraryFind {
			Matchers = FindMatchers (),
			Tags = tags,
			RequireAllTags = FindMatchAllTags,
			Untagged = FindUntagged,
			MinRating = FindMinRating,
			MaxRating = FindMaxRating,
			Start = start,
			End = end,
			RollIds = findRollIds,
			TagQuery = TermQuery
		};
		var visible = catalogPhotos
			.Where (item => PhotoLibraryFilter.Matches (item.Photo, find, RollIdOf (item.Photo)))
			.ToList ();

		DropAllThumbnails ();
		Photos.Clear ();
		foreach (var item in visible)
			Photos.Add (item);

		PhotoItemViewModel? current = SelectedPhoto is not null && visible.Contains (SelectedPhoto) && SelectedPhoto.Selected
			? SelectedPhoto
			: visible.FirstOrDefault (item => item.Selected) ?? visible.FirstOrDefault ();
		SelectedPhoto = current;
		UpdateStatus ();
		OnPropertyChanged (nameof (ShowEmptyLibrary));
		OnPropertyChanged (nameof (ShowNoMatches));
		OnPropertyChanged (nameof (ShowQueryBarNoMatches));
		OnPropertyChanged (nameof (ShowFindBarNoMatches));
		OnPropertyChanged (nameof (ShowPhotoGrid));
		ClearVisibleFiltersCommand.NotifyCanExecuteChanged ();
		NotifyFindTerms ();
		_ = LoadThumbnailsAsync ();
	}

	static uint RollIdOf (IPhoto photo)
		=> photo is Photo catalog ? catalog.RollId : 0;

	void HighlightTimelineRange ()
	{
		DateTime? start = timelineRangeStart ?? SelectedTimeline?.Start;
		DateTime? end = timelineRangeEnd ?? SelectedTimeline?.End;
		foreach (var bucket in TimelineBuckets) {
			bucket.Selected = SelectedTimeline is not null && bucket.Start == SelectedTimeline.Start;
			bucket.InRange = start is DateTime rangeStart && end is DateTime rangeEnd
				&& bucket.Start >= rangeStart && bucket.Start < rangeEnd;
		}
	}

	internal int CatalogCount => catalogPhotos.Count;

	internal void SeedCatalog (params IPhoto[] photos)
	{
		DropAllThumbnails ();
		catalogPhotos.Clear ();
		foreach (var photo in photos)
			catalogPhotos.Add (new PhotoItemViewModel (photo));
		RebuildTimeline ();
		ApplyLocalFilters ();
	}

	void NotifyFindTerms ()
	{
		OnPropertyChanged (nameof (HasLibraryFilter));
		OnPropertyChanged (nameof (HasFindRating));
		OnPropertyChanged (nameof (HasFindDate));
		OnPropertyChanged (nameof (HasFindRoll));
		OnPropertyChanged (nameof (FindRatingLabel));
		OnPropertyChanged (nameof (FindDateLabel));
		OnPropertyChanged (nameof (FindRollLabel));
		NotifyFilterDialogCommands ();
		OnPropertyChanged (nameof (ShowEmptyLibrary));
		OnPropertyChanged (nameof (ShowNoMatches));
		OnPropertyChanged (nameof (ShowQueryBarNoMatches));
		OnPropertyChanged (nameof (ShowFindBarNoMatches));
		OnPropertyChanged (nameof (ShowPhotoGrid));
		ClearVisibleFiltersCommand.NotifyCanExecuteChanged ();
		ClearLibraryFiltersCommand.NotifyCanExecuteChanged ();
	}

	internal Task RealizeThumbnailAsync (object container, PhotoItemViewModel item)
	{
		shownThumbnails.Realize (container, item);
		if (item.Thumbnail is not null || thumbnailGeneration.ContainsKey (item))
			return Task.CompletedTask;
		return LoadOneAsync (item, thumbnailLoads.Token);
	}

	internal void ReleaseThumbnail (object container)
	{
		if (shownThumbnails.Release (container) is not PhotoItemViewModel item)
			return;
		thumbnailGeneration.Remove (item);
		ReplaceThumbnail (item, null);
	}

	void DropAllThumbnails ()
	{
		thumbnailLoads.Cancel ();
		thumbnailLoads.Dispose ();
		thumbnailLoads = new CancellationTokenSource ();
		var extras = shownThumbnails.Held.OfType<PhotoItemViewModel> ().ToArray ();
		shownThumbnails.Clear ();
		thumbnailGeneration.Clear ();
		ClearHover ();
		foreach (var item in catalogPhotos)
			ReplaceThumbnail (item, null);
		foreach (var item in extras)
			ReplaceThumbnail (item, null);
	}

	async Task LoadThumbnailsAsync ()
	{
		var token = thumbnailLoads.Token;
		var held = shownThumbnails.Held.OfType<PhotoItemViewModel> ().ToArray ();
		try {
			foreach (var item in held) {
				token.ThrowIfCancellationRequested ();
				await LoadOneAsync (item, token);
			}
		} catch (OperationCanceledException) {
		}
	}

	async Task LoadOneAsync (PhotoItemViewModel item, CancellationToken token)
	{
		int generation = NextThumbnailGeneration (item);
		int size = HiDpi.PixelSize (Zoom, UiScale);
		Bitmap? bitmap = null;
		try {
			bitmap = await thumbnails.LoadAsync (item.Path, size, token);
			token.ThrowIfCancellationRequested ();
			if (!shownThumbnails.IsHeld (item) || !thumbnailGeneration.TryGetValue (item, out int current) || current != generation) {
				bitmap?.Dispose ();
				return;
			}

			ReplaceThumbnail (item, bitmap);
		} catch (OperationCanceledException) {
			bitmap?.Dispose ();
			throw;
		} catch (Exception ex) {
			bitmap?.Dispose ();
			Logger.Log.Debug (ex, "Failed to decode thumbnail for {Path}", item.Path);
		}
	}

	int NextThumbnailGeneration (PhotoItemViewModel item)
	{
		thumbnailGeneration.TryGetValue (item, out int generation);
		generation++;
		thumbnailGeneration[item] = generation;
		return generation;
	}

	void ReplaceThumbnail (PhotoItemViewModel item, Bitmap? bitmap)
	{
		var previous = item.Thumbnail;
		item.Thumbnail = bitmap;
		ReleasedBitmap.DisposePrevious (previous, bitmap);
	}

	void RequestPreview ()
		=> previewTask = UpdatePreviewAsync ();

	internal Task ReloadPreviewAsync ()
	{
#pragma warning disable VSTHRD003 // Selection already started this load.
		return previewTask;
#pragma warning restore VSTHRD003
	}

	async Task UpdatePreviewAsync ()
	{
		if (!IsEditing || SelectedPhoto is null) {
			SetPreview (null);
			return;
		}

		var item = SelectedPhoto;
		int size = EditorPreview.DecodeWidth (UiScale);
		Bitmap? bitmap = null;
		try {
			bitmap = await thumbnails.LoadAsync (item.Path, size, CancellationToken.None);
			if (!IsEditing || !ReferenceEquals (item, SelectedPhoto)) {
				bitmap?.Dispose ();
				return;
			}

			SetPreview (bitmap);
		} catch (Exception ex) {
			bitmap?.Dispose ();
			Logger.Log.Debug (ex, "Failed to load preview {Path}", item.Path);
		}
	}

	void SetPreview (Bitmap? next)
	{
		if (ReferenceEquals (Preview, next))
			return;
		LoupeSample = null;
		var previous = Preview;
		Preview = next;
		ReleasedBitmap.DisposePrevious (previous, next);
	}

	partial void OnPreviewChanged (Bitmap? value)
	{
		OnPropertyChanged (nameof (PreviewPixelWidth));
		OnPropertyChanged (nameof (PreviewPixelHeight));
		OnPropertyChanged (nameof (ShowLoupe));
		if (value is null)
			LoupeSample = null;
	}

	void UpdateStatus ()
	{
		int shown = Photos.Count;
		int total = database?.Photos.TotalPhotos ?? 0;
		int selected = Photos.Count (item => item.Selected);
		string text;
		if (shown == total)
			text = string.Format (shown <= 1 ? Strings.XPhoto : Strings.XPhotos, shown);
		else
			text = string.Format (shown <= 1 ? Strings.XPhotoOutOfY : Strings.XPhotosOutOfY, shown, total);
		if (selected > 0)
			text += string.Format (Strings.XSelected, selected);
		StatusText = text;
		WindowTitle = $"{Strings.FSpot} — {string.Format (shown <= 1 ? Strings.XPhoto : Strings.XPhotos, shown)}";
	}

	void RequestHistogram (PhotoItemViewModel? item)
	{
		int generation = ++histogramGeneration;
		if (item is null) {
			SetHistogram (null);
			return;
		}
		_ = UpdateHistogramAsync (item, generation);
	}

	internal void ReleaseDecodedImages ()
	{
		menuWatcher?.Dispose ();
		menuWatcher = null;
		DropAllThumbnails ();
		HidePreviewPopup ();
		SetPreview (null);
		SetHistogram (null);
	}

	void SetHistogram (Bitmap? next)
	{
		var previous = Histogram;
		Histogram = next;
		ReleasedBitmap.DisposePrevious (previous, next);
	}

	async Task UpdateHistogramAsync (PhotoItemViewModel item, int generation)
	{
		var context = SynchronizationContext.Current;
		if (EditedPreview is Bitmap edited && ReferenceEquals (item, SelectedPhoto)) {
			ApplyChart (edited, generation, item);
			return;
		}

		Bitmap? loaded = null;
		try {
			loaded = await thumbnails.LoadAsync (item.Path, HistogramRenderer.SourceEdge, CancellationToken.None);
			if (!HistogramStillCurrent (generation, item))
				return;
			if (loaded is null) {
				SetHistogram (null);
				return;
			}
			void Apply ()
			{
				if (!HistogramStillCurrent (generation, item) || loaded is null)
					return;
				Bitmap fitted = HistogramRenderer.FitInside (loaded, HistogramRenderer.SourceEdge);
				try {
					ApplyChart (fitted, generation, item);
				} finally {
					if (!ReferenceEquals (fitted, loaded))
						fitted.Dispose ();
				}
			}
			if (context is null || SynchronizationContext.Current == context)
				Apply ();
			else
				PostTo (context, Apply);
		} catch (Exception ex) {
			if (!HistogramStillCurrent (generation, item))
				return;
			Logger.Log.Debug (ex, "Failed to build histogram for {Path}", item.Path);
			SetHistogram (null);
		} finally {
			if (context is null || SynchronizationContext.Current == context)
				loaded?.Dispose ();
			else {
				var bitmap = loaded;
				PostTo (context, () => bitmap?.Dispose ());
			}
		}
	}

	void ApplyChart (Bitmap source, int generation, PhotoItemViewModel item)
	{
		if (!HistogramStillCurrent (generation, item))
			return;
		try {
			SetHistogram (HistogramRenderer.FromBitmap (source));
		} catch (Exception ex) {
			if (!HistogramStillCurrent (generation, item))
				return;
			Logger.Log.Debug (ex, "Failed to build histogram for {Path}", item.Path);
			SetHistogram (null);
		}
	}

	// Post returns immediately. Apply and dispose must run on the context captured before
	// ConfigureAwait(false). This process does not reference JoinableTaskFactory.
	static void PostTo (SynchronizationContext context, Action action)
	{
#pragma warning disable VSTHRD001
		context.Post (_ => action (), null);
#pragma warning restore VSTHRD001
	}

	bool HistogramStillCurrent (int generation, PhotoItemViewModel item)
		=> generation == histogramGeneration && ReferenceEquals (item, SelectedPhoto);

	public void SelectFromFilmstrip (PhotoItemViewModel? item)
		=> SelectPhoto (item);

	bool EditorEnabled (string id)
	{
		var row = Extensions.FirstOrDefault (addin => addin.Id == id);
		return row is not null ? row.Enabled : AddinCatalog.IsEnabled (id);
	}

	IReadOnlyList<PhotoItemViewModel> MutationItems ()
	{
		var selected = Photos.Where (item => item.Selected).ToList ();
		if (selected.Count > 0)
			return selected;
		return SelectedPhoto is not null ? new[] { SelectedPhoto } : Array.Empty<PhotoItemViewModel> ();
	}

	IEnumerable<Photo> SelectedCatalogPhotos ()
		=> MutationItems ().Select (item => item.Photo).OfType<Photo> ();

	void NotifySelectionCommands ()
	{
		OnPropertyChanged (nameof (CountText));
		PreviousCommand.NotifyCanExecuteChanged ();
		NextCommand.NotifyCanExecuteChanged ();
		AttachTagCommand.NotifyCanExecuteChanged ();
		RemoveTagCommand.NotifyCanExecuteChanged ();
		RemoveFromCatalogCommand.NotifyCanExecuteChanged ();
		DeleteFromDriveCommand.NotifyCanExecuteChanged ();
		RotateLeftCommand.NotifyCanExecuteChanged ();
		RotateRightCommand.NotifyCanExecuteChanged ();
		CreateVersionCommand.NotifyCanExecuteChanged ();
		DeleteVersionCommand.NotifyCanExecuteChanged ();
		RenameVersionCommand.NotifyCanExecuteChanged ();
		DetachVersionCommand.NotifyCanExecuteChanged ();
		AdjustTimeCommand.NotifyCanExecuteChanged ();
		RefreshThumbnailCommand.NotifyCanExecuteChanged ();
		CopyCommand.NotifyCanExecuteChanged ();
		SetAsBackgroundCommand.NotifyCanExecuteChanged ();
		PrintCommand.NotifyCanExecuteChanged ();
		MailCommand.NotifyCanExecuteChanged ();
		ExportCommand.NotifyCanExecuteChanged ();
		ExportZipCommand.NotifyCanExecuteChanged ();
		SelectNoneCommand.NotifyCanExecuteChanged ();
		SharpenCommand.NotifyCanExecuteChanged ();
		ToggleLoupeCommand.NotifyCanExecuteChanged ();
		OnPropertyChanged (nameof (HasPhotoSelection));
		OnPropertyChanged (nameof (HasSinglePhoto));
		OnPropertyChanged (nameof (CanOpenVersionMenu));
		OnPropertyChanged (nameof (CanExport));
		OnPropertyChanged (nameof (CanModifyCurrentVersion));
		OnPropertyChanged (nameof (CanSharpenOrLoupe));
	}

	void NotifyViewLayout ()
	{
		OnPropertyChanged (nameof (IsBrowsing));
		OnPropertyChanged (nameof (IsEditing));
		OnPropertyChanged (nameof (ShowTimeline));
		OnPropertyChanged (nameof (ShowFilmstrip));
		OnPropertyChanged (nameof (ShowTagBar));
		OnPropertyChanged (nameof (ShowEmptyLibrary));
		OnPropertyChanged (nameof (ShowNoMatches));
		OnPropertyChanged (nameof (ShowQueryBarNoMatches));
		OnPropertyChanged (nameof (ShowFindBarNoMatches));
		OnPropertyChanged (nameof (ShowPhotoGrid));
		OnPropertyChanged (nameof (VisibleEditors));
		OnPropertyChanged (nameof (ShowLoupe));
		OnPropertyChanged (nameof (CanUseTimeline));
		OnPropertyChanged (nameof (CanUseFilmstrip));
		OnPropertyChanged (nameof (CanSharpenOrLoupe));
		PreviousCommand.NotifyCanExecuteChanged ();
		NextCommand.NotifyCanExecuteChanged ();
		SharpenCommand.NotifyCanExecuteChanged ();
		ToggleLoupeCommand.NotifyCanExecuteChanged ();
	}

	partial void OnViewModeChanged (LibraryViewMode value)
	{
		if (value != LibraryViewMode.PhotoView) {
			CancelEditor ();
			IsLoupeVisible = false;
		}
		NotifyViewLayout ();
		RequestPreview ();
		if (value == LibraryViewMode.PhotoView) {
			if (SidebarTab != 2)
				browseSidebarTab = SidebarTab;
			SidebarTab = 2;
		} else if (SidebarTab == 2) {
			SidebarTab = browseSidebarTab;
		}
	}

	partial void OnSidebarTabChanged (int value)
	{
		if (value != 2)
			browseSidebarTab = value;
		OnPropertyChanged (nameof (SidebarPageTitle));
		OnPropertyChanged (nameof (IsTagsPage));
		OnPropertyChanged (nameof (IsFoldersPage));
		OnPropertyChanged (nameof (IsEditPage));
		OnPropertyChanged (nameof (ShowEditorList));
	}

	partial void OnSelectedPhotoChanged (PhotoItemViewModel? value)
	{
		// The edited preview belongs to the previous photo. GTK drops it and histograms the file.
		ClearLivePreview ();
		if (value is null) {
			Info.Clear ();
			RequestHistogram (null);
		} else {
			Info.Update (value.Photo);
			RequestHistogram (value);
		}

		RequestPreview ();
		UpdateStatus ();
		NotifySelectionCommands ();
		OnPropertyChanged (nameof (ShowInfoBox));
	}

	partial void OnSelectedTagChanged (TagNode? value)
	{
		AttachTagCommand.NotifyCanExecuteChanged ();
		RemoveTagCommand.NotifyCanExecuteChanged ();
		DeleteTagCommand.NotifyCanExecuteChanged ();
		EditTagCommand.NotifyCanExecuteChanged ();
	}

	partial void OnActiveTagFilterChanged (TagNode? value)
	{
		ReloadPhotos ();
		NotifyFilterBar ();
	}

	partial void OnActiveFolderFilterChanged (FolderNode? value)
	{
		ReloadPhotos ();
		NotifyFilterBar ();
	}

	void NotifyFilterBar ()
	{
		OnPropertyChanged (nameof (HasLibraryFilter));
		OnPropertyChanged (nameof (ShowEmptyLibrary));
		OnPropertyChanged (nameof (ShowNoMatches));
		OnPropertyChanged (nameof (ShowQueryBarNoMatches));
		OnPropertyChanged (nameof (ShowFindBarNoMatches));
		OnPropertyChanged (nameof (ShowPhotoGrid));
		ClearTagFilterCommand.NotifyCanExecuteChanged ();
		ClearFolderFilterCommand.NotifyCanExecuteChanged ();
		ClearLibraryFiltersCommand.NotifyCanExecuteChanged ();
		ClearVisibleFiltersCommand.NotifyCanExecuteChanged ();
	}

	partial void OnReverseOrderChanged (bool value)
		=> ReloadPhotos ();

	partial void OnFindUntaggedChanged (bool value)
	{
		ReloadPhotos ();
		NotifyFilterBar ();
	}

	partial void OnFindTextChanged (string value)
	{
		ParseFindText ();
		ApplyLocalFilters ();
	}

	partial void OnIsFindBarVisibleChanged (bool value)
		=> OnPropertyChanged (nameof (ShowFindBarNoMatches));

	partial void OnSelectedTimelineChanged (TimelineBucket? value)
	{
		HighlightTimelineRange ();
		ApplyLocalFilters ();
	}

	partial void OnDisplayTimelineChanged (bool value)
		=> OnPropertyChanged (nameof (ShowTimeline));

	partial void OnDisplayFilmstripChanged (bool value)
		=> OnPropertyChanged (nameof (ShowFilmstrip));

	partial void OnZoomChanged (double value)
	{
		OnPropertyChanged (nameof (TileSize));
		OnPropertyChanged (nameof (GridCellWidth));
		OnPropertyChanged (nameof (GridCellHeight));
		OnPropertyChanged (nameof (FilmstripPaneHeight));
		OnPropertyChanged (nameof (CanZoomIn));
		OnPropertyChanged (nameof (CanZoomOut));
		ZoomInCommand.NotifyCanExecuteChanged ();
		ZoomOutCommand.NotifyCanExecuteChanged ();
		_ = LoadThumbnailsAsync ();
	}

	partial void OnUiScaleChanged (double value)
	{
		_ = LoadThumbnailsAsync ();
		RequestPreview ();
	}

	internal Task ReloadThumbnailsAsync ()
		=> LoadThumbnailsAsync ();

	[RelayCommand]
	void Browse ()
		=> ViewMode = LibraryViewMode.IconView;

	[RelayCommand]
	void Edit ()
		=> ViewMode = LibraryViewMode.PhotoView;

	public void SelectPhoto (PhotoItemViewModel? item, bool additive = false)
	{
		if (item is null)
			return;

		if (!additive) {
			foreach (var photo in Photos)
				photo.Selected = ReferenceEquals (photo, item);
			SelectedPhoto = item;
			return;
		}

		if (item.Selected && Photos.Count (photo => photo.Selected) > 1) {
			item.Selected = false;
			if (ReferenceEquals (SelectedPhoto, item))
				SelectedPhoto = Photos.FirstOrDefault (photo => photo.Selected);
			else {
				UpdateStatus ();
				NotifySelectionCommands ();
			}

			return;
		}

		item.Selected = true;
		SelectedPhoto = item;
	}

	[RelayCommand]
	void OpenPhoto (PhotoItemViewModel? item)
	{
		if (item is null)
			return;
		SelectPhoto (item);
		ViewMode = LibraryViewMode.PhotoView;
	}

	[RelayCommand (CanExecute = nameof (CanGoPrevious))]
	void Previous ()
	{
		int index = SelectedPhoto is null ? -1 : Photos.IndexOf (SelectedPhoto);
		if (index > 0)
			SelectPhoto (Photos[index - 1]);
	}

	bool CanGoPrevious ()
		=> SelectedPhoto is not null && Photos.IndexOf (SelectedPhoto) > 0;

	[RelayCommand (CanExecute = nameof (CanGoNext))]
	void Next ()
	{
		int index = SelectedPhoto is null ? -1 : Photos.IndexOf (SelectedPhoto);
		if (index >= 0 && index + 1 < Photos.Count)
			SelectPhoto (Photos[index + 1]);
	}

	bool CanGoNext ()
	{
		int index = SelectedPhoto is null ? -1 : Photos.IndexOf (SelectedPhoto);
		return index >= 0 && index + 1 < Photos.Count;
	}

	[RelayCommand]
	void SelectAll ()
	{
		foreach (var item in Photos)
			item.Selected = true;
		if (SelectedPhoto is null)
			SelectedPhoto = Photos.FirstOrDefault ();
		else {
			UpdateStatus ();
			NotifySelectionCommands ();
		}
	}

	[RelayCommand (CanExecute = nameof (HasPhotoSelection))]
	void SelectNone ()
	{
		foreach (var item in Photos)
			item.Selected = false;
		SelectedPhoto = null;
	}

	[RelayCommand]
	void SelectInvert ()
	{
		foreach (var item in Photos)
			item.Selected = !item.Selected;
		SelectedPhoto = Photos.FirstOrDefault (item => item.Selected);
		if (SelectedPhoto is null) {
			Info.Clear ();
			RequestHistogram (null);
			RequestPreview ();
		}

		UpdateStatus ();
		NotifySelectionCommands ();
	}

	public bool CanZoomIn => Zoom < 256;
	public bool CanZoomOut => Zoom > 64;

	[RelayCommand (CanExecute = nameof (CanZoomIn))]
	void ZoomIn ()
		=> Zoom = Math.Min (256, Zoom + 16);

	[RelayCommand (CanExecute = nameof (CanZoomOut))]
	void ZoomOut ()
		=> Zoom = Math.Max (64, Zoom - 16);

	[RelayCommand]
	void ToggleFilmstripSide ()
	{
		FilmstripOnSide = !FilmstripOnSide;
		Preferences.Set (Preferences.FilmstripOrientation, FilmstripOnSide ? 1 : 0);
		OnPropertyChanged (nameof (FilmstripEdge));
		OnPropertyChanged (nameof (FilmstripBreadth));
		OnPropertyChanged (nameof (FilmstripSpan));
		OnPropertyChanged (nameof (FilmstripFlow));
		OnPropertyChanged (nameof (FilmstripOnSide));
		OnPropertyChanged (nameof (FilmstripHorizontal));
	}

	[RelayCommand]
	void SetTagIconSize (string size)
	{
		if (!int.TryParse (size, out int pixels))
			return;
		Tag.TagIconSize = (IconSize)pixels;
		Preferences.Set (Preferences.TagIconSize, pixels);
		OnPropertyChanged (nameof (TagIconEdge));
		OnPropertyChanged (nameof (TagIconsVisible));
		OnPropertyChanged (nameof (TagIconHidden));
		OnPropertyChanged (nameof (TagIconSmall));
		OnPropertyChanged (nameof (TagIconMedium));
		OnPropertyChanged (nameof (TagIconLarge));
	}

	public bool TagIconHidden => Tag.TagIconSize == IconSize.Hidden;
	public bool TagIconSmall => Tag.TagIconSize == IconSize.Small;
	public bool TagIconMedium => Tag.TagIconSize == IconSize.Medium;
	public bool TagIconLarge => Tag.TagIconSize == IconSize.Large;

	[RelayCommand]
	void OpenTagTyping ()
	{
		if (SelectedPhoto is null && !Photos.Any (photo => photo.Selected))
			return;
		IsTagTypingOpen = true;
	}

	[RelayCommand]
	void HideTagTyping ()
		=> IsTagTypingOpen = false;

	[RelayCommand]
	void ApplyTypedTags ()
	{
		if (database is null)
			return;
		var names = TagTyping.Names (TagTypingText);
		if (names.Count == 0)
			return;
		var photos = Photos.Where (item => item.Selected).Select (item => item.Photo).OfType<Photo> ().ToArray ();
		if (photos.Length == 0 && SelectedPhoto?.Photo is Photo selected)
			photos = new[] { selected };
		if (photos.Length == 0)
			return;
		foreach (string name in names) {
			var tag = database.Tags.GetTagByName (name) ?? database.Tags.CreateCategory (database.Tags.RootCategory, name, true);
			foreach (var photo in photos)
				photo.AddTag (tag);
		}
		database.Photos.Commit (photos);
		foreach (var item in Photos.Where (item => item.Photo is Photo photo && photos.Contains (photo)))
			item.RefreshCaptions ();
		if (SelectedPhoto is not null)
			Info.Update (SelectedPhoto.Photo);
		TagTypingText = string.Empty;
		IsTagTypingOpen = false;
	}

	public Task ShowHoverAsync (PhotoItemViewModel item, int screenWidth, int screenHeight)
	{
		if (!ShowHoverPreviewEnabled || IsEditing)
			return Task.CompletedTask;
		int generation = ++hoverGeneration;
		int edge = screenWidth > 0 && screenHeight > 0 ? HoverPreview.Edge (screenWidth, screenHeight) : 480;
		return LoadHoverAsync (item, edge, generation);
	}

	async Task LoadHoverAsync (PhotoItemViewModel item, int edge, int generation)
	{
		Bitmap? bitmap = null;
		try {
			bitmap = await thumbnails.LoadAsync (item.Path, edge, CancellationToken.None);
			if (generation != hoverGeneration || !ShowHoverPreviewEnabled || IsEditing) {
				bitmap?.Dispose ();
				return;
			}

			HoverEdge = edge;
			HoverDetail = HoverPreview.Caption (item.Photo.Description, item.Photo.Time, item.Photo.Name);
			SetHoverBitmap (bitmap);
			ShowHoverPreview = true;
		} catch (Exception ex) {
			bitmap?.Dispose ();
			Logger.Log.Debug (ex, "Failed to load the hover preview for {Path}", item.Path);
		}
	}

	public void HideHover ()
		=> ClearHover ();

	void ClearHover ()
	{
		hoverGeneration++;
		ShowHoverPreview = false;
		HoverDetail = string.Empty;
		SetHoverBitmap (null);
	}

	void SetHoverBitmap (Bitmap? next)
	{
		var previous = HoverThumbnail;
		HoverThumbnail = next;
		ReleasedBitmap.DisposePrevious (previous, next);
	}

	[RelayCommand]
	void ShowFind ()
	{
		IsFindBarVisible = true;
		shell.FocusFind ();
	}

	[RelayCommand]
	void HideFind ()
	{
		IsFindBarVisible = false;
		FindText = string.Empty;
	}

	[RelayCommand]
	void ToggleFullscreen ()
		=> ShowFullScreen (playSlideshow: false);

	[RelayCommand]
	void CloseWindow ()
		=> shell.Close ();

	[RelayCommand]
	void Quit ()
		=> shell.Quit ();

	[RelayCommand]
	Task NotYetAsync ()
		=> shell.ShowWarningAsync (WindowTitle, Strings.ThisFeatureIsNotYetAvailable);

	[RelayCommand (CanExecute = nameof (CanSharpenOrLoupe))]
	void ToggleLoupe ()
		=> IsLoupeVisible = !IsLoupeVisible;

	public void MoveLoupe (double viewX, double viewY, double viewWidth, double viewHeight)
	{
		if (!IsLoupeVisible || Preview is null)
			return;
		if (viewWidth <= 0 || viewHeight <= 0)
			return;

		var image = Preview.PixelSize;
		var point = PhotoViewTransform.MapPoint (
			(int)viewWidth, (int)viewHeight, image.Width, image.Height,
			PreviewZoom, PreviewPanX, PreviewPanY, viewX, viewY);
		var region = LoupeRegion.Around (image.Width, image.Height, point.X, point.Y);
		if (region.IsEmpty)
			return;

		LoupeSample = new CroppedBitmap (Preview, new global::Avalonia.PixelRect (region.X, region.Y, region.Width, region.Height));
		LoupeX = viewX;
		LoupeY = viewY;
	}

	partial void OnIsLoupeVisibleChanged (bool value)
		=> OnPropertyChanged (nameof (ShowLoupe));

	[RelayCommand (CanExecute = nameof (CanSharpenOrLoupe))]
	Task SharpenAsync ()
		=> ApplyEditorAsync (PhotoEditorCatalog.Sharpen);

	[RelayCommand]
	Task ApplyEditorAsync (PhotoEditorDescriptor? editor)
	{
		if (editor is null)
			return Task.CompletedTask;

		if (PhotoEditorSession.IsInstant (editor))
			return ApplyInstantEditorAsync (editor);

		if (PhotoEditorSession.NeedsSession (editor)) {
			if (SelectedCatalogPhotos ().Any () == false && SelectedPhoto is null)
				return shell.ShowWarningAsync (editor.Label, Strings.NoSelectionAvailable);

			int maxEdge = Math.Max (PreviewPixelWidth, PreviewPixelHeight);
			EditorSession = new PhotoEditorSession (editor, maxEdge);
			return Task.CompletedTask;
		}

		return shell.ShowWarningAsync (editor.Label, string.Format (Strings.TheXEditorIsNotYetAvailable, editor.Label));
	}

	Task ApplyInstantEditorAsync (PhotoEditorDescriptor editor)
	{
		var photos = SelectedCatalogPhotos ().ToArray ();
		if (photos.Length == 0)
			return shell.ShowWarningAsync (editor.Label, Strings.NoSelectionAvailable);

		try {
			var factory = TinyIoCContainer.Current.Resolve<IImageFileFactory> ();
			PhotoEditorApply.Apply (editor, photos, new PhotoEditorContext (), factory, database?.Photos);
			RefreshEditedSelection ();
		} catch (Exception ex) {
			Logger.Log.Debug (ex, "Failed to apply {Editor}", editor.Id);
			return shell.ShowWarningAsync (Strings.ErrorEditingPhoto, ex.Message);
		}

		return Task.CompletedTask;
	}

	public void SetEditorSelection (PixelRect selection)
	{
		if (EditorSession is null)
			return;
		EditorSession.Selection = selection;
		CommitEditorCommand.NotifyCanExecuteChanged ();
		OnPropertyChanged (nameof (EditorSession));
	}

	[RelayCommand]
	void SelectEditorRegion (PixelRect selection)
		=> SetEditorSelection (selection);

	[RelayCommand]
	void CancelEditor ()
	{
		if (EditorSession is null)
			return;
		EditorSession = null;
	}

	bool CanCommitEditor ()
		=> EditorSession?.CanCommit == true;

	[RelayCommand (CanExecute = nameof (CanCommitEditor))]
	Task CommitEditorAsync ()
	{
		if (EditorSession is null || !EditorSession.CanCommit)
			return Task.CompletedTask;

		var photos = SelectedCatalogPhotos ().ToArray ();
		if (photos.Length == 0)
			return shell.ShowWarningAsync (EditorSession.Editor.Label, Strings.NoSelectionAvailable);

		var editor = EditorSession.Editor;
		var context = EditorSession.CreateContext ();
		try {
			var factory = TinyIoCContainer.Current.Resolve<IImageFileFactory> ();
			PhotoEditorApply.Apply (editor, photos, context, factory, database?.Photos);
			EditorSession = null;
			RefreshEditedSelection ();
		} catch (Exception ex) {
			Logger.Log.Debug (ex, "Failed to apply {Editor}", editor.Id);
			return shell.ShowWarningAsync (Strings.ErrorEditingPhoto, ex.Message);
		}

		return Task.CompletedTask;
	}

	partial void OnEditorSessionChanged (PhotoEditorSession? value)
	{
		OnPropertyChanged (nameof (HasEditorSession));
		OnPropertyChanged (nameof (ShowEditorList));
		OnPropertyChanged (nameof (ShowEditorSelection));
		OnPropertyChanged (nameof (ShowEditorSettings));
		OnPropertyChanged (nameof (ShowColorSettings));
		OnPropertyChanged (nameof (ShowBwSettings));
		OnPropertyChanged (nameof (ShowResizeSettings));
		OnPropertyChanged (nameof (ShowTiltSettings));
		OnPropertyChanged (nameof (ShowSoftFocusSettings));
		OnPropertyChanged (nameof (ShowSharpenSettings));
		CommitEditorCommand.NotifyCanExecuteChanged ();
	}

	[RelayCommand]
	async Task ImportAsync ()
	{
		if (await shell.ShowImportAsync ())
			LoadCatalog ();
	}

	public async Task ImportDroppedAsync (IReadOnlyList<string> paths, Func<string, bool>? canLoad = null)
	{
		var plan = FileDropImport.Classify (paths, canLoad ?? CanLoadDroppedFile);
		string? report = FileDropImport.Report (plan.Rejected);
		if (report is not null)
			await shell.ShowWarningAsync (Strings.UnsupportedFileFormat, report);
		if (plan.Accepted.Count == 0)
			return;
		if (await shell.ShowImportAsync (plan.Accepted))
			LoadCatalog ();
	}

	public void RateFocused (uint rating)
	{
		if (SelectedPhoto is null)
			return;
		Info.RatingValue = rating;
	}

	static bool CanLoadDroppedFile (string path)
	{
		try {
			var factory = TinyIoCContainer.Current.Resolve<IImageFileFactory> ();
			return factory.HasLoader (new SafeUri (path));
		} catch (Exception ex) {
			Logger.Log.Debug (ex, "Cannot classify dropped file {Path}", path);
			return false;
		}
	}

	[RelayCommand]
	Task AboutAsync ()
		=> shell.ShowAboutAsync ();

	[RelayCommand]
	void SelectTag (TagNode? node)
	{
		if (KeepsTagHighlight (node))
			return;
		SelectedTag = node;
	}

	[RelayCommand]
	void ActivateTag (TagNode? node)
	{
		if (node is null)
			return;
		ActiveTagFilter = node;
		if (FindTags.All (tag => tag.Tag.Id != node.Tag.Id))
			FindTags.Add (node);
		ApplyLocalFilters ();
		NotifyFindTerms ();
	}

	[RelayCommand (CanExecute = nameof (CanClearTagFilter))]
	void ClearTagFilter ()
	{
		ActiveTagFilter = null;
		FindTags.Clear ();
		ApplyLocalFilters ();
		NotifyFindTerms ();
	}

	bool CanClearTagFilter ()
		=> ActiveTagFilter is not null;

	[RelayCommand]
	void SelectFolder (FolderNode? node)
		=> SelectedFolder = node;

	[RelayCommand]
	void ActivateFolder (FolderNode? node)
	{
		if (node is not null)
			ActiveFolderFilter = node;
	}

	[RelayCommand (CanExecute = nameof (CanClearFolderFilter))]
	void ClearFolderFilter ()
		=> ActiveFolderFilter = null;

	bool CanClearFolderFilter ()
		=> ActiveFolderFilter is not null;

	[RelayCommand (CanExecute = nameof (CanClearLibraryFilters))]
	void ClearLibraryFilters ()
	{
		ActiveTagFilter = null;
		ActiveFolderFilter = null;
		FindUntagged = false;
		FindTags.Clear ();
		TagQuery.Clear ();
		ClearTermQuery ();
		ClearFindTerms ();
	}

	bool CanClearLibraryFilters ()
		=> HasLibraryFilter;

	[RelayCommand (CanExecute = nameof (CanClearVisibleFilters))]
	void ClearVisibleFilters ()
	{
		FindText = string.Empty;
		IsFindBarVisible = false;
		SelectedTimeline = null;
		timelineRangeStart = null;
		timelineRangeEnd = null;
		ActiveTagFilter = null;
		ActiveFolderFilter = null;
		FindUntagged = false;
		FindTags.Clear ();
		TagQuery.Clear ();
		ClearTermQuery ();
		ClearFindTerms ();
	}

	bool CanClearVisibleFilters ()
		=> HasLibraryFilter || !string.IsNullOrWhiteSpace (FindText) || SelectedTimeline is not null
			|| timelineRangeStart is not null;

	void ClearFindTerms ()
	{
		FindMinRating = null;
		FindMaxRating = null;
		FindStart = null;
		FindEnd = null;
		findRollIds.Clear ();
		NotifyFindTerms ();
		ReloadPhotos ();
	}

	[RelayCommand]
	void ClearUntagged ()
		=> FindUntagged = false;

	[RelayCommand]
	void ToggleFindMatchAllTags ()
	{
		FindMatchAllTags = !FindMatchAllTags;
		ApplyLocalFilters ();
	}

	[RelayCommand]
	async Task FindByRatingAsync ()
	{
		var initial = RatingFilterLogic.Initial (FindMinRating, FindMaxRating);
		var range = await shell.PromptRatingFilterAsync (initial.Min, initial.Max);
		if (range is null)
			return;
		FindMinRating = range.Value.Min;
		FindMaxRating = range.Value.Max;
		NotifyFindTerms ();
		ReloadPhotos ();
	}

	[RelayCommand]
	void DismissFindRating ()
	{
		FindMinRating = null;
		FindMaxRating = null;
		NotifyFindTerms ();
		ReloadPhotos ();
	}

	[RelayCommand]
	async Task FindByDateAsync ()
	{
		var choice = await shell.PromptDateRangeFilterAsync (FindStart, FindEnd);
		if (choice is null)
			return;
		FindStart = choice.Range?.Start;
		FindEnd = choice.Range?.End;
		NotifyFindTerms ();
		ReloadPhotos ();
	}

	[RelayCommand]
	void DismissFindDate ()
	{
		FindStart = null;
		FindEnd = null;
		NotifyFindTerms ();
		ReloadPhotos ();
	}

	[RelayCommand]
	async Task FindByImportRollAsync ()
	{
		var ids = await shell.PickImportRollsAsync (ImportRollOptions ());
		if (ids is null || ids.Count == 0)
			return;
		findRollIds.Clear ();
		findRollIds.AddRange (ids.Distinct ());
		NotifyFindTerms ();
		ReloadPhotos ();
	}

	[RelayCommand]
	void DismissFindRoll ()
	{
		findRollIds.Clear ();
		NotifyFindTerms ();
		ReloadPhotos ();
	}

	[RelayCommand]
	void SetTimelineRange ((int Min, int Max) range)
	{
		if (TimelineBuckets.Count == 0)
			return;
		var span = TimelineMonths.InclusiveRange (
			TimelineBuckets.Select (bucket => new TimelineMonth (bucket.Start, bucket.End, bucket.Count)).ToList (),
			range.Min, range.Max);
		timelineRangeStart = span.Start;
		timelineRangeEnd = span.End;
		SelectedTimeline = null;
		HighlightTimelineRange ();
		ApplyLocalFilters ();
	}

	[RelayCommand]
	void Slideshow ()
		=> ShowFullScreen (playSlideshow: true);

	[RelayCommand (CanExecute = nameof (HasPhotoSelection))]
	async Task ExportAsync ()
	{
		var paths = PhotoFileActions.SelectedPaths (MutationItems ().Select (item => item.Photo));
		if (paths.Count == 0) {
			await shell.ShowWarningAsync (Strings.Export, Strings.NoSelectionAvailable);
			return;
		}
		string? folder = await shell.PickFolderAsync (Strings.SelectExportFolder);
		if (string.IsNullOrWhiteSpace (folder))
			return;
		PhotoFileActions.CopyToFolder (paths, folder);
	}

	[RelayCommand (CanExecute = nameof (HasPhotoSelection))]
	async Task ExportZipAsync ()
	{
		var paths = PhotoFileActions.SelectedPaths (MutationItems ().Select (item => item.Photo));
		if (paths.Count == 0) {
			await shell.ShowWarningAsync (Strings.Export, Strings.NoSelectionAvailable);
			return;
		}
		string? folder = await shell.PickFolderAsync (Strings.SelectExportFolder);
		if (string.IsNullOrWhiteSpace (folder))
			return;
		string? name = await shell.PromptAsync (Strings.CompressedFile, Strings.FileName, "f-spot_export.zip");
		if (name is not string zipName || !ZipStaysInFolder (folder, zipName))
			return;
		PhotoFileActions.WriteZip (paths, Path.Combine (folder, zipName));
	}

	// GTK enables Create only when the name matches "[.]zip$". A slash would
	// also let the prompt write outside the folder the person just chose.
	static bool ZipStaysInFolder (string folder, string name)
	{
		if (string.IsNullOrWhiteSpace (name) || !name.EndsWith (".zip", StringComparison.Ordinal))
			return false;
		if (name.IndexOfAny (new[] { '/', '\\' }) >= 0)
			return false;
		string root = Path.GetFullPath (folder);
		string zipPath = Path.GetFullPath (Path.Combine (root, name));
		string prefix = root.TrimEnd (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
		return zipPath.StartsWith (prefix, StringComparison.Ordinal);
	}

	[RelayCommand (CanExecute = nameof (CanMutateSelection))]
	async Task CopyAsync ()
	{
		var paths = PhotoFileActions.SelectedPaths (MutationItems ().Select (item => item.Photo));
		if (paths.Count == 0) {
			await shell.ShowWarningAsync (Strings.Copy, Strings.NoSelectionAvailable);
			return;
		}
		await shell.CopyPhotosAsync (paths);
	}

	[RelayCommand]
	async Task PageSetupAsync ()
	{
		var setup = await shell.PromptPageSetupAsync (PageSetup);
		if (setup is null)
			return;
		PageSetup = setup;
	}

	[RelayCommand (CanExecute = nameof (HasSinglePhoto))]
	async Task SetAsBackgroundAsync ()
	{
		var paths = PhotoFileActions.SelectedPaths (MutationItems ().Select (item => item.Photo));
		if (paths.Count == 0) {
			await shell.ShowWarningAsync (Strings.SetAsBackground, Strings.NoSelectionAvailable);
			return;
		}
		await shell.SetDesktopBackgroundAsync (paths[0]);
	}

	[RelayCommand]
	Task ShowToolsAsync ()
		=> ShowExtensionsAsync ();

	[RelayCommand (CanExecute = nameof (HasPhotoSelection))]
	async Task PrintAsync ()
	{
		var photos = MutationItems ().Select (item => item.Photo).ToList ();
		if (PhotoFileActions.SelectedPaths (photos).Count == 0) {
			await shell.ShowWarningAsync (Strings.Print, Strings.NoSelectionAvailable);
			return;
		}
		if (await shell.PrintPhotoItemsAsync (photos, PageSetup) is PrintPageSetup setup)
			PageSetup = setup;
	}

	[RelayCommand (CanExecute = nameof (HasPhotoSelection))]
	async Task MailAsync ()
	{
		var paths = PhotoFileActions.SelectedPaths (MutationItems ().Select (item => item.Photo));
		if (paths.Count == 0) {
			await shell.ShowWarningAsync (Strings.SendMail, Strings.NoSelectionAvailable);
			return;
		}
		await shell.MailPhotosAsync (paths);
	}

	[RelayCommand]
	void ShowTagsPage ()
		=> SidebarTab = 0;

	[RelayCommand]
	void ShowFoldersPage ()
		=> SidebarTab = 1;

	[RelayCommand]
	void ShowEditPage ()
		=> SidebarTab = 2;

	[RelayCommand]
	void HideSidebar ()
		=> DisplaySidebar = false;

	[RelayCommand]
	void ToggleSidebar ()
		=> DisplaySidebar = !DisplaySidebar;

	[RelayCommand]
	void SelectTimeline (TimelineBucket? bucket)
	{
		if (bucket is null || !bucket.HasPhotos)
			return;
		timelineRangeStart = null;
		timelineRangeEnd = null;
		SelectedTimeline = ReferenceEquals (SelectedTimeline, bucket) ? null : bucket;
	}

	[RelayCommand]
	async Task CreateTagAsync ()
	{
		if (database is null)
			return;

		await CreateTagWithDialogAsync (database);
	}

	[RelayCommand (CanExecute = nameof (CanTargetTag))]
	async Task EditTagAsync (TagNode? node)
	{
		var target = node ?? SelectedTag;
		if (database is null || target is null)
			return;

		await EditTagWithDialogAsync (database, target.Tag);
	}

	bool CanTargetTag (TagNode? node)
		=> (node ?? SelectedTag) is not null;

	[RelayCommand (CanExecute = nameof (CanTargetTag))]
	async Task DeleteTagAsync (TagNode? node)
	{
		var target = node ?? SelectedTag;
		if (target is null)
			return;

		if (TagDelete.HasChildren (target.Tag)) {
			await shell.ShowWarningAsync (TagDelete.BlockedHeading, TagDelete.BlockedMessage (target.Tag));
			return;
		}

		if (database is null)
			return;

		if (!await shell.ConfirmAsync (
				TagDelete.ConfirmHeading (target.Tag),
				TagDelete.ConfirmBody (0),
				Strings.Delete))
			return;

		try {
			database.Photos.Remove (new List<Tag> { target.Tag });
		} catch (InvalidTagOperationException ex) {
			await shell.ShowWarningAsync (TagDelete.BlockedHeading, TagDelete.BlockedMessage (ex.Tag ?? target.Tag));
			return;
		}

		if (SelectedTag?.Tag.Id == target.Tag.Id)
			SelectedTag = null;
		ReloadTags ();
		ReloadPhotos ();
	}

	public void AttachDroppedTag (string? tagIdText, PhotoItemViewModel? dropTarget)
	{
		if (!uint.TryParse (tagIdText, out uint id))
			return;
		var node = FindTagNodeById (Tags, id);
		if (node is null)
			return;
		AttachTagToItems (node.Tag, PhotoDropTargets.ForTagDrop (dropTarget, Photos));
	}

	public void AttachSelectionToTag (TagNode? node)
	{
		if (node is null)
			return;
		AttachTagToItems (node.Tag, Photos.Where (photo => photo.Selected).ToList ());
	}

	void AttachTagToItems (Tag tag, IReadOnlyList<PhotoItemViewModel> items)
	{
		if (database is null || items.Count == 0)
			return;

		var photos = items.Select (item => item.Photo).OfType<Photo> ().ToArray ();
		if (photos.Length == 0)
			return;

		foreach (var photo in photos)
			photo.AddTag (tag);
		database.Photos.Commit (photos);
		RefreshSelectedCaptions (photos);
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

	[RelayCommand (CanExecute = nameof (CanAttachTag))]
	void AttachTag (TagNode? node)
	{
		var target = node ?? SelectedTag;
		if (target is null)
			return;
		AttachTagToItems (target.Tag, Photos.Where (photo => photo.Selected).ToList ());
	}

	[RelayCommand (CanExecute = nameof (CanAttachTag))]
	void RemoveTag (TagNode? node)
	{
		var target = node ?? SelectedTag;
		if (database is null || target is null)
			return;

		var photos = SelectedCatalogPhotos ().ToArray ();
		if (photos.Length == 0)
			return;

		foreach (var photo in photos)
			photo.RemoveTag (target.Tag);
		database.Photos.Commit (photos);
		RefreshSelectedCaptions (photos);
	}

	bool CanAttachTag (TagNode? node)
		=> (node ?? SelectedTag) is not null && SelectedCatalogPhotos ().Any ();

	[RelayCommand (CanExecute = nameof (CanMutateSelection))]
	async Task RemoveFromCatalogAsync ()
	{
		if (database is null)
			return;

		var photos = SelectedCatalogPhotos ().ToArray ();
		if (photos.Length == 0)
			return;

		string header = photos.Length <= 1
			? Strings.RemoveTheSelectedPhotoFromFSpot
			: string.Format (Strings.RemoveTheXSelectedPhotosFromFSpot, photos.Length);
		if (!await shell.ConfirmAsync (header, Strings.IfYouRemovePhotosFromFSpotCatalogAllTagInfoLost, Strings.Remove))
			return;

		database.Photos.Remove (photos);
		ReloadPhotos ();
	}

	[RelayCommand (CanExecute = nameof (CanMutateSelection))]
	async Task DeleteFromDriveAsync ()
	{
		if (database is null)
			return;

		var photos = SelectedCatalogPhotos ().ToArray ();
		if (photos.Length == 0)
			return;

		string header = photos.Length <= 1
			? Strings.DeleteTheSelectedPhotoPermanently
			: string.Format (Strings.DeleteTheXSelectedPhotosPermanently, photos.Length);
		string body = photos.Length <= 1
			? Strings.ThisDeletesAllVersionsOfTheSelectedPhotoFromYourDrive
			: Strings.ThisDeletesAllVersionsOfTheSelectedPhotosFromYourDrive;
		if (!await shell.ConfirmAsync (header, body, Strings.Delete))
			return;

		var removed = new List<Photo> ();
		var failures = new List<string> ();
		foreach (var photo in photos) {
			// Captured before any version is removed. Photo.Name reads the original URI.
			string name = PhotoDriveDelete.DisplayName (photo);
			try {
				var versionFailures = PhotoDriveDelete.TryDeleteAllVersions (photo);
				if (versionFailures.Count > 0) {
					foreach (var failure in versionFailures) {
						Logger.Log.Error (failure.Exception, "Failed to delete a version of {Name}", name);
						failures.Add (FormatDeleteFailure (failure.Exception, failure.Path));
					}

					// A photo stays in the catalog when any version file is still on disk.
					database.Photos.Commit (photo);
				} else {
					removed.Add (photo);
				}
			} catch (Exception ex) {
				Logger.Log.Error (ex, "Failed to delete {Name}", name);
				failures.Add (FormatDeleteFailure (ex, name));
			}
		}

		if (removed.Count > 0)
			database.Photos.Remove (removed.ToArray ());
		if (removed.Count > 0 || failures.Count > 0)
			ReloadPhotos ();
		if (failures.Count > 0)
			await shell.ShowWarningAsync (Strings.ErrorDeletingPicture, string.Join (Environment.NewLine + Environment.NewLine, failures));
	}

	static string FormatDeleteFailure (Exception ex, string path)
	{
		if (ex is UnauthorizedAccessException)
			return string.Format (Strings.NoPermissionToDeleteTheFile, path, Environment.NewLine);
		return string.Format (Strings.AnErrorOfTypeXOccurredWhileDeletingTheFile, ex.GetType (), path, Environment.NewLine);
	}

	public bool HasPhotoSelection => MutationItems ().Any ();
	public bool HasSinglePhoto => MutationItems ().Count == 1;
	public bool CanOpenVersionMenu => HasSinglePhoto;
	public bool CanExport => HasPhotoSelection;
	public bool CanSharpenOrLoupe => HasSinglePhoto && IsEditing;

	public bool CanModifyCurrentVersion
	{
		get {
			var items = MutationItems ();
			return items.Count == 1 && items[0].Photo is Photo photo && photo.DefaultVersionId != Photo.OriginalVersionId;
		}
	}

	bool CanMutateSelection ()
		=> HasPhotoSelection;

	[RelayCommand (CanExecute = nameof (CanMutateSelection))]
	void RotateLeft ()
		=> RotateSelection (RotateDirection.Counterclockwise);

	[RelayCommand (CanExecute = nameof (CanMutateSelection))]
	void RotateRight ()
		=> RotateSelection (RotateDirection.Clockwise);

	void RotateSelection (RotateDirection direction)
	{
		var photos = MutationItems ().Select (item => item.Photo).ToArray ();
		if (photos.Length == 0)
			return;

		var thumbnailsService = TinyIoCContainer.Current.Resolve<IThumbnailService> ();
		var op = new RotateMultiple (photos, direction, thumbnailsService);
		try {
			while (op.Step ()) { }
		} catch (RotateException ex) {
			_ = shell.ShowWarningAsync (Strings.ErrorWhileRotatingPhoto, ex.Message);
			return;
		} catch (Exception ex) {
			Logger.Log.Debug (ex, "Failed to rotate selection");
			_ = shell.ShowWarningAsync (Strings.ErrorWhileRotatingPhoto, ex.Message);
			return;
		}

		RefreshEditedSelection ();
	}

	void RefreshEditedSelection ()
	{
		foreach (var item in MutationItems ()) {
			ReplaceThumbnail (item, null);
			thumbnailGeneration.Remove (item);
			item.RefreshFromPhoto ();
		}

		if (SelectedPhoto is not null) {
			Info.Update (SelectedPhoto.Photo);
			RequestPreview ();
			RequestHistogram (SelectedPhoto);
		}

		_ = LoadThumbnailsAsync ();
		NotifySelectionCommands ();
	}

	void RefreshSelectedCaptions (IEnumerable<Photo> photos)
	{
		var ids = photos.Select (photo => photo.Id).ToHashSet ();
		foreach (var item in Photos.Where (item => item.Photo is Photo photo && ids.Contains (photo.Id))) {
			item.RefreshCaptions ();
			if (ReferenceEquals (item, SelectedPhoto))
				Info.Update (item.Photo);
		}
	}

	void ApplyVersion (string name)
	{
		if (database is null || SelectedPhoto?.Photo is not Photo photo)
			return;

		var version = photo.Versions.OfType<PhotoVersion> ().FirstOrDefault (item => item.Name == name);
		if (version is null || ReferenceEquals (version, photo.DefaultVersion))
			return;

		photo.SetDefaultVersion (version);
		database.Photos.Commit (photo);
		SelectedPhoto.RefreshFromPhoto ();
		Info.Update (photo);
		RequestPreview ();
		RequestHistogram (SelectedPhoto);
		_ = LoadThumbnailsAsync ();
		NotifySelectionCommands ();
	}

	[RelayCommand (CanExecute = nameof (HasSinglePhoto))]
	async Task CreateVersionAsync ()
	{
		if (SelectedCatalogPhotos ().FirstOrDefault () is not Photo photo)
			return;
		string? name = await PromptVersionNameAsync (VersionNameRequest.Create, photo);
		if (string.IsNullOrWhiteSpace (name))
			return;
		try {
			PhotoVersionWorkflow.Create (photo, name, p => database?.Photos.Commit (p));
			RefreshEditedSelection ();
		} catch (Exception ex) {
			await shell.ShowWarningAsync (Strings.CreateNewVersion, ex.Message);
		}
	}

	[RelayCommand (CanExecute = nameof (CanModifyCurrentVersion))]
	async Task DeleteVersionAsync ()
	{
		if (SelectedCatalogPhotos ().FirstOrDefault () is not Photo photo)
			return;
		if (!await shell.ConfirmAsync (string.Format (Strings.ReallyDeleteVersionXQuestion, photo.DefaultVersion.Name),
				Strings.ThisRemovesTheVersionAndDeletesTheFileFromDisk, Strings.Delete))
			return;
		try {
			PhotoVersionWorkflow.Delete (photo, p => database?.Photos.Commit (p));
			RefreshEditedSelection ();
		} catch (Exception ex) {
			await shell.ShowWarningAsync (Strings.DeleteVersion, ex.Message);
		}
	}

	[RelayCommand (CanExecute = nameof (CanModifyCurrentVersion))]
	async Task RenameVersionAsync ()
	{
		if (SelectedCatalogPhotos ().FirstOrDefault () is not Photo photo)
			return;
		string? name = await PromptVersionNameAsync (VersionNameRequest.Rename, photo);
		if (string.IsNullOrWhiteSpace (name))
			return;
		try {
			PhotoVersionWorkflow.Rename (photo, name, p => database?.Photos.Commit (p));
			RefreshEditedSelection ();
		} catch (Exception ex) {
			await shell.ShowWarningAsync (Strings.RenameVersion, ex.Message);
		}
	}

	[RelayCommand (CanExecute = nameof (CanModifyCurrentVersion))]
	async Task DetachVersionAsync ()
	{
		if (SelectedCatalogPhotos ().FirstOrDefault () is not Photo photo || database is null)
			return;
		if (!await shell.ConfirmAsync (string.Format (Strings.ReallyDetachVersionXFromY, photo.DefaultVersion.Name, photo.Name),
				Strings.ThisMakesTheVersionAppearAsASeparatePhotoInLibraryToUndoDragNewPhotoBackToParent, Strings.DetachMnemonic))
			return;
		try {
			PhotoVersionWorkflow.Detach (photo, p => database.Photos.CreateFrom (p, true, p.RollId),
				photos => database.Photos.Commit (photos.ToArray ()));
			ReloadPhotos ();
		} catch (Exception ex) {
			await shell.ShowWarningAsync (Strings.DetachVersion, ex.Message);
		}
	}

	[RelayCommand (CanExecute = nameof (CanMutateSelection))]
	async Task AdjustTimeAsync ()
	{
		var photos = SelectedCatalogPhotos ().ToList ();
		if (photos.Count == 0)
			return;
		var choice = await shell.PromptAdjustTimeAsync (photos.Select (photo => (photo.Name, photo.Time, photo.DefaultVersion.Uri.AbsolutePath)).ToList ());
		if (choice is null)
			return;
		if (choice.ShiftByOffset)
			PhotoTimeAdjust.ShiftEach (photos, TimeSpan.FromHours (choice.Hours), batch => database?.Photos.Commit (batch));
		else
			PhotoTimeAdjust.Space (photos, choice.AnchorIndex,
				choice.AnchorTime ?? photos[Math.Clamp (choice.AnchorIndex, 0, photos.Count - 1)].Time,
				TimeSpan.FromMinutes (choice.SpacingMinutes), batch => database?.Photos.Commit (batch));
		RefreshEditedSelection ();
	}

	[RelayCommand (CanExecute = nameof (CanMutateSelection))]
	void RefreshThumbnail ()
	{
		IThumbnailService? thumbs = null;
		try {
			thumbs = TinyIoCContainer.Current.Resolve<IThumbnailService> ();
		} catch (Exception) {
			// tests and hosts without a container still rebuild the preview
		}
		if (thumbs is not null) {
			foreach (var photo in MutationItems ().Select (item => item.Photo))
				ThumbnailRefresh.Invalidate (thumbs, photo);
		}
		RefreshEditedSelection ();
	}

	[RelayCommand]
	async Task ShowPreferencesAsync ()
	{
		await shell.ShowPreferencesAsync ();
		// Sidebar, timeline and filmstrip are window toggles. Preferences does
		// not write them, so closing the dialog must not snap them back.
		ViewerInterpolation = LibraryPreferences.ViewerInterpolation;
		ShowHoverPreviewEnabled = LibraryPreferences.ShowHoverPreview;
		if (!ShowHoverPreviewEnabled)
			ClearHover ();
		IsMailEnabled = LibraryPreferences.MailToEnabled;
		AppearanceTheme.Apply (global::Avalonia.Application.Current);
	}

	[RelayCommand]
	async Task ShowExtensionsAsync ()
	{
		if (Extensions.Count == 0) {
			foreach (var editor in PhotoEditorCatalog.All)
				Extensions.Add (new AddinStatus (editor.Id, editor.Label, AddinCatalog.IsEnabled (editor.Id)));
		}
		await shell.ShowExtensionsAsync (Extensions);
		OnPropertyChanged (nameof (VisibleEditors));
	}

	[RelayCommand]
	void HelpContents ()
		=> shell.OpenUri (FSpot.HelpContents.Current);

	[RelayCommand]
	async Task ImportFromCameraAsync ()
	{
		FSpot.FileSystem.IFileSystem fileSystem;
		try {
			fileSystem = TinyIoCContainer.Current.Resolve<FSpot.FileSystem.IFileSystem> ();
		} catch (TinyIoC.TinyIoCResolutionException) {
			await shell.ShowWarningAsync (Strings.ImportFromCamera, DeviceImport.EmptyMessage);
			return;
		}
		var sources = DeviceImport.Sources (fileSystem);
		if (sources.Count == 0) {
			await shell.ShowWarningAsync (Strings.ImportFromCamera, DeviceImport.EmptyMessage);
			return;
		}

		// A single detected camera/card goes straight to it, matching the GTK
		// combo pre-selecting the only mounted source; several sources fall
		// back to the Import window's own source picker.
		if (sources.Count == 1) {
			if (await shell.ShowImportAsync (new[] { sources[0].Root.LocalPath }))
				LoadCatalog ();
			return;
		}

		await ImportAsync ();
	}

	[RelayCommand]
	void RenameTag (TagNode? node)
	{
		if (node is null || database is null)
			return;
		_ = RenameTagAsync (node);
	}

	async Task RenameTagAsync (TagNode node)
	{
		string? name = await shell.PromptAsync (Strings.EditTag, Strings.NameOfNewTagColon, node.Name);
		if (string.IsNullOrWhiteSpace (name) || database is null || WarnIfTagNameTaken (node.Tag, name))
			return;
		node.Tag.Name = name;
		database.Tags.Commit (node.Tag);
		ReloadTags ();
	}

	[RelayCommand]
	void ReparentTag ((TagNode Child, TagNode Parent)? move)
	{
		if (move is null)
			return;
		var parent = move.Value.Parent.Tag as Category ?? move.Value.Parent.Tag.Category;
		if (!TagReparent.Move (move.Value.Child.Tag, parent))
			return;
		database?.Tags.Commit (move.Value.Child.Tag);
		if (database is not null)
			ReloadTags ();
	}

	[RelayCommand]
	void BeginRenameTag (TagNode? node)
	{
		if (node is null)
			return;
		SelectedTag = node;
		foreach (var tag in FlattenTags (Tags))
			tag.IsRenaming = false;
		node.IsRenaming = true;
	}

	[RelayCommand]
	void CommitRenameTag (TagNode? node)
	{
		if (node is null || string.IsNullOrWhiteSpace (node.Name))
			return;
		if (WarnIfTagNameTaken (node.Tag, node.Name)) {
			node.Name = node.Tag.Name;
			node.IsRenaming = false;
			return;
		}
		node.Tag.Name = node.Name;
		node.IsRenaming = false;
		database?.Tags.Commit (node.Tag);
	}

	static IEnumerable<TagNode> FlattenTags (IEnumerable<TagNode> nodes)
	{
		foreach (var node in nodes) {
			yield return node;
			foreach (var child in FlattenTags (node.Children))
				yield return child;
		}
	}

	[RelayCommand]
	void ToggleInfoField (string? field)
	{
		switch (field) {
		case "Date":
			InfoVisibility.Date = !InfoVisibility.Date;
			break;
		case "Size":
			InfoVisibility.Size = !InfoVisibility.Size;
			break;
		case "Exposure":
			InfoVisibility.Exposure = !InfoVisibility.Exposure;
			break;
		case "FocalLength":
			InfoVisibility.FocalLength = !InfoVisibility.FocalLength;
			break;
		case "Camera":
			InfoVisibility.Camera = !InfoVisibility.Camera;
			break;
		case "Creator":
			InfoVisibility.Creator = !InfoVisibility.Creator;
			break;
		case "Rating":
			InfoVisibility.Rating = !InfoVisibility.Rating;
			break;
		default:
			return;
		}
		InfoVisibility.Save ();
		OnPropertyChanged (nameof (InfoVisibility));
	}

	public void PanPreviewBy (double dx, double dy)
	{
		PreviewPanX += dx;
		PreviewPanY += dy;
	}

	[RelayCommand]
	void ZoomPreview (double factor)
	{
		PreviewZoom = Math.Clamp (PreviewZoom * factor, 0.25, 8);
	}

	void ApplyRating (uint rating)
	{
		if (SelectedPhoto?.Photo is not Photo photo)
			return;

		photo.Rating = rating > 5 ? 5 : rating;
		database?.Photos.Commit (photo);
		SelectedPhoto.RefreshCaptions ();
	}

	void ApplyDescription (string text)
	{
		if (SelectedPhoto?.Photo is not Photo photo)
			return;
		photo.Description = text ?? string.Empty;
		database?.Photos.Commit (photo);
	}
}
