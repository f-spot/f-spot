//
// IFileSystem.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.FileSystem
{
	public interface IFileSystem
	{
		IFile File { get; }
		IDirectory Directory { get; }
		IPath Path { get; }
	}
}
