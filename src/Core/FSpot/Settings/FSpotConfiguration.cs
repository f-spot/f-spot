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

		//$XDG_CONFIG_HOME/f-spot or $HOME/.config/f-spot
		static readonly string xdg_config_home =
			Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);

		public static string BaseDirectory { get; set; } = Path.Combine (xdg_config_home, "f-spot");

		public static SafeUri PhotoUri { get; set; }

		public static string DatabaseName { get; } = "photos.db";

		public static string SettingsName { get; } = "f-spot-settings.json";
		// path is relative
		public static string HelpDirectory { get; } = "f-spot";
	}
}
