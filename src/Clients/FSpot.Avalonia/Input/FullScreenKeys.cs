// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Input;

namespace FSpot.Avalonia.Input;

public enum FullScreenCommand
{
	None,
	Exit,
	ToggleInfo,
	RotateLeft,
	RotateRight,
	Rate,
	RemoveFromCatalog,
	DeleteFromDrive,
	Next,
	Previous,
}

public readonly record struct FullScreenKeyAction (FullScreenCommand Command, uint Rating = 0)
{
	public static FullScreenKeyAction None { get; } = new (FullScreenCommand.None);
}

/// <summary>
/// The GTK FullScreenView key handling (FullScreenView.OnKeyPressEvent, MainWindow.HandleCommonPhotoCommands
/// and the ImageView navigation keys) mapped onto Avalonia keys.
/// </summary>
public static class FullScreenKeys
{
	public static FullScreenKeyAction Map (Key key, KeyModifiers modifiers)
	{
		bool alt = (modifiers & KeyModifiers.Alt) != 0;
		bool shift = (modifiers & KeyModifiers.Shift) != 0;
		bool command = (modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0;

		if (key == Key.Delete && !alt && !command)
			return new (shift ? FullScreenCommand.DeleteFromDrive : FullScreenCommand.RemoveFromCatalog);

		if (alt && !command && Rating (key) is uint rating)
			return new (FullScreenCommand.Rate, rating);

		if (alt || command)
			return FullScreenKeyAction.None;

		return key switch {
			Key.F or Key.Q or Key.F11 or Key.Escape => new (FullScreenCommand.Exit),
			Key.I => new (FullScreenCommand.ToggleInfo),
			Key.OemOpenBrackets => new (FullScreenCommand.RotateLeft),
			Key.OemCloseBrackets => new (FullScreenCommand.RotateRight),
			Key.Right or Key.Down or Key.Space or Key.PageDown => new (FullScreenCommand.Next),
			Key.Left or Key.Up or Key.Back or Key.PageUp => new (FullScreenCommand.Previous),
			_ => FullScreenKeyAction.None,
		};
	}

	static uint? Rating (Key key)
		=> key switch {
			Key.D0 or Key.NumPad0 => 0,
			Key.D1 or Key.NumPad1 => 1,
			Key.D2 or Key.NumPad2 => 2,
			Key.D3 or Key.NumPad3 => 3,
			Key.D4 or Key.NumPad4 => 4,
			Key.D5 or Key.NumPad5 => 5,
			_ => null,
		};
}
