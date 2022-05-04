//
// DotNetFileSystem.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2016 Daniel Köb
// Copyright (C) 2019 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.FileSystem
{
	class DotNetFileSystem : IFileSystem
	{
		DotNetFile file;
		DotNetDirectory directory;
		DotNetPath path;

		public IFile File {
			get => file ??= new DotNetFile ();
		}

		public IDirectory Directory {
			get => directory ??= new DotNetDirectory ();
		}

		public IPath Path {
			get => path ??= new DotNetPath ();
		}
	}
}
