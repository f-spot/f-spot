// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Input;

namespace FSpot.Avalonia.Input;

// Plain, unmodified single-key shortcuts that must not steal keystrokes from
// a focused TextBox (find box, tag typing box, tag rename box, ...). Matches
// GTK's main_window.ui accelerators: rotate_left "[", rotate_right "]",
// sharpen "s", and HandleKeyPressEvent's "/" for the find bar.
public enum PlainKeyCommandKind
{
	RotateLeft,
	RotateRight,
	Sharpen,
	ShowFind,
}

public static class PlainKeyCommand
{
	public static PlainKeyCommandKind? From (Key key, KeyModifiers modifiers, object? focused)
	{
		if (modifiers != KeyModifiers.None || !ShortcutGuards.AllowsUnadornedKey (focused, key))
			return null;
		return key switch {
			Key.OemOpenBrackets => PlainKeyCommandKind.RotateLeft,
			Key.OemCloseBrackets => PlainKeyCommandKind.RotateRight,
			Key.S => PlainKeyCommandKind.Sharpen,
			Key.OemQuestion => PlainKeyCommandKind.ShowFind,
			_ => null
		};
	}
}
