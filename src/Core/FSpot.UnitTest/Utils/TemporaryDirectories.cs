//
// TemporaryDirectories.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace FSpot.UnitTest
{
	// Throwaway directories that are removed even when an assertion fails, which a
	// delete at the end of the test body is not. Lifted out of ProfileCatalogTests when
	// LoggerTests grew a second copy of the same helper.
	sealed class TemporaryDirectories
	{
		readonly List<string> directories = new List<string> ();

		public string Create (string prefix)
		{
			string path = Path.Combine (Path.GetTempPath (), $"f-spot-{prefix}-{Guid.NewGuid ()}");
			Directory.CreateDirectory (path);
			directories.Add (path);

			return path;
		}

		public void DeleteAll ()
		{
			foreach (string directory in directories) {
				MakeDeletable (directory);
				try {
					Directory.Delete (directory, true);
				} catch (DirectoryNotFoundException) {
				}
			}

			directories.Clear ();
		}

		// A test that made a directory unreadable to prove the scan skips it leaves one
		// the delete above cannot enter.
		static void MakeDeletable (string directory)
		{
			if (OperatingSystem.IsWindows () || !Directory.Exists (directory))
				return;

			foreach (string child in Directory.EnumerateDirectories (directory, "*", SearchOption.AllDirectories))
				File.SetUnixFileMode (child, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
		}
	}
}
