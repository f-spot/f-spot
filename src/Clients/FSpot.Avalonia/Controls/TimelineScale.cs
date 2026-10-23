//
// TimelineScale.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

using FSpot.Avalonia.ViewModels;
using FSpot.Utils;

namespace FSpot.Avalonia.Controls;

public class TimelineScale : Control
{
	public static readonly StyledProperty<IEnumerable?> ItemsProperty =
		AvaloniaProperty.Register<TimelineScale, IEnumerable?> (nameof (Items));

	public static readonly StyledProperty<ICommand?> SelectCommandProperty =
		AvaloniaProperty.Register<TimelineScale, ICommand?> (nameof (SelectCommand));

	public static readonly StyledProperty<ICommand?> RangeCommandProperty =
		AvaloniaProperty.Register<TimelineScale, ICommand?> (nameof (RangeCommand));

	public static readonly StyledProperty<ICommand?> LimitsCommandProperty =
		AvaloniaProperty.Register<TimelineScale, ICommand?> (nameof (LimitsCommand));

	readonly List<TimelineBucket> buckets = [];
	ScrollViewer? scroller;
	INotifyCollectionChanged? itemsChanged;
	double columnWidth = TimelineChrome.MinHitPx;
	double arrangedViewport;
	int? pressIndex;
	int hoverIndex = -1;
	int announcedIndex = -1;
	int? followIndex;
	int? previewFrom;
	int? previewTo;
	bool glassDrag;
	bool shiftRange;
	bool finishingGesture;
	Point pressPoint;
	int minLimit;
	int maxLimit = -1;
	int rangeCount;
	TimelineLimit dragLimit;
	double dragLimitX;

	public TimelineScale ()
		=> ClipToBounds = true;

	public IEnumerable? Items {
		get => GetValue (ItemsProperty);
		set => SetValue (ItemsProperty, value);
	}

	public ICommand? SelectCommand {
		get => GetValue (SelectCommandProperty);
		set => SetValue (SelectCommandProperty, value);
	}

	public ICommand? RangeCommand {
		get => GetValue (RangeCommandProperty);
		set => SetValue (RangeCommandProperty, value);
	}

	public ICommand? LimitsCommand {
		get => GetValue (LimitsCommandProperty);
		set => SetValue (LimitsCommandProperty, value);
	}

	public double ColumnWidth => columnWidth;

	public string? HoverCaption { get; private set; }

	public (int Min, int Max) Limits => (minLimit, maxLimit);

	static TimelineScale ()
	{
		AffectsMeasure<TimelineScale> (ItemsProperty);
		AffectsRender<TimelineScale> (ItemsProperty);
		ItemsProperty.Changed.AddClassHandler<TimelineScale> ((scale, _) => scale.WatchItems ());
	}

	public int IndexAt (Point position)
		=> TimelineLayout.IndexAt (position.X, columnWidth, buckets.Count);

	public bool Step (int direction)
	{
		if (buckets.Count == 0 || direction == 0)
			return false;
		int current = GlassSpan ()?.Start ?? -1;
		int index = current;
		do {
			index += direction;
			if (index < 0 || index >= buckets.Count)
				return false;
		} while (!buckets[index].HasPhotos);
		Select (buckets[index], scroll: true);
		return true;
	}

	protected override void OnAttachedToVisualTree (VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree (e);
		scroller = this.FindAncestorOfType<ScrollViewer> ();
		if (scroller is not null)
			scroller.PropertyChanged += OnScrollerPropertyChanged;
		ReloadBuckets ();
	}

	protected override void OnDetachedFromVisualTree (VisualTreeAttachmentEventArgs e)
	{
		if (scroller is not null)
			scroller.PropertyChanged -= OnScrollerPropertyChanged;
		scroller = null;
		UnhookBuckets ();
		base.OnDetachedFromVisualTree (e);
	}

	void OnScrollerPropertyChanged (object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property == ScrollViewer.ViewportProperty || e.Property == BoundsProperty)
			InvalidateMeasure ();
	}

	protected override Size MeasureOverride (Size availableSize)
	{
		double viewport = ViewportWidth (availableSize.Width);
		var layout = TimelineLayout.Measure (buckets.Count, viewport, TimelineChrome.MinHitPx);
		columnWidth = layout.ColumnWidth;
		double height = availableSize.Height;
		if (!double.IsFinite (height) || height < 1)
			height = TimelineLayout.StripHeight;
		return new Size (Math.Max (layout.ContentWidth, viewport), height);
	}

	protected override Size ArrangeOverride (Size finalSize)
	{
		double viewport = scroller?.Bounds.Width ?? 0;
		if (viewport > 1 && Math.Abs (viewport - arrangedViewport) > 0.5) {
			arrangedViewport = viewport;
			InvalidateMeasure ();
		}
		return finalSize;
	}

	double ViewportWidth (double availableWidth)
	{
		if (arrangedViewport > 1)
			return arrangedViewport;
		double viewport = scroller?.Bounds.Width ?? 0;
		if (viewport < 1)
			viewport = scroller?.Viewport.Width ?? 0;
		if (viewport < 1 && double.IsFinite (availableWidth))
			viewport = availableWidth;
		return double.IsFinite (viewport) && viewport > 0 ? viewport : 0;
	}

	public override void Render (DrawingContext context)
	{
		if (buckets.Count == 0)
			return;
		var chrome = TimelineLayout.Split (Bounds.Height > 1 ? Bounds.Height : TimelineLayout.StripHeight);
		var accent = Brush ("SystemAccentColor", Brushes.DodgerBlue);
		var muted = Brush ("SystemControlForegroundBaseMediumBrush", Brushes.Gray);
		var label = new SolidColorBrush (ThemeInk.Foreground (this));
		var plot = Brush ("SystemControlBackgroundChromeMediumLowBrush", Brushes.Transparent);
		double baseline = chrome.Legend + chrome.Bars;
		double plotWidth = Math.Max (Bounds.Width, buckets.Count * columnWidth);
		var glass = GlassSpan ();

		context.FillRectangle (plot, new Rect (0, 0, plotWidth, chrome.Legend + chrome.Bars + chrome.Handle), 0);
		for (int i = 0; i < buckets.Count; i++) {
			var bucket = buckets[i];
			double bar = bucket.Count <= 0 ? 0 : Math.Max (2, bucket.RelativeHeight * (chrome.Bars - 3));
			if (bar <= 0)
				continue;
			bool inside = glass is (int start, int end) && i >= start && i <= end;
			bool limited = i < minLimit || i > maxLimit;
			var (x, width) = Column (i, 1);
			context.FillRectangle ((inside || glass is null) && !limited ? accent : muted, new Rect (x, baseline - bar, width, bar), 0);
		}

		if (glass is (int glassStart, int glassEnd)) {
			var (x, width) = TimelineLayout.Span (glassStart, glassEnd, columnWidth);
			x = Math.Floor (x);
			width = Math.Max (1, Math.Ceiling (width));
			var glassRect = new Rect (x, 0, width, baseline + chrome.Handle);
			context.DrawRectangle (new Pen (accent, 1), glassRect, 0);
			var handle = new Rect (x, baseline, width, chrome.Handle);
			context.FillRectangle (accent, handle, 0);
			double grip = Math.Min (10, Math.Max (0, width - 8));
			if (grip >= 6) {
				double mid = handle.Y + handle.Height / 2;
				double gx = x + (width - grip) / 2;
				var pen = new Pen (label, 1);
				context.DrawLine (pen, new Point (gx, mid - 2), new Point (gx + grip, mid - 2));
				context.DrawLine (pen, new Point (gx, mid + 2), new Point (gx + grip, mid + 2));
			}
		}

		DrawLimit (context, TimelineLimit.Min, baseline + chrome.Handle, label, plot);
		DrawLimit (context, TimelineLimit.Max, baseline + chrome.Handle, label, plot);

		for (int i = 0; i < buckets.Count; i++) {
			bool hovering = i == hoverIndex && !string.IsNullOrEmpty (HoverCaption);
			double tick = hovering || !string.IsNullOrEmpty (buckets[i].YearLabel) ? 5 : 3;
			double tickX = Math.Floor (i * columnWidth);
			context.FillRectangle (muted, new Rect (tickX, 1, 1, tick), 0);
			string? caption = hovering ? HoverCaption : buckets[i].YearLabel;
			if (string.IsNullOrEmpty (caption))
				continue;
			var text = new FormattedText (caption, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
				new Typeface (FontFamily.Default), 9, label);
			double x = tickX + 3;
			// Keep the hovered "Month Year" fully inside the strip. A column at the
			// right edge would otherwise clip the year off the label.
			if (hovering && x + text.Width > Bounds.Width - 4)
				x = Math.Max (4, Bounds.Width - text.Width - 4);
			context.DrawText (text, new Point (x, 0));
		}
	}

	// GTK Limit.Draw: a small box at the top and bottom of the strip on the limit's edge.
	void DrawLimit (DrawingContext context, TimelineLimit limit, double height, IBrush stroke, IBrush fill)
	{
		double x = dragLimit == limit
			? dragLimitX
			: TimelineLimits.EdgeX (limit, limit == TimelineLimit.Min ? minLimit : maxLimit, columnWidth);
		double half = TimelineLimits.HandleWidth / 2;
		double box = Math.Min (TimelineLimits.HandleHeight, height / 3);
		var pen = new Pen (stroke, 1);
		x = Math.Floor (x) + 0.5;
		context.DrawLine (pen, new Point (x, box), new Point (x, height - box));
		context.DrawRectangle (fill, pen, new Rect (x - half, 0.5, TimelineLimits.HandleWidth, box - 1), 1, 1);
		context.DrawRectangle (fill, pen, new Rect (x - half, height - box + 0.5, TimelineLimits.HandleWidth, box - 1), 1, 1);
	}

	(double X, double Width) Column (int index, double inset)
	{
		double x = Math.Floor (index * columnWidth) + inset;
		double next = Math.Floor ((index + 1) * columnWidth) - inset;
		return (x, Math.Max (1, next - x));
	}

	protected override void OnPointerPressed (PointerPressedEventArgs e)
	{
		base.OnPointerPressed (e);
		var position = e.GetPosition (this);
		var limit = buckets.Count == 0 ? TimelineLimit.None : TimelineLimits.HitTest (position.X, minLimit, maxLimit, columnWidth);
		if (limit != TimelineLimit.None && e.GetCurrentPoint (this).Properties.IsLeftButtonPressed) {
			dragLimit = limit;
			dragLimitX = position.X;
			e.Pointer.Capture (this);
			InvalidateVisual ();
			e.Handled = true;
			return;
		}
		int index = IndexAt (position);
		if (index < 0)
			return;
		pressPoint = position;
		shiftRange = e.KeyModifiers.HasFlag (KeyModifiers.Shift);
		var glass = GlassSpan ();
		glassDrag = !shiftRange && glass is (int start, int end) && index >= start && index <= end;
		if (shiftRange || glassDrag) {
			pressIndex = index;
			announcedIndex = index;
			e.Pointer.Capture (this);
		} else
			Activate (index);
		e.Handled = true;
	}

	protected override void OnPointerMoved (PointerEventArgs e)
	{
		base.OnPointerMoved (e);
		var position = e.GetPosition (this);
		if (dragLimit != TimelineLimit.None) {
			dragLimitX = Math.Clamp (position.X, 0, buckets.Count * columnWidth);
			InvalidateVisual ();
			return;
		}
		bool overLimit = buckets.Count > 0
			&& TimelineLimits.HitTest (position.X, minLimit, maxLimit, columnWidth) != TimelineLimit.None;
		Cursor = overLimit ? new Cursor (StandardCursorType.SizeWestEast) : null;
		int index = IndexAt (position);
		if (index >= 0 && index != hoverIndex)
			ShowHover (index);
		if (pressIndex is null || index < 0 || !MovedFarEnough (e))
			return;
		if (shiftRange) {
			previewFrom = pressIndex;
			previewTo = index;
			InvalidateVisual ();
			return;
		}
		if (!glassDrag || index == followIndex)
			return;
		followIndex = index;
		InvalidateVisual ();
	}

	protected override void OnPointerExited (PointerEventArgs e)
	{
		base.OnPointerExited (e);
		if (hoverIndex < 0)
			return;
		hoverIndex = -1;
		HoverCaption = null;
		InvalidateVisual ();
	}

	void ShowHover (int index)
	{
		hoverIndex = index;
		HoverCaption = buckets[index].ToolTip;
		ToolTip.SetTip (this, HoverCaption);
		InvalidateVisual ();
	}

	protected override void OnPointerReleased (PointerReleasedEventArgs e)
	{
		base.OnPointerReleased (e);
		if (dragLimit != TimelineLimit.None) {
			DropLimit (e.GetPosition (this).X);
			e.Pointer.Capture (null);
			e.Handled = true;
			return;
		}
		if (pressIndex is not int start)
			return;
		int end = IndexAt (e.GetPosition (this));
		if (end < 0)
			end = start;
		bool moved = MovedFarEnough (e);
		finishingGesture = true;
		if (shiftRange)
			RangeCommand?.Execute ((Math.Min (start, end), Math.Max (start, end)));
		else if (glassDrag && moved)
			SnapDrag (end);
		else if (glassDrag)
			Activate (end);
		e.Pointer.Capture (null);
		EndGesture ();
		finishingGesture = false;
		e.Handled = true;
	}

	protected override void OnPointerCaptureLost (PointerCaptureLostEventArgs e)
	{
		base.OnPointerCaptureLost (e);
		if (!finishingGesture)
			EndGesture ();
	}

	void DropLimit (double x)
	{
		var limit = dragLimit;
		dragLimit = TimelineLimit.None;
		var (min, max) = TimelineLimits.Drop (limit, x, minLimit, maxLimit, columnWidth, buckets.Count);
		InvalidateVisual ();
		ApplyRange (min, max);
	}

	void SnapDrag (int index)
	{
		int snapped = TimelineLayout.NearestFilled (Filled (), index);
		if (snapped < 0 || snapped == announcedIndex)
			return;
		announcedIndex = snapped;
		Select (buckets[snapped], scroll: false);
	}

	void Activate (int index)
	{
		if ((uint)index >= (uint)buckets.Count)
			return;
		int target = buckets[index].HasPhotos ? index : TimelineLayout.NearestFilled (Filled (), index);
		if (target < 0)
			return;
		if (!buckets[index].HasPhotos && buckets[target].Selected)
			return;
		Select (buckets[target], scroll: true);
	}

	void Select (TimelineBucket bucket, bool scroll)
	{
		if (SelectCommand?.CanExecute (bucket) == true)
			SelectCommand.Execute (bucket);
		if (scroll)
			ScrollColumnIntoView (buckets.IndexOf (bucket));
	}

	void ScrollColumnIntoView (int index)
	{
		if (scroller is null || index < 0 || columnWidth <= 0)
			return;
		double view = scroller.Viewport.Width;
		if (view < 1)
			return;
		double x = index * columnWidth;
		double right = x + columnWidth;
		double left = scroller.Offset.X;
		if (x < left)
			scroller.Offset = new global::Avalonia.Vector (x, 0);
		else if (right > left + view)
			scroller.Offset = new global::Avalonia.Vector (Math.Max (0, right - view), 0);
	}

	bool MovedFarEnough (PointerEventArgs e)
		=> Input.DragGesture.MovedFarEnough (pressPoint, e.GetPosition (this));

	void EndGesture ()
	{
		pressIndex = null;
		glassDrag = false;
		shiftRange = false;
		followIndex = null;
		previewFrom = null;
		previewTo = null;
		dragLimit = TimelineLimit.None;
		InvalidateVisual ();
	}

	void WatchItems ()
	{
		if (itemsChanged is not null)
			itemsChanged.CollectionChanged -= OnItemsChanged;
		itemsChanged = Items as INotifyCollectionChanged;
		if (itemsChanged is not null)
			itemsChanged.CollectionChanged += OnItemsChanged;
		ReloadBuckets ();
	}

	void OnItemsChanged (object? sender, NotifyCollectionChangedEventArgs e)
		=> ReloadBuckets ();

	internal void ApplyRange (int min, int max)
	{
		if (min == minLimit && max == maxLimit)
			return;
		minLimit = min;
		maxLimit = max;
		if (LimitsCommand?.CanExecute ((min, max)) == true)
			LimitsCommand.Execute ((min, max));
	}

	void ReloadBuckets ()
	{
		// Detach clears the bucket list. The count from the last load is what
		// "the month count changed" has to be compared with, or a reattach
		// looks like a new strip and snaps the range back to every month.
		int previous = buckets.Count > 0 ? buckets.Count : rangeCount;
		UnhookBuckets ();
		if (Items is not null)
			buckets.AddRange (Items.OfType<TimelineBucket> ());
		// GTK GroupSelector.Adaptor: a new month count resets the limits to the whole strip.
		if (buckets.Count != previous || maxLimit >= buckets.Count || maxLimit < 0) {
			minLimit = 0;
			maxLimit = buckets.Count - 1;
		}
		if (buckets.Count > 0)
			rangeCount = buckets.Count;
		foreach (var bucket in buckets)
			bucket.PropertyChanged += OnBucketChanged;
		InvalidateMeasure ();
		InvalidateVisual ();
	}

	void UnhookBuckets ()
	{
		foreach (var bucket in buckets)
			bucket.PropertyChanged -= OnBucketChanged;
		buckets.Clear ();
	}

	void OnBucketChanged (object? sender, PropertyChangedEventArgs e)
		=> InvalidateVisual ();

	List<bool> Filled ()
	{
		var filled = new List<bool> (buckets.Count);
		foreach (var bucket in buckets)
			filled.Add (bucket.HasPhotos);
		return filled;
	}

	(int Start, int End)? GlassSpan ()
	{
		if (previewFrom is int from && previewTo is int to)
			return (Math.Min (from, to), Math.Max (from, to));
		if (followIndex is int follow)
			return (follow, follow);
		int start = -1;
		int end = -1;
		for (int i = 0; i < buckets.Count; i++) {
			if (!buckets[i].Selected && !buckets[i].InRange)
				continue;
			if (start < 0)
				start = i;
			end = i;
		}
		return start < 0 ? null : (start, end);
	}

	IBrush Brush (string key, IBrush fallback)
	{
		if (!TryGetResource (key, ActualThemeVariant, out var value) || value is null)
			return fallback;
		return value switch {
			IBrush brush => brush,
			Color color => new SolidColorBrush (color),
			_ => fallback
		};
	}
}
