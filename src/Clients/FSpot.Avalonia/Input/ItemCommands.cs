//
// ItemCommands.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace FSpot.Avalonia.Input;

/// <summary>
/// Runs a command with the control's data context from a tap or a double-tap.
/// </summary>
public static class ItemCommands
{
	public static readonly AttachedProperty<ICommand?> TapProperty =
		AvaloniaProperty.RegisterAttached<Control, ICommand?> ("Tap", typeof (ItemCommands));

	public static readonly AttachedProperty<ICommand?> DoubleTapProperty =
		AvaloniaProperty.RegisterAttached<Control, ICommand?> ("DoubleTap", typeof (ItemCommands));

	static readonly AttachedProperty<bool> ListeningProperty =
		AvaloniaProperty.RegisterAttached<Control, bool> ("Listening", typeof (ItemCommands));

	static ItemCommands ()
	{
		TapProperty.Changed.AddClassHandler<Control> (OnChanged);
		DoubleTapProperty.Changed.AddClassHandler<Control> (OnChanged);
	}

	public static void SetTap (Control element, ICommand? value) => element.SetValue (TapProperty, value);
	public static ICommand? GetTap (Control element) => element.GetValue (TapProperty);
	public static void SetDoubleTap (Control element, ICommand? value) => element.SetValue (DoubleTapProperty, value);
	public static ICommand? GetDoubleTap (Control element) => element.GetValue (DoubleTapProperty);

	static void OnChanged (Control control, AvaloniaPropertyChangedEventArgs _)
	{
		if (control.GetValue (ListeningProperty))
			return;
		control.SetValue (ListeningProperty, true);
		control.AddHandler (InputElement.TappedEvent, OnTapped);
		control.AddHandler (InputElement.DoubleTappedEvent, OnDoubleTapped);
	}

	static void OnTapped (object? sender, TappedEventArgs e)
		=> Run (sender, GetTap, e);

	static void OnDoubleTapped (object? sender, TappedEventArgs e)
		=> Run (sender, GetDoubleTap, e);

	static void Run (object? sender, Func<Control, ICommand?> command, RoutedEventArgs e)
	{
		if (sender is not Control control)
			return;
		var bound = command (control);
		if (bound?.CanExecute (control.DataContext) != true)
			return;
		bound.Execute (control.DataContext);
		e.Handled = true;
	}
}
