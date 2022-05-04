//
// DotNetPath.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

namespace FSpot.FileSystem
{
	class DotNetPath : IPath
	{
		public string GetTempPath () => Path.GetTempPath ();
	}
}
