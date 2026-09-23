// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Data;

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

	static IEnumerable<Control> Items (NativeMenu source)
	{
		foreach (object entry in source.Items) {
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
		item.Bind (MenuItem.IsEnabledProperty, BindTo (native, nameof (NativeMenuItem.IsEnabled)));
		if (native.Gesture is { } gesture)
			item.InputGesture = gesture;
		if (native.ToggleType != MenuItemToggleType.None) {
			item.ToggleType = native.ToggleType;
			item.Bind (MenuItem.IsCheckedProperty, BindTo (native, nameof (NativeMenuItem.IsChecked), BindingMode.TwoWay));
		}
		if (native.Menu is { } children) {
			foreach (var child in Items (children))
				item.Items.Add (child);
		}
		return item;
	}

	static Binding BindTo (object source, string path, BindingMode mode = BindingMode.OneWay)
		=> new (path) { Source = source, Mode = mode };
}
