//
// MainWindowViewModel.Interaction.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FSpot.Avalonia.Controls;
using FSpot.Avalonia.Imaging;
using FSpot.Avalonia.Input;
using FSpot.Avalonia.Models;
using FSpot.Core;
using FSpot.Query;
using FSpot.Resources.Lang;
using FSpot.Settings;
using FSpot.Utils;

using Hyena;

namespace FSpot.Avalonia.ViewModels;

/// <summary>
/// Timeline limits, grid keys and rubber band, viewer keys, loupe dragging, the v/V
/// preview popup, rating steps, and tag tree and tag entry keys.
/// </summary>
public partial class MainWindowViewModel
{
	[ObservableProperty]
	bool displayFilenames = Preferences.Get<bool> (Preferences.ViewerShawFilenames);

	[ObservableProperty]
	double loupeAngle = Math.PI / 4;

	[ObservableProperty]
	bool showPreviewPopup;

	[ObservableProperty]
	Bitmap? previewPopupImage;

	[ObservableProperty]
	Bitmap? previewPopupHistogram;

	[ObservableProperty]
	string previewPopupDetail = string.Empty;

	[ObservableProperty]
	int previewPopupEdge = 480;

	DateTime? timelineLimitStart;
	DateTime? timelineLimitEnd;
	bool loupePlaced;
	int previewPopupGeneration;
	CancellationTokenSource? popupDecode;

	public bool IsPreviewZoomed => !Imaging.ViewerZoom.IsFit (PreviewZoom);

	public PhotoItemViewModel? PreviewPopupItem { get; private set; }

	public bool PreviewPopupShowsHistogram { get; private set; }

	partial void OnDisplayFilenamesChanged (bool value)
		=> Preferences.Set (Preferences.ViewerShawFilenames, value);

	partial void OnPreviewZoomChanged (double value)
		=> OnPropertyChanged (nameof (IsPreviewZoomed));

	/// <summary>
	/// GTK <c>GroupSelector.UpdateLimits</c> → <c>TimeAdaptor.SetLimits</c>: the min and max
	/// handles bound the photos to the months between them. The whole strip clears the bound.
	/// </summary>
	[RelayCommand]
	void SetTimelineLimits ((int Min, int Max) limits)
	{
		if (TimelineBuckets.Count == 0 || TimelineLimits.IsFull (limits.Min, limits.Max, TimelineBuckets.Count)) {
			ClearTimelineLimits ();
		} else {
			var span = TimelineMonths.InclusiveRange (
				TimelineBuckets.Select (bucket => new TimelineMonth (bucket.Start, bucket.End, bucket.Count)).ToList (),
				limits.Min, limits.Max);
			timelineLimitStart = span.Start;
			timelineLimitEnd = span.End;
		}
		ApplyLocalFilters ();
	}

	internal bool HasTimelineLimits => timelineLimitStart is not null;

	void ClearTimelineLimits ()
	{
		timelineLimitStart = null;
		timelineLimitEnd = null;
	}

	(DateTime? Start, DateTime? End) ApplyTimelineLimits (DateTime? start, DateTime? end)
	{
		if (timelineLimitStart is DateTime limitStart)
			start = start is DateTime current && current > limitStart ? current : limitStart;
		if (timelineLimitEnd is DateTime limitEnd)
			end = end is DateTime current && current < limitEnd ? current : limitEnd;
		return (start, end);
	}

	public IReadOnlySet<int> SelectedIndices ()
	{
		var selected = new HashSet<int> ();
		for (int i = 0; i < Photos.Count; i++) {
			if (Photos[i].Selected)
				selected.Add (i);
		}
		return selected;
	}

	/// <summary>
	/// Applies a grid selection from the keyboard or the rubber band, with the focus cell as
	/// the current photo. The current photo is set first so the list's own single-item sync
	/// cannot undo the flags written after it.
	/// </summary>
	public void ApplyGridSelection (IReadOnlySet<int> selection, int focus)
	{
		if (Photos.Count == 0)
			return;
		focus = Math.Clamp (focus, 0, Photos.Count - 1);
		var focused = Photos[focus];
		bool changed = !ReferenceEquals (SelectedPhoto, focused);
		SelectedPhoto = focused;
		for (int i = 0; i < Photos.Count; i++)
			Photos[i].Selected = selection.Contains (i);
		if (!changed) {
			UpdateStatus ();
			NotifySelectionCommands ();
		} else {
			UpdateStatus ();
		}
	}

	public void RunViewerAction (ViewerAction action, double viewWidth, double viewHeight, double anchorX, double anchorY)
	{
		switch (action) {
		case ViewerAction.Previous:
			if (PreviousCommand.CanExecute (null))
				PreviousCommand.Execute (null);
			break;
		case ViewerAction.Next:
			if (NextCommand.CanExecute (null))
				NextCommand.Execute (null);
			break;
		case ViewerAction.First:
			if (Photos.Count > 0)
				SelectPhoto (Photos[0]);
			break;
		case ViewerAction.Last:
			if (Photos.Count > 0)
				SelectPhoto (Photos[^1]);
			break;
		case ViewerAction.Random:
			// GTK: new Random ().Next (0, Query.Count - 1).
			if (Photos.Count > 0)
				SelectPhoto (Photos[Random.Shared.Next (0, Math.Max (0, Photos.Count - 1))]);
			break;
		case ViewerAction.ZoomFit:
			PreviewZoom = 1;
			PreviewPanX = 0;
			PreviewPanY = 0;
			break;
		case ViewerAction.ZoomActual:
			ZoomPreviewTo (Imaging.ViewerZoom.ForScale (1, viewWidth, viewHeight, PreviewPixelWidth, PreviewPixelHeight, Info.PixelWidth, UiScale),
				viewWidth, viewHeight, anchorX, anchorY);
			break;
		case ViewerAction.ZoomDouble:
			ZoomPreviewTo (Imaging.ViewerZoom.ForScale (2, viewWidth, viewHeight, PreviewPixelWidth, PreviewPixelHeight, Info.PixelWidth, UiScale),
				viewWidth, viewHeight, anchorX, anchorY);
			break;
		case ViewerAction.ZoomIn:
			ZoomPreviewTo (Imaging.ViewerZoom.Clamp (PreviewZoom * Imaging.ViewerZoom.Step), viewWidth, viewHeight, viewWidth / 2, viewHeight / 2);
			break;
		case ViewerAction.ZoomOut:
			ZoomPreviewTo (Imaging.ViewerZoom.Clamp (PreviewZoom / Imaging.ViewerZoom.Step), viewWidth, viewHeight, viewWidth / 2, viewHeight / 2);
			break;
		case ViewerAction.PanUp:
			PanPreviewBy (0, Imaging.ViewerZoom.PanStep);
			break;
		case ViewerAction.PanDown:
			PanPreviewBy (0, -Imaging.ViewerZoom.PanStep);
			break;
		case ViewerAction.PanLeft:
			PanPreviewBy (Imaging.ViewerZoom.PanStep, 0);
			break;
		case ViewerAction.PanRight:
			PanPreviewBy (-Imaging.ViewerZoom.PanStep, 0);
			break;
		}
	}

	void ZoomPreviewTo (double zoom, double viewWidth, double viewHeight, double anchorX, double anchorY)
	{
		var pan = Imaging.ViewerZoom.AnchorPan (PreviewZoom, zoom, PreviewPanX, PreviewPanY, anchorX, anchorY, viewWidth, viewHeight);
		PreviewZoom = zoom;
		if (Imaging.ViewerZoom.IsFit (zoom)) {
			PreviewPanX = 0;
			PreviewPanY = 0;
		} else {
			PreviewPanX = pan.X;
			PreviewPanY = pan.Y;
		}
	}

	/// <summary>
	/// GTK <c>Loupe.MoveWindow</c>: the loupe stays where it was put and samples the pixel under its hotspot.
	/// </summary>
	public void DragLoupe (double viewX, double viewY, double viewWidth, double viewHeight)
	{
		MoveLoupe (viewX, viewY, viewWidth, viewHeight);
		loupePlaced = true;
	}

	/// <summary>
	/// The first time the loupe shows it lands under the pointer; after a new photo it samples again where it was left.
	/// </summary>
	public void EnsureLoupeSample (double pointerX, double pointerY, double viewWidth, double viewHeight)
	{
		if (!IsLoupeVisible || Preview is null || LoupeSample is not null)
			return;
		if (loupePlaced)
			MoveLoupe (LoupeX, LoupeY, viewWidth, viewHeight);
		else
			DragLoupe (pointerX, pointerY, viewWidth, viewHeight);
	}

	public void TurnLoupe (double angle)
		=> LoupeAngle = angle;

	[RelayCommand]
	void HideLoupe ()
		=> IsLoupeVisible = false;

	/// <summary>
	/// GTK <c>PreviewPopup</c>: while v is held the photo under the pointer shows at a third of
	/// the average screen edge; Shift+V adds the histogram.
	/// </summary>
	public Task ShowPreviewPopupAsync (PhotoItemViewModel? item, bool histogram, int screenWidth, int screenHeight)
	{
		if (item is null || !IsBrowsing) {
			HidePreviewPopup ();
			return Task.CompletedTask;
		}
		if (ShowPreviewPopup && ReferenceEquals (item, PreviewPopupItem) && histogram == PreviewPopupShowsHistogram)
			return Task.CompletedTask;
		int generation = ++previewPopupGeneration;
		int edge = screenWidth > 0 && screenHeight > 0 ? HoverPreview.Edge (screenWidth, screenHeight) : 480;
		PreviewPopupItem = item;
		PreviewPopupShowsHistogram = histogram;
		HideHover ();
		return LoadPreviewPopupAsync (item, histogram, edge, generation);
	}

	async Task LoadPreviewPopupAsync (PhotoItemViewModel item, bool histogram, int edge, int generation)
	{
		Bitmap? bitmap = null;
		Bitmap? chart = null;
		var source = RenewDecode (ref popupDecode);
		var token = source.Token;
		try {
			bitmap = await thumbnails.LoadAsync (item.Path, edge, token);
			if (generation != previewPopupGeneration || !IsBrowsing) {
				bitmap?.Dispose ();
				return;
			}
			if (histogram)
				chart = HistogramRenderer.FromBitmap (bitmap, UiScale,
					HistogramRenderer.ChartWidth / 2, HistogramRenderer.ChartHeight / 2,
					backgroundRed: 127, backgroundGreen: 127, backgroundBlue: 127, backgroundAlpha: 255);
			PreviewPopupEdge = edge;
			PreviewPopupDetail = HoverPreview.Caption (item.Photo.Description, item.Photo.Time, item.Photo.Name);
			SetPreviewPopupBitmaps (bitmap, chart);
			bitmap = null;
			chart = null;
			ShowPreviewPopup = true;
		} catch (OperationCanceledException) {
			bitmap?.Dispose ();
			chart?.Dispose ();
		} catch (Exception ex) {
			bitmap?.Dispose ();
			chart?.Dispose ();
			Logger.Log.Debug (ex, "Failed to load the preview popup for {Path}", item.Path);
		} finally {
			CancelSlot.Finish (ref popupDecode, source);
		}
	}

	public void HidePreviewPopup ()
	{
		previewPopupGeneration++;
		CancelDecode (ref popupDecode);
		PreviewPopupItem = null;
		ShowPreviewPopup = false;
		PreviewPopupDetail = string.Empty;
		SetPreviewPopupBitmaps (null, null);
	}

	void SetPreviewPopupBitmaps (Bitmap? image, Bitmap? chart)
	{
		var previousImage = PreviewPopupImage;
		var previousChart = PreviewPopupHistogram;
		PreviewPopupImage = image;
		PreviewPopupHistogram = chart;
		ReleasedBitmap.DisposePrevious (previousImage, image);
		ReleasedBitmap.DisposePrevious (previousChart, chart);
	}

	/// <summary>GTK <c>RatingEntry</c>: arrows, +, = and - and the wheel step the rating.</summary>
	public void StepRating (int delta)
	{
		if (SelectedPhoto is null || delta == 0)
			return;
		Info.RatingValue = RatingStep.Apply (Info.RatingValue, delta);
	}

	/// <summary>GTK <c>HandleTagSelectionKeyPress</c>: Space and Return add the highlighted tags to the query.</summary>
	public void IncludeTagsInQuery (IEnumerable<TagNode> nodes)
	{
		foreach (var node in nodes.ToList ())
			ActivateTagCommand.Execute (node);
	}

	/// <summary>
	/// GTK <c>TagSelectionWidget.HandleTagNameEdited</c>: renaming a tag to another tag's name
	/// (ignoring case) warns and keeps the old name.
	/// </summary>
	bool WarnIfTagNameTaken (Tag tag, string newName)
	{
		bool taken = Models.TagNames.IsTakenByAnother (tag.Name, newName,
			name => database?.Tags.GetTagByName (name) is not null
				|| (database is null && FlattenTags (Tags).Any (node => string.Equals (node.Tag.Name, name, StringComparison.OrdinalIgnoreCase))));
		if (taken)
			UiTasks.Forget (shell.ShowWarningAsync (Strings.ErrorRenamingTag, Strings.ThisNameIsAlreadyInUse));
		return taken;
	}

	/// <summary>GTK <c>TagStore.GetTagsByNameStart</c>, most popular first.</summary>
	public IReadOnlyList<string>? TagNamesStartingWith (string prefix)
	{
		if (string.IsNullOrEmpty (prefix))
			return null;
		if (database is not null)
			return database.Tags.GetTagsByNameStart (prefix)?.Select (tag => tag.Name).ToList ();
		var names = FlattenTags (Tags)
			.Select (node => node.Tag.Name)
			.Where (name => name.StartsWith (prefix, StringComparison.OrdinalIgnoreCase))
			.ToList ();
		return names.Count == 0 ? null : names;
	}
}
