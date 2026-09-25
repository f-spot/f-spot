// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
		return directory;
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
		return request.Current.LocalPath;
	}
}
