// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Utils
{
	// Numeric values match Gtk.IconSize. ResourceLoader must not RenderIcon with
	// Invalid (0): Mac logs "Invalid icon size 0" and skips the embedded FSpot-22/48 pngs.
	public static class GtkIconSizeMap
	{
		public static bool TryMapPixels (int pixels, out int gtkIconSize)
		{
			gtkIconSize = pixels switch {
				16 => 2,       // SmallToolbar
				22 or 24 => 3, // LargeToolbar
				32 => 5,       // Dnd
				48 => 6,       // Dialog
				_ => 0         // Invalid
			};
			return gtkIconSize != 0;
		}
	}
}
