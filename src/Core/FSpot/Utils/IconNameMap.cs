// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Utils
{
	// Toolbar and menus use Freedesktop icon names (media-playback-start).
	// GTK 2 stock is gtk-media-play. Without this map, IconSet lookup misses
	// and quartz paints gtk-missing-image even though the stock icon exists.
	public static class IconNameMap
	{
		public static string StockId (string name)
		{
			if (string.IsNullOrEmpty (name))
				return name;

			return name switch {
				"media-playback-start" => "gtk-media-play",
				"media-playback-pause" => "gtk-media-pause",
				"dialog-error" => "gtk-dialog-error",
				_ => name
			};
		}
	}
}
