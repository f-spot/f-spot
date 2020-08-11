//
// DotNetDirectory.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2019 Stephen Shaw
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

using FSpot.Utils;

using Hyena;

namespace FSpot.FileSystem
{
	class DotNetDirectory : IDirectory
	{
		public bool Exists (SafeUri uri)
		{
			return Directory.Exists (uri.AbsolutePath);
		}

		public void CreateDirectory (SafeUri uri)
		{
			Directory.CreateDirectory (uri.AbsolutePath);
		}

		public void Delete (SafeUri uri)
		{
			if (!Exists (uri)) {
				//FIXME to be consistent with System.IO.Directory.Delete we should throw an exception in this case
				return;
			}
			Directory.Delete (uri.AbsolutePath);
		}

		public IEnumerable<SafeUri> Enumerate (SafeUri uri)
		{
			if (!Exists (uri))
				yield break;

			foreach (var file in Directory.EnumerateFileSystemEntries (uri.AbsolutePath))
				yield return new SafeUri (file);
		}
	}
}
