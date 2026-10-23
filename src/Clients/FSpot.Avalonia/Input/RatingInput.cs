//
// RatingInput.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using FSpot.Avalonia.ViewModels;

namespace FSpot.Avalonia.Input;

/// <summary>
/// GTK <c>RatingEntry</c> keys and wheel on a row of rating stars: arrows, +, = and - step
/// the rating, digits set it, and the wheel steps it (<see cref="RatingStep"/>).
/// </summary>
public static class RatingInput
{
	public static readonly AttachedProperty<bool> EnableProperty =
		AvaloniaProperty.RegisterAttached<Control, bool> ("Enable", typeof (RatingInput));

	static RatingInput ()
		=> EnableProperty.Changed.AddClassHandler<Control> ((control, args) => {
			if (!args.GetNewValue<bool> ())
				return;
			control.AddHandler (InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
			control.AddHandler (InputElement.PointerWheelChangedEvent, OnWheel);
			control.AddHandler (InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
			control.AddHandler (InputElement.PointerExitedEvent, OnPointerExited, RoutingStrategies.Tunnel);
		});

	public static void SetEnable (Control element, bool value) => element.SetValue (EnableProperty, value);
	public static bool GetEnable (Control element) => element.GetValue (EnableProperty);

	static void OnKeyDown (object? sender, KeyEventArgs e)
	{
		if (sender is not Control control || WindowCommand.Model (control) is not MainWindowViewModel viewModel)
			return;
		if ((e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) != 0)
			return;
		int delta = RatingStep.KeyDelta (e.Key);
		if (delta != 0) {
			viewModel.StepRating (delta);
			e.Handled = true;
			return;
		}
		if (RatingKey.From (e.Key, KeyModifiers.None, control) is uint rating) {
			viewModel.RateFocused (rating);
			e.Handled = true;
		}
	}

	static void OnPointerMoved (object? sender, PointerEventArgs e)
	{
		if (sender is not Control control || WindowCommand.Model (control) is not MainWindowViewModel viewModel)
			return;
		viewModel.Info.PreviewStars (StarUnder (control, e.GetPosition (control)));
	}

	static void OnPointerExited (object? sender, PointerEventArgs e)
	{
		if (sender is not Control control || WindowCommand.Model (control) is not MainWindowViewModel viewModel)
			return;
		viewModel.Info.PreviewStars (null);
	}

	static uint? StarUnder (Control root, Point point)
	{
		var hit = root.InputHitTest (point) as StyledElement;
		while (hit is not null && !ReferenceEquals (hit, root)) {
			if (hit is Button button && RatingOf (button.CommandParameter) is uint rating)
				return rating;
			hit = hit.Parent as StyledElement;
		}
		return null;
	}

	static uint? RatingOf (object? parameter)
		=> parameter switch {
			uint value when value is > 0 and <= 5 => value,
			int value when value is > 0 and <= 5 => (uint)value,
			_ => null
		};

	static void OnWheel (object? sender, PointerWheelEventArgs e)
	{
		if (sender is not Control control || WindowCommand.Model (control) is not MainWindowViewModel viewModel)
			return;
		int delta = RatingStep.WheelDelta (e.Delta.X, e.Delta.Y);
		if (delta == 0)
			return;
		viewModel.StepRating (delta);
		e.Handled = true;
	}
}
