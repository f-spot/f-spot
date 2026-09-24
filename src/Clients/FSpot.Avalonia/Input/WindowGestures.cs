//
// WindowGestures.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using FSpot.Avalonia.Models;
using FSpot.Avalonia.ViewModels;

namespace FSpot.Avalonia.Input;

static class WindowCommand
{
	public static MainWindowViewModel? Model (StyledElement element)
	{
		for (StyledElement? current = element; current is not null; current = current.Parent) {
			if (current.DataContext is MainWindowViewModel model)
				return model;
		}
		return null;
	}
}

public enum TreeRow
{
	Tag,
	Folder
}

public static class TreeBackground
{
	public static readonly AttachedProperty<TreeRow?> ClearsProperty =
		AvaloniaProperty.RegisterAttached<Control, TreeRow?> ("Clears", typeof (TreeBackground));

	static TreeBackground ()
		=> ClearsProperty.Changed.AddClassHandler<Control> ((control, _) => {
			if (GetClears (control) is null)
				return;
			control.AddHandler (InputElement.PointerPressedEvent, OnPressed);
		});

	public static void SetClears (Control element, TreeRow? value) => element.SetValue (ClearsProperty, value);
	public static TreeRow? GetClears (Control element) => element.GetValue (ClearsProperty);

	static void OnPressed (object? sender, PointerPressedEventArgs e)
	{
		if (sender is not Control control || WindowCommand.Model (control) is not MainWindowViewModel viewModel)
			return;
		if (GetClears (control) == TreeRow.Tag) {
			if (Ancestor<TagNode> (e.Source as Visual) is not null)
				return;
			viewModel.SelectTagCommand.Execute (null);
		} else if (Ancestor<FolderNode> (e.Source as Visual) is null) {
			viewModel.SelectFolderCommand.Execute (null);
		}
	}

	static T? Ancestor<T> (Visual? start) where T : class
	{
		for (var visual = start; visual is not null; visual = visual.GetVisualParent ()) {
			if (visual is Control { DataContext: T value })
				return value;
		}
		return null;
	}
}

public static class ThumbnailItems
{
	public static readonly AttachedProperty<bool> EnableProperty =
		AvaloniaProperty.RegisterAttached<ItemsControl, bool> ("Enable", typeof (ThumbnailItems));

	static ThumbnailItems ()
		=> EnableProperty.Changed.AddClassHandler<ItemsControl> ((list, args) => {
			if (!args.GetNewValue<bool> ())
				return;
			list.ContainerPrepared += OnPrepared;
			list.ContainerClearing += OnClearing;
		});

	public static void SetEnable (ItemsControl element, bool value) => element.SetValue (EnableProperty, value);
	public static bool GetEnable (ItemsControl element) => element.GetValue (EnableProperty);

	static void OnPrepared (object? sender, ContainerPreparedEventArgs e)
	{
		if (sender is not ItemsControl list || WindowCommand.Model (list) is not MainWindowViewModel viewModel)
			return;
		if ((uint)e.Index >= (uint)list.Items.Count || list.Items[e.Index] is not PhotoItemViewModel item)
			return;
		_ = viewModel.RealizeThumbnailAsync (e.Container, item);
	}

	static void OnClearing (object? sender, ContainerClearingEventArgs e)
	{
		if (sender is ItemsControl list && WindowCommand.Model (list) is MainWindowViewModel viewModel)
			viewModel.ReleaseThumbnail (e.Container);
	}
}

public static class FilmstripPick
{
	public static readonly AttachedProperty<bool> EnableProperty =
		AvaloniaProperty.RegisterAttached<ListBox, bool> ("Enable", typeof (FilmstripPick));

	static readonly AttachedProperty<bool> ChoosingProperty =
		AvaloniaProperty.RegisterAttached<ListBox, bool> ("Choosing", typeof (FilmstripPick));

	static FilmstripPick ()
		=> EnableProperty.Changed.AddClassHandler<ListBox> ((list, args) => {
			if (!args.GetNewValue<bool> ())
				return;
			list.AddHandler (InputElement.PointerPressedEvent, OnPressed);
			list.AddHandler (InputElement.PointerReleasedEvent, OnReleased);
			list.AddHandler (InputElement.KeyDownEvent, OnKeyDown);
			list.AddHandler (InputElement.KeyUpEvent, OnKeyUp);
			list.SelectionChanged += OnSelectionChanged;
		});

	public static void SetEnable (ListBox element, bool value) => element.SetValue (EnableProperty, value);

	static void OnPressed (object? sender, PointerPressedEventArgs e)
		=> Mark (sender, true);

	static void OnReleased (object? sender, PointerReleasedEventArgs e)
		=> Mark (sender, false);

	static void OnKeyDown (object? sender, KeyEventArgs e)
		=> Mark (sender, true);

	static void OnKeyUp (object? sender, KeyEventArgs e)
		=> Mark (sender, false);

	static void Mark (object? sender, bool choosing)
	{
		if (sender is ListBox list)
			list.SetValue (ChoosingProperty, choosing);
	}

	static void OnSelectionChanged (object? sender, SelectionChangedEventArgs e)
	{
		if (sender is not ListBox list || !list.GetValue (ChoosingProperty))
			return;
		if (WindowCommand.Model (list) is MainWindowViewModel viewModel && list.SelectedItem is PhotoItemViewModel item)
			viewModel.SelectFromFilmstrip (item);
	}
}

public static class PreviewPointer
{
	public static readonly AttachedProperty<bool> EnableProperty =
		AvaloniaProperty.RegisterAttached<Control, bool> ("Enable", typeof (PreviewPointer));

	static readonly AttachedProperty<Point?> OriginProperty =
		AvaloniaProperty.RegisterAttached<Control, Point?> ("Origin", typeof (PreviewPointer));

	static PreviewPointer ()
		=> EnableProperty.Changed.AddClassHandler<Control> ((image, args) => {
			if (!args.GetNewValue<bool> ())
				return;
			image.AddHandler (InputElement.PointerMovedEvent, OnMoved);
			image.AddHandler (InputElement.PointerPressedEvent, OnPressed);
			image.AddHandler (InputElement.PointerReleasedEvent, OnReleased);
			image.AddHandler (InputElement.PointerWheelChangedEvent, OnWheel);
		});

	public static void SetEnable (Control element, bool value) => element.SetValue (EnableProperty, value);

	static void OnMoved (object? sender, PointerEventArgs e)
	{
		if (sender is not Control image || WindowCommand.Model (image) is not MainWindowViewModel viewModel)
			return;
		var point = e.GetPosition (image);
		if (image.GetValue (OriginProperty) is Point origin) {
			viewModel.PanPreviewBy (point.X - origin.X, point.Y - origin.Y);
			image.SetValue (OriginProperty, point);
		}
		if (viewModel.IsLoupeVisible)
			viewModel.MoveLoupe (point.X, point.Y, image.Bounds.Width, image.Bounds.Height);
	}

	static void OnPressed (object? sender, PointerPressedEventArgs e)
	{
		if (sender is not Control image)
			return;
		if (!e.KeyModifiers.HasFlag (KeyModifiers.Control) || !e.GetCurrentPoint (image).Properties.IsLeftButtonPressed)
			return;
		image.SetValue (OriginProperty, e.GetPosition (image));
		e.Pointer.Capture (image);
		e.Handled = true;
	}

	static void OnReleased (object? sender, PointerReleasedEventArgs e)
	{
		if (sender is not Control image || image.GetValue (OriginProperty) is null)
			return;
		image.SetValue (OriginProperty, null);
		e.Pointer.Capture (null);
	}

	static void OnWheel (object? sender, PointerWheelEventArgs e)
	{
		if (sender is not Control image || WindowCommand.Model (image) is not MainWindowViewModel viewModel)
			return;
		viewModel.ZoomPreviewCommand.Execute (e.Delta.Y > 0 ? 1.1 : 0.9);
		e.Handled = true;
	}
}

public static class TagRenameInput
{
	public static readonly AttachedProperty<bool> EnableProperty =
		AvaloniaProperty.RegisterAttached<Control, bool> ("Enable", typeof (TagRenameInput));

	static TagRenameInput ()
		=> EnableProperty.Changed.AddClassHandler<Control> ((box, args) => {
			if (!args.GetNewValue<bool> ())
				return;
			box.AddHandler (InputElement.LostFocusEvent, OnLostFocus);
			box.AddHandler (InputElement.KeyDownEvent, OnKeyDown);
		});

	public static void SetEnable (Control element, bool value) => element.SetValue (EnableProperty, value);

	static void OnLostFocus (object? sender, RoutedEventArgs e)
	{
		if (sender is Control { DataContext: TagNode node } box && WindowCommand.Model (box) is MainWindowViewModel viewModel)
			viewModel.CommitRenameTagCommand.Execute (node);
	}

	static void OnKeyDown (object? sender, KeyEventArgs e)
	{
		if (e.Key != Key.Enter && e.Key != Key.Escape)
			return;
		if (sender is not Control { DataContext: TagNode node } box || WindowCommand.Model (box) is not MainWindowViewModel viewModel)
			return;
		if (e.Key == Key.Enter)
			viewModel.CommitRenameTagCommand.Execute (node);
		else {
			node.Name = node.Tag.Name;
			node.IsRenaming = false;
		}
		e.Handled = true;
	}
}

public static class EnterCommand
{
	public static readonly AttachedProperty<ICommand?> CommandProperty =
		AvaloniaProperty.RegisterAttached<Control, ICommand?> ("Command", typeof (EnterCommand));

	static EnterCommand ()
		=> CommandProperty.Changed.AddClassHandler<Control> ((box, _) => box.AddHandler (InputElement.KeyDownEvent, OnKeyDown));

	public static void SetCommand (Control element, ICommand? value) => element.SetValue (CommandProperty, value);
	public static ICommand? GetCommand (Control element) => element.GetValue (CommandProperty);

	static void OnKeyDown (object? sender, KeyEventArgs e)
	{
		if (e.Key != Key.Enter || sender is not Control box)
			return;
		var command = GetCommand (box);
		if (command?.CanExecute (null) != true)
			return;
		command.Execute (null);
		e.Handled = true;
	}
}

public static class CellHover
{
	public static readonly AttachedProperty<bool> EnableProperty =
		AvaloniaProperty.RegisterAttached<Control, bool> ("Enable", typeof (CellHover));

	static CellHover ()
		=> EnableProperty.Changed.AddClassHandler<Control> ((cell, args) => {
			if (!args.GetNewValue<bool> ())
				return;
			cell.AddHandler (InputElement.PointerEnteredEvent, OnEntered);
			cell.AddHandler (InputElement.PointerExitedEvent, OnExited);
		});

	public static void SetEnable (Control element, bool value) => element.SetValue (EnableProperty, value);

	static void OnEntered (object? sender, PointerEventArgs e)
	{
		if (sender is not Control cell || cell.DataContext is not PhotoItemViewModel item)
			return;
		if (TopLevel.GetTopLevel (cell) is not Window window || WindowCommand.Model (cell) is not MainWindowViewModel viewModel)
			return;
		if (window.FindControl<Popup> ("HoverPopup") is Popup popup)
			popup.PlacementTarget = cell;
		var screen = window.Screens?.ScreenFromVisual (window)?.Bounds;
		int width = screen?.Width ?? (int)window.Bounds.Width;
		int height = screen?.Height ?? (int)window.Bounds.Height;
		_ = viewModel.ShowHoverAsync (item, width, height);
	}

	static void OnExited (object? sender, PointerEventArgs e)
	{
		if (sender is Control cell && WindowCommand.Model (cell) is MainWindowViewModel viewModel)
			viewModel.HideHover ();
	}
}
