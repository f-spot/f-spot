//
// GridInput.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

using FSpot.Avalonia.Controls;
using FSpot.Avalonia.ViewModels;

namespace FSpot.Avalonia.Input;

/// <summary>
/// GTK <c>SelectionCollectionGridView</c> input for the photo grid: the keyboard
/// (<see cref="GridNavigation"/>), rubber-band selection from empty space, and the
/// <c>PreviewPopup</c> shown while v or V is held.
/// </summary>
public static class GridInput
{
	public static readonly AttachedProperty<bool> EnableProperty =
		AvaloniaProperty.RegisterAttached<ListBox, bool> ("Enable", typeof (GridInput));

	static readonly AttachedProperty<GridState?> StateProperty =
		AvaloniaProperty.RegisterAttached<ListBox, GridState?> ("State", typeof (GridInput));

	static GridInput ()
		=> EnableProperty.Changed.AddClassHandler<ListBox> ((list, args) => {
			if (!args.GetNewValue<bool> () || list.GetValue (StateProperty) is not null)
				return;
			list.SetValue (StateProperty, new GridState ());
			list.AddHandler (InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
			list.AddHandler (InputElement.KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);
			list.AddHandler (InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
			list.AddHandler (InputElement.PointerMovedEvent, OnMoved, RoutingStrategies.Bubble, handledEventsToo: true);
			list.AddHandler (InputElement.PointerReleasedEvent, OnReleased, RoutingStrategies.Bubble, handledEventsToo: true);
			list.AddHandler (InputElement.PointerCaptureLostEvent, OnCaptureLost);
			list.AddHandler (InputElement.PointerExitedEvent, OnExited);
		});

	public static void SetEnable (ListBox element, bool value) => element.SetValue (EnableProperty, value);
	public static bool GetEnable (ListBox element) => element.GetValue (EnableProperty);

	sealed class GridState
	{
		public PhotoItemViewModel? Hovered;
		public Point? Press;
		public KeyModifiers PressModifiers;
		public bool Banding;
		public IReadOnlySet<int> Initial = new HashSet<int> ();
		public bool InitialAll;
		public Point Pointer;
		public RubberBandAdorner? Adorner;
		public DispatcherTimer? ScrollTimer;
		public double ScrollDelta;
	}

	static void OnKeyDown (object? sender, KeyEventArgs e)
	{
		if (sender is not ListBox list || WindowCommand.Model (list) is not MainWindowViewModel viewModel)
			return;
		var focused = TopLevel.GetTopLevel (list)?.FocusManager?.GetFocusedElement ();
		if (!ShortcutGuards.AllowsUnadornedKey (focused, e.Key))
			return;

		if (e.Key == Key.V && (e.KeyModifiers & ~KeyModifiers.Shift) == KeyModifiers.None) {
			ShowPopup (list, viewModel, (e.KeyModifiers & KeyModifiers.Shift) != 0);
			e.Handled = true;
			return;
		}

		if (!GridNavigation.Handles (e.Key) || viewModel.Photos.Count == 0)
			return;
		var panel = list.ItemsPanelRoot as UniformVirtualizingWrapPanel;
		int focus = FocusIndex (list, viewModel, focused);
		GridKeyResult result = viewModel.TryGetIndexSelection (out bool allSelected, out IReadOnlySet<int> picked)
			? GridNavigation.Apply (e.Key, e.KeyModifiers, focus,
				panel?.Columns ?? 1, panel?.VisibleRows ?? 1, viewModel.Photos.Count, allSelected, picked)
			: GridNavigation.Apply (e.Key, e.KeyModifiers, focus,
				panel?.Columns ?? 1, panel?.VisibleRows ?? 1, viewModel.Photos.Count, viewModel.SelectedIndices ());
		if (result.Action == GridKeyAction.None)
			return;

		viewModel.ApplyNavigation (result);
		FocusCell (list, result.Focus);
		if (result.Action == GridKeyAction.Open)
			viewModel.OpenPhotoCommand.Execute (viewModel.Photos[result.Focus]);
		e.Handled = true;
	}

	static void OnKeyUp (object? sender, KeyEventArgs e)
	{
		// GTK PreviewPopup.HandleKeyRelease hides on v, V and h.
		if (e.Key is not (Key.V or Key.H) || sender is not ListBox list)
			return;
		if (WindowCommand.Model (list) is MainWindowViewModel viewModel && viewModel.ShowPreviewPopup) {
			viewModel.HidePreviewPopup ();
			e.Handled = true;
		}
	}

	static int FocusIndex (ListBox list, MainWindowViewModel viewModel, IInputElement? focused)
	{
		for (var visual = focused as Visual; visual is not null && !ReferenceEquals (visual, list); visual = visual.GetVisualParent ()) {
			if (visual is ListBoxItem { DataContext: PhotoItemViewModel item }) {
				int index = viewModel.Photos.IndexOf (item);
				if (index >= 0)
					return index;
			}
		}
		return viewModel.SelectedPhoto is null ? 0 : Math.Max (0, viewModel.Photos.IndexOf (viewModel.SelectedPhoto));
	}

	static void FocusCell (ListBox list, int index)
	{
		list.ScrollIntoView (index);
		Dispatcher.UIThread.Post (() => list.ContainerFromIndex (index)?.Focus (NavigationMethod.Directional));
	}

	static void ShowPopup (ListBox list, MainWindowViewModel viewModel, bool histogram)
	{
		var state = list.GetValue (StateProperty);
		var item = state?.Hovered ?? viewModel.SelectedPhoto;
		if (item is null) {
			viewModel.HidePreviewPopup ();
			return;
		}
		PlacePopup (list, item);
		var (width, height) = ScreenSize (list);
		UiTasks.Forget (viewModel.ShowPreviewPopupAsync (item, histogram, width, height));
	}

	static void PlacePopup (ListBox list, PhotoItemViewModel item)
	{
		if (TopLevel.GetTopLevel (list) is not Window window || window.FindControl<Popup> ("PreviewPopup") is not Popup popup)
			return;
		int index = list.Items.IndexOf (item);
		popup.PlacementTarget = index >= 0 && list.ContainerFromIndex (index) is Control cell ? cell : list;
	}

	static (int Width, int Height) ScreenSize (Visual visual)
	{
		if (TopLevel.GetTopLevel (visual) is not Window window)
			return (0, 0);
		var screen = window.Screens?.ScreenFromVisual (window)?.Bounds;
		return (screen?.Width ?? (int)window.Bounds.Width, screen?.Height ?? (int)window.Bounds.Height);
	}

	static PhotoItemViewModel? ItemAt (object? source, ListBox list)
	{
		for (var visual = source as Visual; visual is not null && !ReferenceEquals (visual, list); visual = visual.GetVisualParent ()) {
			if (visual is ListBoxItem { DataContext: PhotoItemViewModel item })
				return item;
		}
		return null;
	}

	static bool OnScrollBar (object? source, ListBox list)
	{
		for (var visual = source as Visual; visual is not null && !ReferenceEquals (visual, list); visual = visual.GetVisualParent ()) {
			if (visual is ScrollBar)
				return true;
		}
		return false;
	}

	static void OnPressed (object? sender, PointerPressedEventArgs e)
	{
		if (sender is not ListBox list || list.GetValue (StateProperty) is not GridState state)
			return;
		if (e.Route == RoutingStrategies.Tunnel) {
			if (WindowCommand.Model (list) is MainWindowViewModel viewModel && viewModel.ShowPreviewPopup)
				viewModel.HidePreviewPopup ();
			return;
		}
		if (state.Press is not null || list.ItemsPanelRoot is not UniformVirtualizingWrapPanel panel)
			return;
		if (!e.GetCurrentPoint (list).Properties.IsLeftButtonPressed || ItemAt (e.Source, list) is not null || OnScrollBar (e.Source, list))
			return;
		state.Press = Content (panel, e.GetPosition (panel));
		state.PressModifiers = e.KeyModifiers;
		state.Pointer = e.GetPosition (panel);
		list.Focus (NavigationMethod.Pointer);
		e.Handled = true;
	}

	static void OnMoved (object? sender, PointerEventArgs e)
	{
		if (sender is not ListBox list || list.GetValue (StateProperty) is not GridState state)
			return;
		var hovered = ItemAt (e.Source, list);
		if (!ReferenceEquals (hovered, state.Hovered)) {
			state.Hovered = hovered;
			// GTK PreviewPopup.HandleIconViewMotion follows the pointer while the popup is up.
			if (WindowCommand.Model (list) is MainWindowViewModel viewModel && viewModel.ShowPreviewPopup) {
				if (hovered is null)
					viewModel.HidePreviewPopup ();
				else {
					PlacePopup (list, hovered);
					var (width, height) = ScreenSize (list);
					UiTasks.Forget (viewModel.ShowPreviewPopupAsync (hovered, viewModel.PreviewPopupShowsHistogram, width, height));
				}
			}
		}

		if (state.Press is not Point press || list.ItemsPanelRoot is not UniformVirtualizingWrapPanel panel)
			return;
		state.Pointer = e.GetPosition (panel);
		if (!state.Banding) {
			if (!DragGesture.MovedFarEnough (press, Content (panel, state.Pointer)))
				return;
			StartBand (list, panel, state, e.Pointer);
		}
		UpdateBand (list, panel, state);
		UpdateAutoScroll (list, panel, state);
	}

	static void OnReleased (object? sender, PointerReleasedEventArgs e)
	{
		if (sender is not ListBox list || list.GetValue (StateProperty) is not GridState state || state.Press is null)
			return;
		bool banding = state.Banding;
		EndBand (list, state);
		if (banding) {
			e.Pointer.Capture (null);
			e.Handled = true;
		}
	}

	static void OnCaptureLost (object? sender, PointerCaptureLostEventArgs e)
	{
		if (sender is ListBox list && list.GetValue (StateProperty) is GridState state && state.Banding)
			EndBand (list, state);
	}

	static void OnExited (object? sender, PointerEventArgs e)
	{
		if (sender is ListBox list && list.GetValue (StateProperty) is GridState state)
			state.Hovered = null;
	}

	static Point Content (UniformVirtualizingWrapPanel panel, Point viewport)
		=> new (viewport.X, viewport.Y + panel.Offset.Y);

	static void StartBand (ListBox list, UniformVirtualizingWrapPanel panel, GridState state, IPointer pointer)
	{
		if (WindowCommand.Model (list) is not MainWindowViewModel viewModel)
			return;
		state.Banding = true;
		// GTK: Ctrl toggles and Shift keeps the selection; otherwise the band starts from nothing.
		state.InitialAll = false;
		if ((state.PressModifiers & (KeyModifiers.Shift | KeyModifiers.Control | KeyModifiers.Meta)) == 0)
			state.Initial = new HashSet<int> ();
		else if (viewModel.TryGetIndexSelection (out bool allSelected, out IReadOnlySet<int> picked) && allSelected) {
			state.InitialAll = true;
			state.Initial = new HashSet<int> (picked);
		} else
			state.Initial = viewModel.SelectedIndices ();
		pointer.Capture (list);
		if (AdornerLayer.GetAdornerLayer (panel) is AdornerLayer layer) {
			state.Adorner = new RubberBandAdorner ();
			AdornerLayer.SetAdornedElement (state.Adorner, panel);
			layer.Children.Add (state.Adorner);
		}
	}

	static void UpdateBand (ListBox list, UniformVirtualizingWrapPanel panel, GridState state)
	{
		if (state.Press is not Point press || WindowCommand.Model (list) is not MainWindowViewModel viewModel)
			return;
		var band = GridGeometry.Bounded (press, Content (panel, state.Pointer));
		if (state.Adorner is not null)
			state.Adorner.Band = band.Translate (new Vector (0, -panel.Offset.Y));
		var cells = GridGeometry.CellsInRect (band, panel.CellWidth, panel.CellHeight, panel.Columns, viewModel.Photos.Count);
		bool toggle = (state.PressModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0;
		int focus = cells.Count > 0 ? cells[0] : viewModel.SelectedPhoto is null ? -1 : viewModel.Photos.IndexOf (viewModel.SelectedPhoto);
		if (state.InitialAll) {
			int bandFocus = focus < 0 ? 0 : focus;
			viewModel.ApplyNavigation (GridNavigation.Band (state.Initial, cells, toggle, bandFocus));
			return;
		}
		var selection = GridGeometry.BandSelection (state.Initial, cells, toggle);
		if (focus < 0)
			focus = selection.Count > 0 ? selection.Min () : 0;
		if (!selection.SetEquals (viewModel.SelectedIndices ()) || cells.Count > 0)
			viewModel.ApplyGridSelection (selection, focus);
	}

	// GTK HandleMotionTimeout: scroll while the pointer is above or below the grid, faster the longer it stays there.
	static void UpdateAutoScroll (ListBox list, UniformVirtualizingWrapPanel panel, GridState state)
	{
		bool outside = state.Pointer.Y <= 0 || state.Pointer.Y >= panel.Bounds.Height;
		if (!outside) {
			state.ScrollTimer?.Stop ();
			state.ScrollDelta = 30;
			return;
		}
		if (state.ScrollTimer is { IsEnabled: true })
			return;
		state.ScrollDelta = 30;
		state.ScrollTimer ??= new DispatcherTimer (TimeSpan.FromMilliseconds (100), DispatcherPriority.Input, (_, _) => AutoScroll (list, panel, state));
		state.ScrollTimer.Start ();
	}

	static void AutoScroll (ListBox list, UniformVirtualizingWrapPanel panel, GridState state)
	{
		if (!state.Banding || list.FindDescendantOfType<ScrollViewer> () is not ScrollViewer scroller) {
			state.ScrollTimer?.Stop ();
			return;
		}
		if (state.ScrollDelta < 130)
			state.ScrollDelta += 15;
		double direction = state.Pointer.Y <= 0 ? -1 : state.Pointer.Y >= panel.Bounds.Height ? 1 : 0;
		if (direction == 0) {
			state.ScrollTimer?.Stop ();
			return;
		}
		double max = Math.Max (0, scroller.Extent.Height - scroller.Viewport.Height);
		scroller.Offset = new Vector (scroller.Offset.X, Math.Clamp (scroller.Offset.Y + direction * state.ScrollDelta, 0, max));
		UpdateBand (list, panel, state);
	}

	static void EndBand (ListBox list, GridState state)
	{
		state.ScrollTimer?.Stop ();
		if (state.Adorner is not null && AdornerLayer.GetAdornerLayer (state.Adorner) is AdornerLayer layer)
			layer.Children.Remove (state.Adorner);
		else if (state.Adorner?.Parent is Panel parent)
			parent.Children.Remove (state.Adorner);
		state.Adorner = null;
		state.Press = null;
		state.Banding = false;
	}

	sealed class RubberBandAdorner : Control
	{
		Rect band;

		public RubberBandAdorner ()
			=> IsHitTestVisible = false;

		public Rect Band {
			get => band;
			set {
				band = value;
				InvalidateVisual ();
			}
		}

		public override void Render (DrawingContext context)
		{
			var color = TryGetResource ("SystemAccentColor", ActualThemeVariant, out var value) && value is Color accent
				? accent
				: Colors.DodgerBlue;
			var fill = new SolidColorBrush (color, 0.25);
			var stroke = new Pen (new SolidColorBrush (color), 1);
			context.DrawRectangle (fill, stroke, band.Deflate (0.5));
		}
	}
}
