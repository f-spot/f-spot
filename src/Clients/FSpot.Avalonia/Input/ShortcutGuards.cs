using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;

using FSpot.Avalonia;

namespace FSpot.Avalonia.Input;

public static class ShortcutGuards
{
	public static bool IsUnadornedLoupeKey (Key key, KeyModifiers modifiers)
		=> key == Key.V && modifiers == KeyModifiers.None;

	public static bool IsTagTypingKey (Key key, KeyModifiers modifiers, object? focused)
		=> key == Key.T && modifiers == KeyModifiers.None && AllowsUnadornedKey (focused, key);

	// Text, lists, sliders and combos own their keys. A button owns Space and Enter.
	// stopAt is the handler's own control: the tag tree must not reject the keys
	// it handles, while a text box inside that tree still owns what it types.
	public static bool AllowsUnadornedKey (object? focused, Key key = Key.None, Visual? stopAt = null)
	{
		for (var visual = focused as Visual; visual is not null; visual = visual.GetVisualParent ()) {
			if (stopAt is not null && ReferenceEquals (visual, stopAt))
				return true;
			if (visual is Control control && RatingInput.GetEnable (control))
				return false;
			if (visual is TextBox or ComboBox or Slider or TreeView or ScrollBar)
				return false;
			if (key is Key.Space or Key.Enter && visual is Button)
				return false;
			if (visual is TopLevel)
				break;
		}

		return true;
	}

	// Window KeyBindings run in base.OnKeyDown. Returning false means the
	// caller must not call base, so V can type into a TextBox.
	public static bool ContinueToWindowKeyBindings (object? focused, Key key, KeyModifiers modifiers)
		=> ContinueToWindowKeyBindings (focused, key, modifiers, PlatformGestures.CommandModifiers);

	public static bool ContinueToWindowKeyBindings (object? focused, Key key, KeyModifiers modifiers, KeyModifiers command)
	{
		if (modifiers == command && key is Key.C or Key.A or Key.V && OwnsText (focused))
			return false;
		if (modifiers == KeyModifiers.None && YieldsWhenTyping (key) && !AllowsUnadornedKey (focused, key))
			return false;
		return true;
	}

	// Window bindings for these keys must not run while a text box, list, or
	// slider owns the key. Escape still dismisses find from a text box.
	static bool YieldsWhenTyping (Key key)
		=> key is Key.V or Key.F1 or Key.F5 or Key.F9 or Key.F11
			or Key.Delete or Key.Left or Key.Right or Key.Up or Key.Down
			or Key.OemOpenBrackets or Key.OemCloseBrackets;

	static bool OwnsText (object? focused)
	{
		for (var visual = focused as Visual; visual is not null; visual = visual.GetVisualParent ()) {
			if (visual is TextBox)
				return true;
			if (visual is TopLevel)
				break;
		}
		return false;
	}
}
