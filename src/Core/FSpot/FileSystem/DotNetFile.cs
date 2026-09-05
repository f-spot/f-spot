//
// DotNetFile.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2016 Daniel Köb
// Copyright (C) 2019 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Hyena;

namespace FSpot.FileSystem
{
	public class DotNetFile : IFile
	{
		// System.Web.MimeMapping is not available on net10 testhosts.
		static readonly Dictionary<string, string> MimeTypesByExtension = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase) {
			{ ".gif", "image/gif" },
			{ ".pcx", "image/x-pcx" },
			{ ".pnm", "image/x-portable-anymap" },
			{ ".pbm", "image/x-portable-bitmap" },
			{ ".pgm", "image/x-portable-graymap" },
			{ ".ppm", "image/x-portable-pixmap" },
			{ ".bmp", "image/bmp" },
			{ ".png", "image/png" },
			{ ".tif", "image/tiff" },
			{ ".tiff", "image/tiff" },
			{ ".svg", "image/svg+xml" },
			{ ".svgz", "image/svg+xml" },
			{ ".jfi", "image/jpeg" },
			{ ".jfif", "image/jpeg" },
			{ ".jif", "image/jpeg" },
			{ ".jpe", "image/jpeg" },
			{ ".jpeg", "image/jpeg" },
			{ ".jpg", "image/jpeg" },
			{ ".arw", "image/x-sony-arw" },
			{ ".nef", "image/x-nikon-nef" },
			{ ".pef", "image/x-pentax-pef" },
			{ ".raw", "image/x-panasonic-raw" },
			{ ".orf", "image/x-orf" },
			{ ".kdc", "image/x-kodak-kdc" },
			{ ".srf", "image/x-sony-srf" },
			{ ".cr2", "image/x-canon-cr2" },
			{ ".dng", "image/x-adobe-dng" },
			{ ".rw2", "image/x-raw" },
			{ ".mrw", "image/x-mrw" },
			{ ".x3f", "image/x-x3f" },
			{ ".srw", "image/x-samsung-srw" },
			{ ".crw", "image/x-ciff" },
			{ ".raf", "image/x-raf" },
			{ ".xcf", "image/x-xcf" },
			{ ".xmp", "application/rdf+xml" },
			{ ".xml", "text/xml" },
			{ ".rss", "application/rss+xml" },
			{ ".txt", "text/plain" },
		};

		public bool Exists (SafeUri uri)
			=> File.Exists (uri.AbsolutePath);

		public bool IsSymlink (SafeUri uri)
		{
			// FIXME, this might return a false positive
			var pathInfo = new FileInfo (uri.AbsolutePath);
			return pathInfo.Attributes.HasFlag (FileAttributes.ReparsePoint);
		}

		public void Copy (SafeUri source, SafeUri destination, bool overwrite)
			=> File.Copy (source.AbsolutePath, destination.AbsolutePath, overwrite);

		public void Delete (SafeUri uri)
			=> File.Delete (uri.AbsolutePath);

		public string GetMimeType (SafeUri uri)
		{
			var extension = Path.GetExtension (uri.AbsolutePath);
			if (!string.IsNullOrEmpty (extension) && MimeTypesByExtension.TryGetValue (extension, out var mime))
				return mime;
			return "application/octet-stream";
		}

		public DateTime GetMTime (SafeUri uri)
			=> File.GetLastWriteTime (uri.AbsolutePath);

		public long GetSize (SafeUri uri)
			=> new FileInfo (uri.AbsolutePath).Length;

		public Stream Read (SafeUri uri)
			=> new FileStream (uri.AbsolutePath, FileMode.Open, FileAccess.Read);
	}
}
