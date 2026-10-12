//
// SafeUriExtensions.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Hyena;

namespace FSpot.Utils
{
	public static class SafeUriExtensions
	{
		public static SafeUri Append (this SafeUri base_uri, string filename)
		{
			return new SafeUri (base_uri.AbsoluteUri + (base_uri.AbsoluteUri.EndsWith ("/") ? "" : "/") + filename, true);
		}

		// filename is a new file on disk. Append keeps catalog text as stored.
		public static SafeUri AppendFileName (this SafeUri base_uri, string filename)
		{
			return base_uri.Append (SafeUri.EscapeFileName (filename));
		}

		public static SafeUri GetBaseUri (this SafeUri uri)
		{
			var abs_uri = uri.AbsoluteUri;
			return new SafeUri (abs_uri.Substring (0, abs_uri.LastIndexOf ('/')), true);
		}

		public static string GetFilename (this SafeUri uri)
		{
			var abs_uri = uri.AbsoluteUri;
			return abs_uri.Substring (abs_uri.LastIndexOf ('/') + 1);
		}

		public static string GetExtension (this SafeUri uri)
		{
			string name = uri.GetFilename ();
			int index = name.LastIndexOf ('.');
			// A dot in a parent folder is not this file's type. A leading or
			// trailing dot is not an extension either.
			if (index <= 0 || index == name.Length - 1)
				return string.Empty;

			return name.Substring (index);
		}

		public static string GetFilenameWithoutExtension (this SafeUri uri)
		{
			var name = uri.GetFilename ();
			var index = name.LastIndexOf ('.');
			// Same rule as GetExtension: a leading or trailing dot is not an extension.
			if (index <= 0 || index == name.Length - 1)
				return name;

			return name.Substring (0, index);
		}

		public static SafeUri ReplaceExtension (this SafeUri uri, string extension)
		{
			return uri.GetBaseUri ().Append (uri.GetFilenameWithoutExtension () + extension);
		}
	}
}
