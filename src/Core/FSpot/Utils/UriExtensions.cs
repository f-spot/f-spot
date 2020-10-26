//
// UriExtensions.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Mike Gemünde
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace FSpot.Utils
{
	public static class UriExtensions
	{
		public static Uri GetDirectoryUri (this Uri uri)
		{
			var builder = new UriBuilder (uri) {
				Path = $"{Path.GetDirectoryName (uri.AbsolutePath)}/"
			};

			return builder.Uri;
		}

		public static string GetFilename (this Uri uri)
		{
			return Path.GetFileName (uri.AbsolutePath);
		}
	}
}
