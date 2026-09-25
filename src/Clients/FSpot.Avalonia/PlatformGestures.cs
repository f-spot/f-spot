// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;

namespace FSpot.Avalonia;

// Menu markup says Ctrl. On macOS the command key is Meta, from the platform hotkey configuration.
public static class PlatformGestures
{
	public static KeyModifiers CommandModifiers
		=> Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control;

	public static KeyGesture Retarget (KeyGesture gesture)
	{
		var command = CommandModifiers;
		if (command == KeyModifiers.Control || (gesture.KeyModifiers & KeyModifiers.Control) == 0)
			return gesture;
		var modifiers = (gesture.KeyModifiers & ~KeyModifiers.Control) | command;
		return new KeyGesture (gesture.Key, modifiers);
	}

	public static void Apply (NativeMenu? menu)
	{
		if (menu is null)
			return;
		foreach (object entry in menu.Items) {
			if (entry is not NativeMenuItem item)
				continue;
			if (item.Gesture is { } gesture)
				item.Gesture = Retarget (gesture);
			Apply (item.Menu);
		}
	}

	public static void Apply (Window window)
	{
		foreach (var binding in window.KeyBindings.ToList ()) {
			if (binding.Gesture is KeyGesture gesture)
				binding.Gesture = Retarget (gesture);
		}

		foreach (var child in window.GetLogicalChildren ())
			RetargetMenus (child);
	}

	static void RetargetMenus (object node)
	{
		if (node is MenuItem item && item.InputGesture is KeyGesture gesture)
			item.InputGesture = Retarget (gesture);
		if (node is ILogical logical) {
			foreach (var child in logical.GetLogicalChildren ())
				RetargetMenus (child);
		}
	}
}
