//
// DynamicMenu.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

using FSpot.Avalonia.Models;

namespace FSpot.Avalonia.Input;

/// <summary>
/// Fills a submenu from a <see cref="MenuSource"/> each time it opens, the way GTK's
/// TagMenu, PhotoTagMenu and PhotoVersionMenu repopulate. Works on a <see cref="NativeMenuItem"/>
/// (menu bar) and on a <see cref="MenuItem"/> (in-window menu and context menus). Items declared
/// in markup stay in place; the generated rows follow them.
/// </summary>
public static class DynamicMenu
{
	public static readonly AttachedProperty<MenuSource?> SourceProperty =
		AvaloniaProperty.RegisterAttached<AvaloniaObject, MenuSource?> ("Source", typeof (DynamicMenu));

	static readonly ConditionalWeakTable<AvaloniaObject, List<object>> generated = new ();

	static DynamicMenu ()
	{
		SourceProperty.Changed.AddClassHandler<NativeMenuItem> (OnNativeSourceChanged);
		SourceProperty.Changed.AddClassHandler<MenuItem> (OnMenuItemSourceChanged);
	}

	public static void SetSource (AvaloniaObject element, MenuSource? value) => element.SetValue (SourceProperty, value);
	public static MenuSource? GetSource (AvaloniaObject element) => element.GetValue (SourceProperty);

	static void OnNativeSourceChanged (NativeMenuItem item, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.OldValue is null && e.NewValue is not null) {
			item.PropertyChanged += (_, change) => {
				if (change.Property == NativeMenuItem.MenuProperty && change.NewValue is NativeMenu replaced)
					Hook (item, replaced);
			};
			if (item.Menu is { } existing)
				Hook (item, existing);
			else
				item.Menu = new NativeMenu ();
		}
		Fill (item);
	}

	static void Hook (NativeMenuItem item, NativeMenu menu)
	{
		menu.NeedsUpdate += (_, _) => Fill (item);
		menu.Opening += (_, _) => Fill (item);
	}

	static void OnMenuItemSourceChanged (MenuItem item, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.OldValue is null && e.NewValue is not null) {
			item.AddHandler (MenuItem.SubmenuOpenedEvent, OnOwnSubmenuOpened);
			if (item.GetLogicalParent () is null)
				item.AttachedToLogicalTree += OnAttached;
			else
				HookParent (item);
		}
		Fill (item);
	}

	// Refresh as soon as the enclosing menu opens so the rows are current before they show.
	static void OnAttached (object? sender, LogicalTreeAttachmentEventArgs e)
	{
		if (sender is not MenuItem item)
			return;
		item.AttachedToLogicalTree -= OnAttached;
		HookParent (item);
	}

	static void HookParent (MenuItem item)
	{
		switch (item.GetLogicalParent ()) {
		case ContextMenu context:
			context.Opening += (_, _) => Fill (item);
			break;
		case MenuItem parent:
			parent.AddHandler (MenuItem.SubmenuOpenedEvent, (_, args) => {
				if (ReferenceEquals (args.Source, parent))
					Fill (item);
			});
			break;
		}
	}

	static void OnOwnSubmenuOpened (object? sender, RoutedEventArgs e)
	{
		if (sender is MenuItem item && ReferenceEquals (e.Source, item) && item.GetLogicalParent () is Menu)
			Fill (item);
	}

	public static bool IsGenerated (AvaloniaObject owner, object item)
		=> generated.TryGetValue (owner, out var rows) && rows.Contains (item);

	public static void Fill (NativeMenuItem item)
	{
		if (GetSource (item) is not { } source || item.Menu is not { } menu)
			return;
		var previous = generated.GetOrCreateValue (item);
		foreach (var old in previous.OfType<NativeMenuItemBase> ())
			menu.Items.Remove (old);
		previous.Clear ();
		foreach (var entry in source.Build ()) {
			var native = ToNative (entry);
			menu.Items.Add (native);
			previous.Add (native);
		}
	}

	public static void Fill (MenuItem item)
	{
		if (GetSource (item) is not { } source)
			return;
		var previous = generated.GetOrCreateValue (item);
		foreach (var old in previous)
			item.Items.Remove (old);
		previous.Clear ();
		foreach (var entry in source.Build ()) {
			var control = ToControl (entry);
			item.Items.Add (control);
			previous.Add (control);
		}
	}

	public static NativeMenuItemBase ToNative (MenuEntry entry)
	{
		if (entry.IsSeparator)
			return new NativeMenuItemSeparator ();
		var item = new NativeMenuItem (entry.Header);
		if (entry.IsEnabled) {
			item.Command = entry.Command;
			item.CommandParameter = entry.CommandParameter;
		} else {
			item.IsEnabled = false;
		}
		if (entry.IsChecked is bool isChecked) {
			item.ToggleType = MenuItemToggleType.Radio;
			item.IsChecked = isChecked;
		}
		if (entry.HasChildren) {
			var menu = new NativeMenu ();
			foreach (var child in entry.Children)
				menu.Items.Add (ToNative (child));
			item.Menu = menu;
		}
		return item;
	}

	public static Control ToControl (MenuEntry entry)
	{
		if (entry.IsSeparator)
			return new Separator ();
		var item = new MenuItem { Header = entry.Header };
		if (entry.IsEnabled) {
			item.Command = entry.Command;
			item.CommandParameter = entry.CommandParameter;
		} else {
			item.IsEnabled = false;
		}
		if (entry.IsChecked is bool isChecked) {
			item.ToggleType = MenuItemToggleType.Radio;
			item.IsChecked = isChecked;
		}
		foreach (var child in entry.Children)
			item.Items.Add (ToControl (child));
		return item;
	}
}
