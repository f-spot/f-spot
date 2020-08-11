//
// SortedFileEnumerator.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Hyena;

namespace FSpot.FileSystem
{
	class SortedFileEnumerator : IEnumerable<SafeUri>
	{
		readonly List<SafeUri> files;

		public SortedFileEnumerator (IEnumerable<SafeUri> baseEnumerable, IFileSystem fileSystem)
		{
			files = baseEnumerable.ToList ();

			files.Sort ((x, y) => {
				if (fileSystem.Directory.Exists (x)) {
					if (fileSystem.File.Exists (y)) {
						return -1;
					}
					return string.Compare (x.LocalPath, y.LocalPath, StringComparison.Ordinal);
				}

				if (fileSystem.Directory.Exists (y)) {
					return 1;
				}

				return string.Compare (x.LocalPath, y.LocalPath, StringComparison.Ordinal);
			});
		}

		public IEnumerator<SafeUri> GetEnumerator ()
		{
			foreach (var file in files) {
				yield return file;
			}
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return files.GetEnumerator ();
		}
	}
}
