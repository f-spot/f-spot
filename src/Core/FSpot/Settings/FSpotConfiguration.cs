//
// FSpotConfiguration.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Stephane Delcroix <sdelcroix@novell.com>
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2019 Stephen Shaw
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

		// Set once from CommandLineOptions.DebuggingRequested so Core can log slow
		// queries without depending on how arguments were parsed.
		public static bool Debugging { get; set; }

		public static readonly Version version = Assembly.GetExecutingAssembly ().GetName ().Version;
		public static string Version { get; } = $"{version.Major}.{version.Minor}.{version.Revision}";

		public static string AppDataDir { get; } = Path.Combine (AppContext.BaseDirectory, "share", "f-spot");

		public static string HomeDirectory {
			get => Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
		}

		// Mono mapped ApplicationData to ~/.config. Modern .NET on macOS maps it to
		// ~/Library/Application Support, which would split the catalog from the GTK
		// client. Honor XDG on Unix so both clients open the same photos.db.
		public static string BaseDirectory { get; set; } = DefaultBaseDirectory ();

		// GTK must call this before the first preference read and before Sentry
		// starts, or --basedir still loads the default catalog's settings.
		public static bool TryApplyBaseDirectory (CommandLineOptions options)
		{
			if (options == null || options.BaseDir == null)
				return true;
			if (options.BaseDir.Length == 0)
				return false;

			BaseDirectory = options.BaseDir;
			return true;
		}

		static string DefaultBaseDirectory ()
		{
#if NET
			bool windows = OperatingSystem.IsWindows ();
#else
			bool windows = Environment.OSVersion.Platform == PlatformID.Win32NT;
#endif
			return DefaultBaseDirectory (
				windows,
				Environment.GetEnvironmentVariable ("XDG_CONFIG_HOME"),
				Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData),
				HomeDirectory);
		}

		// XDG_CONFIG_HOME is a Unix convention. On Windows it must not move the catalog.
		internal static string DefaultBaseDirectory (bool windows, string configHome, string windowsAppData, string home)
		{
			string root;
			if (windows)
				root = windowsAppData;
			else if (string.IsNullOrWhiteSpace (configHome))
				root = Path.Combine (home, ".config");
			else
				root = configHome;

			return Path.Combine (root, "f-spot");
		}

		public static SafeUri PhotoUri { get; set; }

		public static string DatabaseName { get; } = "photos.db";

		public static string SettingsName { get; } = "f-spot-settings.json";
		// path is relative
		public static string HelpDirectory { get; } = "f-spot";
	}
}
