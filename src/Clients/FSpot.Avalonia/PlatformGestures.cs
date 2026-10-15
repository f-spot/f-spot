// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;

using FSpot.Avalonia.Input;

namespace FSpot.Avalonia;

// Menu markup says Ctrl. On macOS the command key is Meta, from the platform hotkey configuration.
public static class PlatformGestures
{
	public static KeyModifiers CommandModifiers
		=> Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control;

	public static KeyGesture Retarget (KeyGesture gesture)
		=> Retarget (gesture, CommandModifiers);

	public static KeyGesture Retarget (KeyGesture gesture, KeyModifiers command)
	{
		if (command == KeyModifiers.Control || (gesture.KeyModifiers & KeyModifiers.Control) == 0)
			return gesture;
		var modifiers = (gesture.KeyModifiers & ~KeyModifiers.Control) | command;
		return new KeyGesture (gesture.Key, modifiers);
	}

	public static void ReleaseNativeEquivalents (NativeMenu? menu)
		=> ReleaseNativeEquivalents (menu, CommandModifiers);

	// macOS turns a native gesture into a key equivalent before the window's
	// focus guard runs. Copy the shortcut onto the window first, then clear
	// the native gesture for keys a text box must be able to type.
	public static void ReleaseNativeEquivalents (NativeMenu? menu, KeyModifiers command)
	{
		if (menu is null)
			return;
		foreach (object entry in menu.Items) {
			if (entry is not NativeMenuItem item)
				continue;
			if (item.Gesture is { } gesture && LeavesNativeMenu (gesture, command))
				item.Gesture = null;
			ReleaseNativeEquivalents (item.Menu, command);
		}
	}

	public static void GuardYieldingBindings (Window window)
		=> GuardYieldingBindings (window, CommandModifiers);

	// KeyBindings run before KeyDown. A binding that can yield keeps its
	// command behind the same focus guard, so a text box sees the key.
	public static void GuardYieldingBindings (Window window, KeyModifiers command)
	{
		foreach (var binding in window.KeyBindings) {
			if (binding.Gesture is not KeyGesture gesture || binding.Command is null or YieldingCommand)
				continue;
			if (!CanYield (gesture, command))
				continue;
			binding.Command = new YieldingCommand (window, gesture, command, binding.Command);
		}
	}

	static bool LeavesNativeMenu (KeyGesture gesture, KeyModifiers command)
		=> gesture.KeyModifiers == KeyModifiers.None || IsTextChord (gesture, command);

	static bool CanYield (KeyGesture gesture, KeyModifiers command)
		=> gesture.Key != Key.Escape && (gesture.KeyModifiers == KeyModifiers.None || IsTextChord (gesture, command));

	static bool IsTextChord (KeyGesture gesture, KeyModifiers command)
		=> gesture.KeyModifiers == command && gesture.Key is Key.C or Key.A or Key.V;

	sealed class YieldingCommand : ICommand
	{
		readonly Window window;
		readonly KeyGesture gesture;
		readonly KeyModifiers command;
		readonly ICommand inner;

		public YieldingCommand (Window window, KeyGesture gesture, KeyModifiers command, ICommand inner)
		{
			this.window = window;
			this.gesture = gesture;
			this.command = command;
			this.inner = inner;
			inner.CanExecuteChanged += (_, args) => CanExecuteChanged?.Invoke (this, args);
		}

		public event EventHandler? CanExecuteChanged;

		public bool CanExecute (object? parameter)
		{
			var focused = window.FocusManager?.GetFocusedElement ();
			if (!ShortcutGuards.ContinueToWindowKeyBindings (focused, gesture.Key, gesture.KeyModifiers, command))
				return false;
			return inner.CanExecute (parameter);
		}

		public void Execute (object? parameter)
			=> inner.Execute (parameter);
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
