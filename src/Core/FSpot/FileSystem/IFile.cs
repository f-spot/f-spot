//
// IFile.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Hyena;

namespace FSpot.FileSystem
{
	public interface IFile
	{
		bool Exists (SafeUri uri);
		bool IsSymlink (SafeUri uri);
		void Copy (SafeUri source, SafeUri destination, bool overwrite);
		void Delete (SafeUri uri);
		string GetMimeType (SafeUri uri);
		DateTime GetMTime (SafeUri uri);
		long GetSize (SafeUri uri);
		Stream Read (SafeUri uri);
	}
}
