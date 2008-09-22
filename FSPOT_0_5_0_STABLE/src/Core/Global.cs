/*
 * Global.cs
 *
 * This is free software. See COPYING for details
 *
 */

namespace FSpot {
	public static class Global {
		public static string HomeDirectory {
			get { return System.IO.Path.Combine (System.Environment.GetEnvironmentVariable ("HOME"), System.String.Empty); }
		}
		
		private static string base_dir = System.IO.Path.Combine (HomeDirectory,  System.IO.Path.Combine (".gnome2", "f-spot"));
		public static string BaseDirectory {
			get { return base_dir; }
			set { base_dir = value; }
		}

		private static string photo_directory;
		public static string PhotoDirectory {
			get { return photo_directory; }
			set { photo_directory = value; }
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

		private static Gtk.IconTheme icon_theme = null;
		public static Gtk.IconTheme IconTheme {
			get {
				if (icon_theme == null) {
					icon_theme = Gtk.IconTheme.Default;
					icon_theme.AppendSearchPath (System.IO.Path.Combine (Defines.APP_DATA_DIR, "icons"));
				}
				return icon_theme;
			}
		}

#if GTK_2_12_2
		private static string [] default_rc_files = null;
		public static string [] DefaultRcFiles {
			get {
				if (default_rc_files == null)
					default_rc_files = Gtk.Rc.DefaultFiles;
				return default_rc_files;
			}
			set { default_rc_files = value; }
		}
#endif

#if GTK_2_10
		private static Gtk.PageSetup page_setup = null;
		public static Gtk.PageSetup PageSetup {
			get { return page_setup; }
			set { page_setup = value; }
		}
#endif
	}
}
