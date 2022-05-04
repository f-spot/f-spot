using System;
using System.IO;

namespace FSpot.Utils
{
	public static class EnvironmentPathExtensions
	{
		const char Unix = ':';
		const char Windows = ';';

		public static char EnvironmentPathSeparator { get; } = Platform.IsWindows ? Windows : Unix;

		public static string FindInEnvironmentPath (this string executable)
		{
			var path = Environment.GetEnvironmentVariable ("PATH");
			var directories = path.Split (EnvironmentPathSeparator);

			foreach (var dir in directories) {
				var fullPath = Path.Combine (dir, executable);
				if (File.Exists (fullPath)) return fullPath;
			}

			return string.Empty;
		}
	}
}
