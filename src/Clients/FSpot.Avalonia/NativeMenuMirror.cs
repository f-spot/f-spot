// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows.Input;

using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;

namespace FSpot.Avalonia;

public static class NativeMenuMirror
{
	public static void Fill (Menu menu, NativeMenu? source)
	{
		if (source is null || menu.Items.Count > 0)
			return;
		foreach (var item in Items (source))
			menu.Items.Add (item);
	}

	// The native macOS menu bar turns a NativeMenuItem's Gesture into a real,
	// OS-level key equivalent on its own. Nothing does that off macOS (and
	// for the in-window automation mirror above), so every gesture is copied
	// onto the window's own KeyBindings too, making every menu accelerator
	// work as a genuine shortcut everywhere. Existing declarative
	// Window.KeyBindings entries win over an auto-derived one for the same
	// gesture.
	public static void ApplyAsKeyBindings (Window window, NativeMenu? source)
	{
		if (source is null)
			return;
		var existing = new HashSet<KeyGesture> ();
		foreach (var binding in window.KeyBindings)
			if (binding.Gesture is { } gesture)
				existing.Add (gesture);
		foreach (var (gesture, command, parameter) in Gestures (source)) {
			if (command is null || !existing.Add (gesture))
				continue;
			window.KeyBindings.Add (new KeyBinding {
				Gesture = gesture,
				Command = command,
				CommandParameter = parameter!,
			});
		}
	}

	static IEnumerable<(KeyGesture Gesture, ICommand? Command, object? Parameter)> Gestures (NativeMenu menu)
	{
		foreach (object entry in menu.Items) {
			if (entry is not NativeMenuItem item)
				continue;
			if (item.Gesture is { } gesture)
				yield return (gesture, item.Command, item.CommandParameter);
			if (item.Menu is { } children)
				foreach (var nested in Gestures (children))
					yield return nested;
		}
	}

	// Rows a dynamic menu generated are rebuilt by the mirrored item itself, so only markup rows are copied.
	static IEnumerable<Control> Items (NativeMenu source, NativeMenuItem? dynamicOwner = null)
	{
		foreach (object entry in source.Items) {
			if (dynamicOwner is not null && Input.DynamicMenu.IsGenerated (dynamicOwner, entry))
				continue;
			if (entry is NativeMenuItemSeparator) {
				yield return new Separator ();
				continue;
			}
			if (entry is NativeMenuItem native)
				yield return Copy (native);
		}
	}

	static MenuItem Copy (NativeMenuItem native)
	{
		var item = new MenuItem ();
		item.Bind (MenuItem.HeaderProperty, BindTo (native, nameof (NativeMenuItem.Header)));
		item.Bind (MenuItem.CommandProperty, BindTo (native, nameof (NativeMenuItem.Command)));
		item.Bind (MenuItem.CommandParameterProperty, BindTo (native, nameof (NativeMenuItem.CommandParameter)));
		item.Bind (MenuItem.IsEnabledProperty, BindTo (native, nameof (NativeMenuItem.IsEnabled)));
		if (native.Gesture is { } gesture)
			item.InputGesture = gesture;
		if (native.ToggleType != MenuItemToggleType.None) {
			item.ToggleType = native.ToggleType;
			item.Bind (MenuItem.IsCheckedProperty, BindTo (native, nameof (NativeMenuItem.IsChecked), BindingMode.TwoWay));
		}
		var source = Input.DynamicMenu.GetSource (native);
		if (native.Menu is { } children) {
			foreach (var child in Items (children, source is not null ? native : null))
				item.Items.Add (child);
		}
		if (source is not null)
			Input.DynamicMenu.SetSource (item, source);
		return item;
	}

	static Binding BindTo (object source, string path, BindingMode mode = BindingMode.OneWay)
		=> new (path) { Source = source, Mode = mode };
}
