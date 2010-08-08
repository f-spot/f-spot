/*
 * Global.cs
 *
 * This is free software. See COPYING for details
 *
 */
using System;
using Hyena;

namespace FSpot.Core {
	public static class Global {
		public static string HomeDirectory {
			get { return System.IO.Path.Combine (System.Environment.GetEnvironmentVariable ("HOME"), System.String.Empty); }
		}

		//$XDG_CONFIG_HOME/f-spot or $HOME/.config/f-spot
		private static string xdg_config_home = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
		private static string base_dir = System.IO.Path.Combine (xdg_config_home, "f-spot");
		public static string BaseDirectory {
			get { return base_dir; }
			set { base_dir = value; }
		}

		private static SafeUri photo_uri;
		public static SafeUri PhotoUri {
			get { return photo_uri; }
			set { photo_uri = value; }
		}

		public static string HelpDirectory {
			get {
				// path is relative
				return "f-spot";
			}
		}

		private static Cms.Profile display_profile;
		public static Cms.Profile DisplayProfile {
			set { display_profile = value; }
			get { return display_profile; }
		}

		private static Cms.Profile destination_profile;
		public static Cms.Profile DestinationProfile {
			set { destination_profile = value; }
			get { return destination_profile; }
		}

		private static Gtk.IconTheme icon_theme;
		public static Gtk.IconTheme IconTheme {
			get {
				if (icon_theme == null) {
					icon_theme = Gtk.IconTheme.Default;
					icon_theme.AppendSearchPath (System.IO.Path.Combine (Defines.APP_DATA_DIR, "icons"));
				}
				return icon_theme;
			}
		}

		private static string [] default_rc_files;
		public static string [] DefaultRcFiles {
			get {
				if (default_rc_files == null)
					default_rc_files = Gtk.Rc.DefaultFiles;
				return default_rc_files;
			}
			set { default_rc_files = value; }
		}

		private static Gtk.PageSetup page_setup;
		public static Gtk.PageSetup PageSetup {
			get { return page_setup; }
			set { page_setup = value; }
		}
	}
}
