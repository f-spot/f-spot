// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

namespace FSpot.Avalonia;

// gsettings and mailto attach= take a file URI. On Windows, Path.GetFullPath and
// new Uri both root a leading slash on the current drive, so a POSIX path becomes
// file:///D:/...
static class FileUri
{
	public static string FromPath (string path)
	{
		string normalized = Normalize (path);
		var builder = new StringBuilder ("file://");
		if (HasDrive (normalized)) {
			builder.Append ('/');
			builder.Append (normalized[0]);
			builder.Append (':');
			AppendSegments (builder, normalized.AsSpan (2));
		} else {
			AppendSegments (builder, normalized);
		}

		return builder.ToString ();
	}

	static string Normalize (string path)
	{
		string slash = path.Replace ('\\', '/');
		if (!slash.StartsWith ('/') && !HasDrive (slash))
			slash = Path.GetFullPath (path).Replace ('\\', '/');
		return Collapse (slash);
	}

	static bool HasDrive (string slash)
		=> slash.Length >= 3
			&& char.IsLetter (slash[0])
			&& slash[1] == ':'
			&& slash[2] == '/';

	static string Collapse (string slash)
	{
		bool rooted = slash.StartsWith ('/');
		bool drive = HasDrive (slash);
		string prefix = drive ? slash[..2] : "";
		string body = drive ? slash[2..] : slash;
		var stack = new List<string> ();
		foreach (string segment in body.Split ('/', StringSplitOptions.RemoveEmptyEntries)) {
			if (segment == ".")
				continue;
			if (segment == "..") {
				if (stack.Count > 0)
					stack.RemoveAt (stack.Count - 1);
				continue;
			}

			stack.Add (segment);
		}

		string joined = string.Join ('/', stack);
		if (drive)
			return prefix + "/" + joined;
		return rooted ? "/" + joined : joined;
	}

	static void AppendSegments (StringBuilder builder, ReadOnlySpan<char> path)
	{
		int start = 0;
		while (start < path.Length) {
			if (path[start] == '/') {
				start++;
				continue;
			}

			int relative = path[start..].IndexOf ('/');
			ReadOnlySpan<char> segment = relative < 0 ? path[start..] : path.Slice (start, relative);
			builder.Append ('/');
			builder.Append (Uri.EscapeDataString (segment.ToString ()));
			if (relative < 0)
				break;
			start += relative + 1;
		}
	}
}
