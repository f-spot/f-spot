// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia;

// Mail folders and print PDFs live in the temp directory. A mail folder is
// readable by its owner only. It is removed when this process exits, not when
// the next message is prepared, so a composer left open keeps its attachments.
// A print PDF from an earlier run is removed once it is a day old. The Windows
// print verb still needs the file after Process.Start returns, so that copy is
// not deleted when the print window closes.
static class TempFiles
{
	public static readonly TimeSpan StaleAfter = TimeSpan.FromDays (1);
	static readonly object gate = new ();
	static readonly List<string> owned = new ();
	static bool exitHooked;

	public static void OwnUntilExit (string directory)
	{
		lock (gate) {
			owned.Add (directory);
			if (exitHooked)
				return;
			exitHooked = true;
			AppDomain.CurrentDomain.ProcessExit += (_, _) => DeleteOwned ();
		}
	}

	internal static void DeleteOwned ()
	{
		string[] copy;
		lock (gate) {
			copy = owned.ToArray ();
			owned.Clear ();
		}
		foreach (string directory in copy) {
			try {
				if (Directory.Exists (directory))
					Directory.Delete (directory, true);
			} catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
			}
		}
	}

	public static void RestrictToOwner (string directory)
	{
		if (OperatingSystem.IsWindows ())
			return;
		File.SetUnixFileMode (directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
	}

	public static void DeleteOlderThan (string root, string prefix, TimeSpan age)
	{
		if (!Directory.Exists (root))
			return;
		DateTime cutoff = DateTime.UtcNow - age;
		foreach (string entry in Directory.EnumerateFileSystemEntries (root, prefix + "*")) {
			DateTime written = Directory.Exists (entry)
				? Directory.GetLastWriteTimeUtc (entry)
				: File.GetLastWriteTimeUtc (entry);
			if (written >= cutoff)
				continue;
			try {
				if (Directory.Exists (entry))
					Directory.Delete (entry, true);
				else
					File.Delete (entry);
			} catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
			}
		}
	}
}
