//
// QueryDrop.cs
//
// The query bar's tag logic gestures, after GTK's LogicWidget and Literal:
// drop tags on the bar to OR them in, drop on a tag to AND with it, drag tags
// between groups (or off the bar to remove them), double-click to negate, Delete to remove.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

using FSpot.Avalonia.ViewModels;

namespace FSpot.Avalonia.Input;

public enum QueryDropTarget
{
	Bar,
	Literal
}

public static class QueryDrop
{
	public static readonly AttachedProperty<QueryDropTarget?> TargetProperty =
		AvaloniaProperty.RegisterAttached<Control, QueryDropTarget?> ("Target", typeof (QueryDrop));

	static readonly AttachedProperty<PointerPressedEventArgs?> ArmProperty =
		AvaloniaProperty.RegisterAttached<Control, PointerPressedEventArgs?> ("Arm", typeof (QueryDrop));

	public static readonly IValueConverter NegatedOpacity = new FuncValueConverter<bool, double> (negated => negated ? 0.4 : 1.0);

	public static readonly IValueConverter NegatedDecorations =
		new FuncValueConverter<bool, TextDecorationCollection?> (negated => negated ? TextDecorations.Strikethrough : null);

	// The literal being dragged, like GTK's Literal.FocusedLiterals: the drag stays in process.
	static QueryLiteralViewModel? dragging;

	static QueryDrop ()
		=> TargetProperty.Changed.AddClassHandler<Control> (OnTargetChanged);

	public static void SetTarget (Control element, QueryDropTarget? value) => element.SetValue (TargetProperty, value);
	public static QueryDropTarget? GetTarget (Control element) => element.GetValue (TargetProperty);

	public static DragDropEffects Effects (IDataTransfer transfer)
		=> transfer.Contains (DragFormats.TagId) || transfer.Contains (DragFormats.QueryLiteral)
			? DragDropEffects.Copy | DragDropEffects.Move
			: DragDropEffects.None;

	/// <summary>
	/// Applies a drop: <paramref name="after"/> is the tag dropped on, or null for the bar itself.
	/// Returns false when the payload is not something the query bar takes.
	/// </summary>
	public static bool Apply (MainWindowViewModel viewModel, IDataTransfer transfer, QueryLiteralViewModel? after, QueryLiteralViewModel? moving)
	{
		if (transfer.Contains (DragFormats.QueryLiteral)) {
			if (moving is null || ReferenceEquals (moving, after))
				return false;
			viewModel.MoveQueryLiteral (moving, after);
			return true;
		}
		if (transfer.Contains (DragFormats.TagId)) {
			viewModel.DropTagOnQuery (CatalogDrop.Read (transfer, DragFormats.TagId), after);
			return true;
		}
		return false;
	}

	static void OnTargetChanged (Control control, AvaloniaPropertyChangedEventArgs args)
	{
		if (args.OldValue is not null)
			return;
		control.AddHandler (DragDrop.DragOverEvent, OnDragOver);
		control.AddHandler (DragDrop.DropEvent, OnDrop);
		if (args.GetNewValue<QueryDropTarget?> () != QueryDropTarget.Literal)
			return;
		control.AddHandler (InputElement.DoubleTappedEvent, OnDoubleTapped);
		control.AddHandler (InputElement.KeyDownEvent, OnKeyDown);
		control.AddHandler (InputElement.PointerPressedEvent, OnPressed);
		control.AddHandler (InputElement.PointerMovedEvent, OnMoved);
		control.AddHandler (InputElement.PointerReleasedEvent, OnReleased);
	}

	static void OnDragOver (object? sender, DragEventArgs e)
	{
		if (sender is not Control)
			return;
		e.DragEffects = Effects (e.DataTransfer);
		if (e.DragEffects != DragDropEffects.None)
			e.Handled = true;
	}

	static void OnDrop (object? sender, DragEventArgs e)
	{
		if (sender is not Control control || e.Handled || WindowCommand.Model (control) is not MainWindowViewModel viewModel)
			return;
		var after = GetTarget (control) == QueryDropTarget.Literal ? control.DataContext as QueryLiteralViewModel : null;
		var moving = dragging;
		if (Apply (viewModel, e.DataTransfer, after, moving)) {
			dragging = null;
			e.DragEffects = DragDropEffects.Move;
			e.Handled = true;
		}
	}

	static void OnDoubleTapped (object? sender, TappedEventArgs e)
	{
		if (sender is Control { DataContext: QueryLiteralViewModel literal } control
			&& WindowCommand.Model (control) is MainWindowViewModel viewModel) {
			viewModel.ToggleQueryLiteralNegatedCommand.Execute (literal);
			e.Handled = true;
		}
	}

	static void OnKeyDown (object? sender, KeyEventArgs e)
	{
		if (e.Key != Key.Delete || e.KeyModifiers != KeyModifiers.None)
			return;
		if (sender is Control { DataContext: QueryLiteralViewModel literal } control
			&& WindowCommand.Model (control) is MainWindowViewModel viewModel) {
			viewModel.RemoveQueryLiteralCommand.Execute (literal);
			e.Handled = true;
		}
	}

	static void OnPressed (object? sender, PointerPressedEventArgs e)
	{
		if (sender is not Control control || !e.GetCurrentPoint (control).Properties.IsLeftButtonPressed)
			return;
		control.Focus ();
		if (e.ClickCount > 1)
			return;
		control.SetValue (ArmProperty, e);
	}

	static void OnMoved (object? sender, PointerEventArgs e)
	{
		if (sender is not Control control || control.GetValue (ArmProperty) is not PointerPressedEventArgs press)
			return;
		if (!DragGesture.MovedFarEnough (press.GetPosition (control), e.GetPosition (control)))
			return;
		control.SetValue (ArmProperty, null);
		if (control.DataContext is not QueryLiteralViewModel literal || WindowCommand.Model (control) is not MainWindowViewModel viewModel)
			return;

		_ = DragLiteralAsync (press, literal, viewModel);
	}

	static async Task DragLiteralAsync (PointerPressedEventArgs press, QueryLiteralViewModel literal, MainWindowViewModel viewModel)
	{
		dragging = literal;
		var data = new DataTransfer ();
		data.Add (DataTransferItem.Create (DragFormats.QueryLiteral, literal.AutomationId));
		var effect = await DragDrop.DoDragDropAsync (press, data, DragDropEffects.Move);
		// A tag dragged off the query bar and dropped nowhere is thrown away, as in GTK.
		if (effect == DragDropEffects.None && ReferenceEquals (dragging, literal))
			viewModel.RemoveQueryLiteralCommand.Execute (literal);
		dragging = null;
	}

	static void OnReleased (object? sender, PointerReleasedEventArgs e)
	{
		if (sender is Control control)
			control.SetValue (ArmProperty, null);
	}
}
