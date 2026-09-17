//
// GtkSettings.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using FSpot.Settings;

namespace FSpot
{
	public static class GtkSettings
	{
		static Gtk.IconTheme icon_theme;
		public static Gtk.IconTheme IconTheme {
			get {
				if (icon_theme == null) {
					icon_theme = Gtk.IconTheme.Default;
					icon_theme.AppendSearchPath (Path.Combine (FSpotConfiguration.AppDataDir, "icons"));
					RegisterStockAliases ();
				}
				return icon_theme;
			}
		}

		static string[] default_rc_files;
		public static string[] DefaultRcFiles {
			get { return default_rc_files ?? Gtk.Rc.DefaultFiles; }
			set { default_rc_files = value; }
		}

		public static Gtk.PageSetup PageSetup { get; set; }

		// Mono's gtkrc on macOS sets gtk-button-images=0 and a Tango icon
		// theme that is not shipped. GTK then hides Button.Image and paints
		// gtk-missing-image for IconName. Call after Gtk.Application.Init.
		public static void ConfigurePlatform ()
		{
			var settings = Gtk.Settings.Default;
			settings.SetLongProperty ("gtk-button-images", 1, "");
			settings.SetLongProperty ("gtk-menu-images", 1, "");
			var theme = IconTheme;
			theme.RescanIfNeeded ();
		}

		// IconName = "media-playback-start" looks up a Freedesktop name.
		// GTK 2 ships the same glyph as stock gtk-media-play. Alias so
		// remaining IconName call sites (fullscreen slideshow) resolve.
		static void RegisterStockAliases ()
		{
			var factory = new Gtk.IconFactory ();
			AddAlias (factory, "media-playback-start", "gtk-media-play");
			AddAlias (factory, "media-playback-pause", "gtk-media-pause");
			AddAlias (factory, "dialog-error", "gtk-dialog-error");
			factory.AddDefault ();
		}

		static void AddAlias (Gtk.IconFactory factory, string freedesktopName, string stockId)
		{
			var set = Gtk.IconFactory.LookupDefault (stockId);
			if (set != null)
				factory.Add (freedesktopName, set);
		}
	}
}
