// Copyright (C) 2019-2022 Stephen Shaw
// Copyright (C) 2004-2010 Novell, Inc.
// Copyright (C) 2007-2008 Stephane Delcroix
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2004-2007 Larry Ewing
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;

using Hyena;

namespace FSpot.Settings
{
	public static class FSpotConfiguration
	{
		public static string Package { get; } = "f-spot";

		static readonly Version version = Assembly.GetExecutingAssembly ().GetName ().Version;
		public static string Version { get; } = $"{version.Major}.{version.Minor}.{version.Revision}";

		// FIXME, for now we are going to hard code these
		//			I think this stuff will be "installed"
		//			next to the assembly in the future instead
		public const string DatabaseName = "photos_ef.db";

		public static string SettingsName { get; } = "f-spot-settings.json";

		public static string HomeDirectory {
			get => Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
		}

		//$XDG_CONFIG_HOME/f-spot or $HOME/.config/f-spot
		static readonly string ConfigHome =
			Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);

		public static string BaseDirectory { get; set; } = Path.Combine (ConfigHome, "f-spot");


		// FIXME, for now we are going to hard code these
		//			I think this stuff will be "installed"
		//			next to the assembly in the future instead
		public static string LocaleDir { get; } = "/usr/local/share/locale";
		public static string AppDataDir { get; } = "/usr/local/share/f-spot";

		public static SafeUri PhotoUri { get; set; }

		// path is relative
		public static string HelpDirectory { get; } = "f-spot";

		public static Cms.Profile DisplayProfile { get; set; }

		public static Cms.Profile DestinationProfile { get; set; }

		static Gtk.IconTheme icon_theme;
		public static Gtk.IconTheme IconTheme {
			get {
				if (icon_theme == null) {
					icon_theme = Gtk.IconTheme.Default;
					icon_theme.AppendSearchPath (Path.Combine (AppDataDir, "icons"));
				}
				return icon_theme;
			}
		}

		static string[] default_rc_files;
		public static string[] DefaultRcFiles {
			get {
				return default_rc_files ?? Gtk.Rc.DefaultFiles;
			}
			set { default_rc_files = value; }
		}

		public static Gtk.PageSetup PageSetup { get; set; }
	}
}
