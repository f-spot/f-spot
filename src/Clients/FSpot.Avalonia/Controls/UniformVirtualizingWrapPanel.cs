//
// UniformVirtualizingWrapPanel.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Specialized;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

using FSpot.Avalonia.Imaging;

namespace FSpot.Avalonia.Controls;

/// <summary>
/// Uniform photo grid that realizes only the viewport plus one overscan row.
/// Avalonia ships a virtualizing stack, not a virtualizing wrap.
/// </summary>
public class UniformVirtualizingWrapPanel : VirtualizingPanel, ILogicalScrollable
{
	const int OverscanRows = 1;

	public static readonly StyledProperty<double> ItemWidthProperty =
		AvaloniaProperty.Register<UniformVirtualizingWrapPanel, double> (nameof (ItemWidth), 136);

	public static readonly StyledProperty<double> ItemHeightProperty =
		AvaloniaProperty.Register<UniformVirtualizingWrapPanel, double> (nameof (ItemHeight), 224);

	readonly Dictionary<int, Control> realized = new ();

	Vector offset;
	Size extent;
	Size viewport = new (1, 1);
	int anchorColumnCount = 1;
	bool scrolling;

	static UniformVirtualizingWrapPanel ()
	{
		AffectsMeasure<UniformVirtualizingWrapPanel> (ItemWidthProperty, ItemHeightProperty);
	}

	public UniformVirtualizingWrapPanel ()
	{
		ClipToBounds = true;
	}

	public double ItemWidth {
		get => GetValue (ItemWidthProperty);
		set => SetValue (ItemWidthProperty, value);
	}

	public double ItemHeight {
		get => GetValue (ItemHeightProperty);
		set => SetValue (ItemHeightProperty, value);
	}

	internal int RealizedCount => realized.Count;

	public bool CanHorizontallyScroll { get; set; }

	public bool CanVerticallyScroll { get; set; } = true;

	public bool IsLogicalScrollEnabled => true;

	public Size ScrollSize => new (CellWidth, CellHeight);

	public Size PageScrollSize => viewport;

	public Size Extent => extent;

	public Size Viewport => viewport;

	public Vector Offset {
		get => offset;
		set {
			var next = Clamp (value);
			if (next == offset)
				return;
			offset = next;
			InvalidateMeasure ();
		}
	}

	public event EventHandler? ScrollInvalidated;

	public void RaiseScrollInvalidated (EventArgs e)
		=> ScrollInvalidated?.Invoke (this, e);

	public bool BringIntoView (Control target, Rect targetRect)
	{
		int index = IndexOf (target);
		if (index < 0)
			return false;
		var before = offset;
		ScrollIntoView (index);
		return offset != before;
	}

	public Control? GetControlInDirection (NavigationDirection direction, Control? from)
		=> GetControl (direction, from, wrap: false);

	protected override Size MeasureOverride (Size availableSize)
	{
		double width = double.IsFinite (availableSize.Width) && availableSize.Width > 0 ? availableSize.Width : CellWidth;
		double height = double.IsFinite (availableSize.Height) && availableSize.Height > 0 ? availableSize.Height : CellHeight;
		viewport = new Size (width, height);
		Realize ();
		var next = Clamp (offset);
		bool moved = next != offset;
		offset = next;
		if (!scrolling && (moved || extent.Height != ExtentHeight (width) || extent.Width != width)) {
			extent = new Size (width, ExtentHeight (width));
			scrolling = true;
			try {
				RaiseScrollInvalidated (EventArgs.Empty);
			} finally {
				scrolling = false;
			}
		} else {
			extent = new Size (width, ExtentHeight (width));
		}

		return viewport;
	}

	protected override Size ArrangeOverride (Size finalSize)
	{
		int columns = anchorColumnCount;
		double cellW = CellWidth;
		double cellH = CellHeight;
		foreach (var (index, child) in realized) {
			int row = index / columns;
			int column = index % columns;
			child.Arrange (new Rect (column * cellW, row * cellH - offset.Y, cellW, cellH));
		}

		return finalSize;
	}

	protected override Control? ScrollIntoView (int index)
	{
		if (Items is not IList items || (uint)index >= (uint)items.Count)
			return null;
		int columns = Math.Max (1, anchorColumnCount);
		double cellH = CellHeight;
		double rowTop = index / columns * cellH;
		double view = viewport.Height > 0 ? viewport.Height : cellH;
		if (rowTop < offset.Y)
			offset = new Vector (0, rowTop);
		else if (rowTop + cellH > offset.Y + view)
			offset = new Vector (0, Math.Max (0, rowTop + cellH - view));
		offset = Clamp (offset);
		Realize ();
		RaiseScrollInvalidated (EventArgs.Empty);
		realized.TryGetValue (index, out var container);
		return container;
	}

	protected override Control? ContainerFromIndex (int index)
		=> realized.TryGetValue (index, out var container) ? container : null;

	protected override int IndexFromContainer (Control container)
	{
		foreach (var (index, realizedContainer) in realized) {
			if (ReferenceEquals (realizedContainer, container))
				return index;
		}

		return -1;
	}

	protected override IEnumerable<Control> GetRealizedContainers ()
		=> realized.Values;

	protected override Control? GetControl (NavigationDirection direction, IInputElement? from, bool wrap)
	{
		if (Items is not IList items || items.Count == 0)
			return null;
		int index = from is Control control ? IndexOf (control) : -1;
		if (index < 0 && direction is not (NavigationDirection.First or NavigationDirection.Next))
			return direction == NavigationDirection.Last ? ScrollIntoView (items.Count - 1) : ScrollIntoView (0);
		int columns = Math.Max (1, anchorColumnCount);
		int page = Math.Max (1, (int)Math.Floor (viewport.Height / CellHeight)) * columns;
		int target = direction switch {
			NavigationDirection.Left or NavigationDirection.Previous => index - 1,
			NavigationDirection.Right or NavigationDirection.Next => index + 1,
			NavigationDirection.Up => index - columns,
			NavigationDirection.Down => index + columns,
			NavigationDirection.PageUp => index - page,
			NavigationDirection.PageDown => index + page,
			NavigationDirection.First => 0,
			NavigationDirection.Last => items.Count - 1,
			_ => -1
		};
		if (target < 0 || target >= items.Count) {
			if (!wrap || items.Count == 0)
				return null;
			target = (target % items.Count + items.Count) % items.Count;
		}

		return ScrollIntoView (target);
	}

	protected override void OnItemsChanged (IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
	{
		base.OnItemsChanged (items, e);
		int last = realized.Count == 0 ? -1 : realized.Keys.Max ();
		bool appended = e.Action == NotifyCollectionChangedAction.Add
			&& e.NewStartingIndex >= 0
			&& (last < 0 || e.NewStartingIndex > last);
		if (!appended)
			UnrealizeAll ();
		InvalidateMeasure ();
	}

	void Realize ()
	{
		if (ItemContainerGenerator is not { } generator || Items is not IList items)
			return;
		double width = viewport.Width > 0 ? viewport.Width : CellWidth;
		int columns = Math.Max (1, (int)Math.Floor ((width + 0.01) / CellWidth));
		anchorColumnCount = columns;
		var slice = ThumbnailWindow.Range (items.Count, columns, offset.Y, viewport.Height, CellHeight, OverscanRows);
		int first = slice.First;
		int end = slice.First + slice.Count;
		foreach (int index in realized.Keys.Where (index => index < first || index >= end).ToArray ())
			Unrealize (index);
		for (int index = first; index < end; index++)
			RealizeOne (generator, items, index);
		extent = new Size (width, ExtentHeight (width));
	}

	void RealizeOne (ItemContainerGenerator generator, IList items, int index)
	{
		if (realized.ContainsKey (index))
			return;
		object? item = items[index];
		if (item is null || !generator.NeedsContainer (item, index, out var recycleKey))
			return;
		var container = generator.CreateContainer (item, index, recycleKey);
		generator.PrepareItemContainer (container, item, index);
		AddInternalChild (container);
		generator.ItemContainerPrepared (container, item, index);
		realized[index] = container;
	}

	void Unrealize (int index)
	{
		if (!realized.Remove (index, out var container))
			return;
		ItemContainerGenerator?.ClearItemContainer (container);
		RemoveInternalChild (container);
	}

	void UnrealizeAll ()
	{
		foreach (int index in realized.Keys.ToArray ())
			Unrealize (index);
	}

	double CellWidth => ItemWidth > 1 ? ItemWidth : 1;

	double CellHeight => ItemHeight > 1 ? ItemHeight : 1;

	double ExtentHeight (double width)
	{
		if (Items is not IList items || items.Count == 0)
			return 0;
		int columns = Math.Max (1, (int)Math.Floor ((width + 0.01) / CellWidth));
		int rows = (items.Count + columns - 1) / columns;
		return rows * CellHeight;
	}

	Vector Clamp (Vector value)
	{
		double maxY = Math.Max (0, extent.Height - viewport.Height);
		double y = double.IsFinite (value.Y) ? Math.Clamp (value.Y, 0, maxY) : 0;
		return new Vector (0, y);
	}

	int IndexOf (Control? control)
	{
		while (control is not null) {
			int index = IndexFromContainer (control);
			if (index >= 0)
				return index;
			control = control.Parent as Control;
		}

		return -1;
	}
}
