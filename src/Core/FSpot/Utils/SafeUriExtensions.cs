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

using System;

using Hyena;

namespace FSpot.Utils
{
	public static class SafeUriExtensions
	{
		public static SafeUri Append (this SafeUri base_uri, string filename)
		{
			return new SafeUri (base_uri.AbsoluteUri + (base_uri.AbsoluteUri.EndsWith ("/") ? "" : "/") + filename, true);
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
			var abs_uri = uri.AbsoluteUri;
			var index = abs_uri.LastIndexOf ('.');
			return index == -1 ? string.Empty : abs_uri.Substring (index);
		}

		public static string GetFilenameWithoutExtension (this SafeUri uri)
		{
			var name = uri.GetFilename ();
			var index = name.LastIndexOf ('.');
			return index > -1 ? name.Substring (0, index) : name;
		}

		public static SafeUri ReplaceExtension (this SafeUri uri, string extension)
		{
			return uri.GetBaseUri ().Append (uri.GetFilenameWithoutExtension () + extension);
		}
	}
}
