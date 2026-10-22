// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

using FSpot.Filters;

using Hyena;

namespace FSpot.Avalonia.Mail;

// SendEmail's filter chain: resize (unless Original) then copy under a unique name
// into the temporary mail folder.
public static class MailAttachments
{
	public static string CreateTempDirectory ()
	{
		string directory = Path.Combine (Path.GetTempPath (), "f-spot-mail-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (directory);
		TempFiles.RestrictToOwner (directory);
		TempFiles.OwnUntilExit (directory);
		return directory;
	}

	// Thunderbird's compose string treats a quote or a comma in the file name as syntax.
	public static string SafeFileName (string name)
	{
		var buffer = new StringBuilder (name.Length);
		foreach (char c in name) {
			if (c is '\'' or ',' or '\r' or '\n' or '/' or '\\')
				buffer.Append ('_');
			else
				buffer.Append (c);
		}
		string safe = buffer.ToString ();
		if (string.IsNullOrWhiteSpace (safe) || safe is "." or "..")
			return "photo";
		return safe;
	}

	// Returns the attachment's path inside directory; size 0 keeps the original.
	public static string Prepare (string path, int size, string directory)
	{
		var filters = new FilterSet ();
		if (size != 0)
			filters.Add (new SkiaResizeFilter ((uint)size));
		filters.Add (new UniqueNameFilter (new SafeUri (directory)));

		using var request = new FilterRequest (new SafeUri (path));
		filters.Convert (request);
		request.Preserve (request.Current);
		return RenameIfUnsafe (request.Current.LocalPath);
	}

	static string RenameIfUnsafe (string prepared)
	{
		string name = Path.GetFileName (prepared);
		string safe = SafeFileName (name);
		if (safe == name)
			return prepared;
		string? directory = Path.GetDirectoryName (prepared);
		if (string.IsNullOrEmpty (directory))
			return prepared;
		string dest = Path.Combine (directory, safe);
		int i = 1;
		while (File.Exists (dest)) {
			dest = Path.Combine (directory,
				Path.GetFileNameWithoutExtension (safe) + "-" + i++ + Path.GetExtension (safe));
		}
		File.Move (prepared, dest);
		return dest;
	}
}
