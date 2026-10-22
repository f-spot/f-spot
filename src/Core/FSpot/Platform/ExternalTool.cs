// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace FSpot.Platform
{
	// A process is started from the absolute path found on PATH. A bare name is
	// resolved again at start, so a later change to PATH or the working directory
	// cannot select a different file. "." and empty PATH entries are skipped.
	public static class ExternalTool
	{
		public static Func<string, IEnumerable<string>> SearchDirectories = DefaultSearchDirectories;

		public static string Resolve (string name)
		{
			if (name == null)
				throw new ArgumentNullException (nameof (name));
			if (Path.IsPathRooted (name))
				return name;

			foreach (string directory in SearchDirectories (name)) {
				if (string.IsNullOrEmpty (directory) || directory == ".")
					continue;
				string candidate = Path.Combine (directory, ExecutableFileName (name));
				if (File.Exists (candidate))
					return Path.GetFullPath (candidate);
			}

			throw new FileNotFoundException ($"Could not find {name}.", name);
		}

		static IEnumerable<string> DefaultSearchDirectories (string name)
			=> (Environment.GetEnvironmentVariable ("PATH") ?? string.Empty).Split (Path.PathSeparator);

		static string ExecutableFileName (string name)
			=> FileNameFor (name, OperatingSystem.IsWindows ());

		// Windows CreateProcess searches PATHEXT. The tools F-Spot starts are .exe.
		// A .bat or .cmd name is kept so a shim with that extension is still found.
		internal static string FileNameFor (string name, bool windows)
		{
			if (windows
				&& !name.EndsWith (".exe", StringComparison.OrdinalIgnoreCase)
				&& !name.EndsWith (".bat", StringComparison.OrdinalIgnoreCase)
				&& !name.EndsWith (".cmd", StringComparison.OrdinalIgnoreCase))
				return name + ".exe";
			return name;
		}
	}
}
