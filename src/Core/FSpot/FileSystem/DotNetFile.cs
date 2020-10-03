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
using System.IO;
using System.Web;

using Hyena;

namespace FSpot.FileSystem
{
	public class DotNetFile : IFile
	{
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
			=> MimeMapping.GetMimeMapping (uri.AbsolutePath);

		public DateTime GetMTime (SafeUri uri)
			=> File.GetLastWriteTime (uri.AbsolutePath);

		public long GetSize (SafeUri uri)
			=> new FileInfo (uri.AbsolutePath).Length;

		public Stream Read (SafeUri uri)
			=> new FileStream (uri.AbsolutePath, FileMode.Open, FileAccess.Read);
	}
}
