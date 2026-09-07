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
	}
}
