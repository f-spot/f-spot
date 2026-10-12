// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

using Hyena;

namespace FSpot.Avalonia;

// gsettings and mailto attach= take a file URI. On Windows, Path.GetFullPath and
// new Uri both root a leading slash on the current drive, so a POSIX path becomes
// file:///D:/...
static class FileUri
{
	public static string FromPath (string path)
	{
		string slash = path.Replace ('\\', '/');
		if (!slash.StartsWith ("/", StringComparison.Ordinal) && !HasDrive (slash))
			slash = Path.GetFullPath (path).Replace ('\\', '/');

		var builder = new StringBuilder ("file://");
		// Strict escape: a space has to be %20 for gsettings. Empty segments are
		// not meaningful in that URI, and a trailing slash is not kept.
		SafeUri.AppendFileSegments (builder, slash, false, false, false, static (segment, _) => Uri.EscapeDataString (segment));
		return builder.ToString ();
	}

	static bool HasDrive (string slash)
		=> slash.Length >= 3
			&& char.IsLetter (slash[0])
			&& slash[1] == ':'
			&& slash[2] == '/';
}
