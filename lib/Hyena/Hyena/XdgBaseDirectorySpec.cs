//
// XdgBaseDirectorySpec.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Hyena
{
	public static class XdgBaseDirectorySpec
	{
		public static string GetUserDirectory (string key, string fallback)
		{
			string home_dir = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
			string config_dir = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);

			string env_path = Environment.GetEnvironmentVariable (key);
			if (!string.IsNullOrEmpty (env_path)) {
				return env_path;
			}

			string user_dirs_path = Path.Combine (config_dir, "user-dirs.dirs");

			if (!File.Exists (user_dirs_path)) {
				return Path.Combine (home_dir, fallback);
			}

			try {
				using (var reader = new StreamReader (user_dirs_path)) {
					string line;
					while ((line = reader.ReadLine ()) != null) {
						line = line.Trim ();
						int delim_index = line.IndexOf ('=');
						if (delim_index > 8 && line.Substring (0, delim_index) == key) {
							string path = line.Substring (delim_index + 1).Trim ('"');
							bool relative = false;

							if (path.StartsWith ("$HOME/")) {
								relative = true;
								path = path.Substring (6);
							} else if (path.StartsWith ("~")) {
								relative = true;
								path = path.Substring (1);
							} else if (!path.StartsWith ("/")) {
								relative = true;
							}

							return relative ? Path.Combine (home_dir, path) : path;
						}
					}
				}
			} catch (FileNotFoundException) {
			}

			return Path.Combine (home_dir, fallback);
		}

		public static string GetXdgDirectoryUnderHome (string key, string fallback)
		{
			string xdg_dir = XdgBaseDirectorySpec.GetUserDirectory (key, fallback);
			string home_dir = Environment.GetFolderPath (Environment.SpecialFolder.Personal);

			if (xdg_dir == null || xdg_dir == home_dir || !xdg_dir.StartsWith (home_dir)) {
				xdg_dir = Path.Combine (home_dir, fallback);
			}

			return xdg_dir;
		}
	}
}
