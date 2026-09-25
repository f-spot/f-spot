// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Input;

namespace FSpot.Avalonia.Input;

public static class RatingKey
{
	// GTK requires Alt+0-5 for the global accelerator (see
	// HandleCommonPhotoCommands); the Avalonia port additionally accepts the
	// plain digit for convenience, matching the existing behavior here.
	public static uint? From (Key key, KeyModifiers modifiers, object? focused)
	{
		// GTK HandleCommonPhotoCommands rates with Alt+digit. Plain digits also rate,
		// except where the photo view keeps 0, 1 and 2 for zoom.
		if (modifiers != KeyModifiers.None && modifiers != KeyModifiers.Alt)
			return null;
		if (!ShortcutGuards.AllowsUnadornedKey (focused, key))
			return null;
		return key switch {
			Key.D0 or Key.NumPad0 => 0,
			Key.D1 or Key.NumPad1 => 1,
			Key.D2 or Key.NumPad2 => 2,
			Key.D3 or Key.NumPad3 => 3,
			Key.D4 or Key.NumPad4 => 4,
			Key.D5 or Key.NumPad5 => 5,
			_ => null
		};
	}
}
