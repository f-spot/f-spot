//
// ViewerKeyMap.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Input;

namespace FSpot.Avalonia.Input;

public enum ViewerAction
{
	None,
	Previous,
	Next,
	First,
	Last,
	Random,
	ZoomFit,
	ZoomActual,
	ZoomDouble,
	ZoomIn,
	ZoomOut,
	PanUp,
	PanDown,
	PanLeft,
	PanRight
}

/// <summary>
/// GTK <c>PhotoImageView.OnKeyPressEvent</c> and <c>ImageView.OnKeyPressEvent</c> for the photo view.
/// Alt and Ctrl combinations pass through, like GTK.
/// </summary>
public static class ViewerKeyMap
{
	public static ViewerAction Map (Key key, KeyModifiers modifiers, bool zoomed)
	{
		if ((modifiers & (KeyModifiers.Alt | KeyModifiers.Control | KeyModifiers.Meta)) != 0)
			return ViewerAction.None;

		return key switch {
			Key.Up or Key.K => zoomed ? ViewerAction.PanUp : ViewerAction.Previous,
			Key.Left or Key.H => zoomed ? ViewerAction.PanLeft : ViewerAction.Previous,
			Key.Down or Key.J => zoomed ? ViewerAction.PanDown : ViewerAction.Next,
			Key.Right or Key.L => zoomed ? ViewerAction.PanRight : ViewerAction.Next,
			Key.PageUp or Key.Back or Key.B => ViewerAction.Previous,
			Key.PageDown or Key.Space or Key.N => ViewerAction.Next,
			Key.Home => ViewerAction.First,
			Key.End => ViewerAction.Last,
			Key.R => ViewerAction.Random,
			Key.D0 or Key.NumPad0 => ViewerAction.ZoomFit,
			Key.D1 or Key.NumPad1 => ViewerAction.ZoomActual,
			Key.D2 or Key.NumPad2 => ViewerAction.ZoomDouble,
			Key.OemPlus or Key.Add => ViewerAction.ZoomIn,
			Key.OemMinus or Key.Subtract => ViewerAction.ZoomOut,
			_ => ViewerAction.None
		};
	}
}
