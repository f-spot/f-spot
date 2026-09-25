//
// ViewerInput.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using FSpot.Avalonia.ViewModels;

namespace FSpot.Avalonia.Input;

/// <summary>
/// Photo view keys (<see cref="ViewerKeyMap"/>) on the window, ahead of its key bindings,
/// and the pointer anchor for 1 and 2. Set <c>ViewerInput.Surface</c> on the panel that
/// holds the preview; set <c>ViewerInput.Enable</c> on the window.
/// </summary>
public static class ViewerInput
{
	public static readonly AttachedProperty<bool> EnableProperty =
		AvaloniaProperty.RegisterAttached<Window, bool> ("Enable", typeof (ViewerInput));

	public static readonly AttachedProperty<bool> SurfaceProperty =
		AvaloniaProperty.RegisterAttached<Control, bool> ("Surface", typeof (ViewerInput));

	/// <summary>The margin between the surface and the preview image and its overlays.</summary>
	public static readonly AttachedProperty<double> InsetProperty =
		AvaloniaProperty.RegisterAttached<Control, double> ("Inset", typeof (ViewerInput), 8);

	static readonly AttachedProperty<Point?> PointerProperty =
		AvaloniaProperty.RegisterAttached<Control, Point?> ("Pointer", typeof (ViewerInput));

	static ViewerInput ()
	{
		EnableProperty.Changed.AddClassHandler<Window> ((window, args) => {
			if (args.GetNewValue<bool> ())
				window.AddHandler (InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
		});
		SurfaceProperty.Changed.AddClassHandler<Control> ((surface, args) => {
			if (!args.GetNewValue<bool> ())
				return;
			surface.AddHandler (InputElement.PointerMovedEvent, OnSurfaceMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
			surface.AddHandler (InputElement.PointerExitedEvent, (_, _) => surface.SetValue (PointerProperty, null));
			surface.AddHandler (InputElement.PointerPressedEvent, (_, _) => surface.Focus (NavigationMethod.Pointer), RoutingStrategies.Tunnel, handledEventsToo: true);
		});
	}

	public static void SetEnable (Window element, bool value) => element.SetValue (EnableProperty, value);
	public static bool GetEnable (Window element) => element.GetValue (EnableProperty);
	public static void SetSurface (Control element, bool value) => element.SetValue (SurfaceProperty, value);
	public static bool GetSurface (Control element) => element.GetValue (SurfaceProperty);
	public static void SetInset (Control element, double value) => element.SetValue (InsetProperty, value);
	public static double GetInset (Control element) => element.GetValue (InsetProperty);

	static void OnSurfaceMoved (object? sender, PointerEventArgs e)
	{
		if (sender is Control surface)
			surface.SetValue (PointerProperty, e.GetPosition (surface));
	}

	/// <summary>
	/// The view keys apply unless a control that uses the same key has focus: text entry,
	/// a list or tree that moves with the arrows, a button that clicks on Space, or the rating stars.
	/// </summary>
	public static bool FocusAllows (object? focused, Key key)
		=> ShortcutGuards.AllowsUnadornedKey (focused, key);

	static void OnKeyDown (object? sender, KeyEventArgs e)
	{
		if (sender is not Window window || window.DataContext is not MainWindowViewModel viewModel || !viewModel.IsEditing)
			return;
		var focused = window.FocusManager?.GetFocusedElement ();
		if (!FocusAllows (focused, e.Key))
			return;
		var action = ViewerKeyMap.Map (e.Key, e.KeyModifiers, viewModel.IsPreviewZoomed);
		if (action == ViewerAction.None)
			return;

		var surface = FindSurface (window);
		double inset = surface is null ? 0 : GetInset (surface);
		double width = Math.Max (0, (surface?.Bounds.Width ?? 0) - 2 * inset);
		double height = Math.Max (0, (surface?.Bounds.Height ?? 0) - 2 * inset);
		var pointer = surface?.GetValue (PointerProperty) is Point point
			? new Point (point.X - inset, point.Y - inset)
			: new Point (width / 2, height / 2);
		viewModel.RunViewerAction (action, width, height, pointer.X, pointer.Y);
		e.Handled = true;
	}

	static Control? FindSurface (Window window)
		=> window.GetVisualDescendants ().OfType<Control> ().FirstOrDefault (control => GetSurface (control) && control.IsEffectivelyVisible);
}
