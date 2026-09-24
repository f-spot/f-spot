//
// PointerDrag.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using FSpot.Avalonia.Models;
using FSpot.Avalonia.ViewModels;

namespace FSpot.Avalonia.Input;

/// <summary>
/// Starts a drag after the pointer moves past <see cref="DragGesture.Threshold"/>.
/// The payload comes from the control's data context: a tag, a folder, or a photo.
/// </summary>
public static class PointerDrag
{
	public readonly record struct Payload (DataFormat<string> Format, string Value);

	public static readonly AttachedProperty<bool> IsSourceProperty =
		AvaloniaProperty.RegisterAttached<Control, bool> ("IsSource", typeof (PointerDrag));

	public static readonly AttachedProperty<ICommand?> PressCommandProperty =
		AvaloniaProperty.RegisterAttached<Control, ICommand?> ("PressCommand", typeof (PointerDrag));

	public static readonly AttachedProperty<ICommand?> RightPressCommandProperty =
		AvaloniaProperty.RegisterAttached<Control, ICommand?> ("RightPressCommand", typeof (PointerDrag));

	static readonly AttachedProperty<DragArm?> ArmProperty =
		AvaloniaProperty.RegisterAttached<Control, DragArm?> ("Arm", typeof (PointerDrag));

	static PointerDrag ()
	{
		IsSourceProperty.Changed.AddClassHandler<Control> (OnIsSourceChanged);
	}

	public static void SetIsSource (Control element, bool value) => element.SetValue (IsSourceProperty, value);
	public static bool GetIsSource (Control element) => element.GetValue (IsSourceProperty);
	public static void SetPressCommand (Control element, ICommand? value) => element.SetValue (PressCommandProperty, value);
	public static ICommand? GetPressCommand (Control element) => element.GetValue (PressCommandProperty);
	public static void SetRightPressCommand (Control element, ICommand? value) => element.SetValue (RightPressCommandProperty, value);
	public static ICommand? GetRightPressCommand (Control element) => element.GetValue (RightPressCommandProperty);

	public static Payload? For (object? source)
		=> source switch {
			TagNode tag => new Payload (DragFormats.TagId, tag.Tag.Id.ToString ()),
			FolderNode folder => new Payload (DragFormats.FolderUri, folder.Uri.AbsoluteUri),
			PhotoItemViewModel => new Payload (DragFormats.Photos, "1"),
			_ => null
		};

	static void OnIsSourceChanged (Control control, AvaloniaPropertyChangedEventArgs args)
	{
		if (args.GetNewValue<bool> ()) {
			control.AddHandler (InputElement.PointerPressedEvent, OnPressed);
			control.AddHandler (InputElement.PointerMovedEvent, OnMoved);
			control.AddHandler (InputElement.PointerReleasedEvent, OnReleased);
		} else {
			control.RemoveHandler (InputElement.PointerPressedEvent, OnPressed);
			control.RemoveHandler (InputElement.PointerMovedEvent, OnMoved);
			control.RemoveHandler (InputElement.PointerReleasedEvent, OnReleased);
		}
	}

	static void OnPressed (object? sender, PointerPressedEventArgs e)
	{
		if (sender is not Control control)
			return;
		var point = e.GetCurrentPoint (control);
		if (point.Properties.IsRightButtonPressed) {
			Invoke (GetRightPressCommand (control), control.DataContext);
			return;
		}
		if (!point.Properties.IsLeftButtonPressed)
			return;
		Invoke (GetPressCommand (control), control.DataContext);
		if (For (control.DataContext) is null)
			return;
		control.SetValue (ArmProperty, new DragArm (e));
		e.Pointer.Capture (control);
	}

	static void OnMoved (object? sender, PointerEventArgs e)
	{
		if (sender is not Control control || control.GetValue (ArmProperty) is not DragArm arm)
			return;
		if (!DragGesture.MovedFarEnough (arm.Press.GetPosition (control), e.GetPosition (control)))
			return;
		control.SetValue (ArmProperty, null);
		e.Pointer.Capture (null);
		if (For (control.DataContext) is not Payload payload)
			return;
		var data = new DataTransfer ();
		data.Add (DataTransferItem.Create (payload.Format, payload.Value));
		_ = DragDrop.DoDragDropAsync (arm.Press, data, DragDropEffects.Copy);
	}

	static void OnReleased (object? sender, PointerReleasedEventArgs e)
	{
		if (sender is not Control control || control.GetValue (ArmProperty) is null)
			return;
		control.SetValue (ArmProperty, null);
		e.Pointer.Capture (null);
	}

	static void Invoke (ICommand? command, object? parameter)
	{
		if (command?.CanExecute (parameter) == true)
			command.Execute (parameter);
	}

	sealed class DragArm (PointerPressedEventArgs press)
	{
		public PointerPressedEventArgs Press { get; } = press;
	}
}
