//
// RecursiveFileEnumerator.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//   Mike Gemünde <mike@gemuende.de>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2016 Daniel Köb
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

using Hyena;

namespace FSpot.FileSystem
{
	public class RecursiveFileEnumerator : IEnumerable<SafeUri>
	{
		readonly SafeUri root;
		readonly IFileSystem fileSystem;

		public bool Recurse { get; set; }
		public bool CatchErrors { get; set; }
		public bool IgnoreSymlinks { get; set; }

		public RecursiveFileEnumerator (SafeUri root, IFileSystem fileSystem)
		{
			this.root = root;
			this.fileSystem = fileSystem;

			Recurse = true;
			CatchErrors = false;
			IgnoreSymlinks = false;
		}

		IEnumerable<SafeUri> ScanForFiles (SafeUri rootPath, bool firstLevel)
		{
			bool isSymlink;
			// TODO: this try catch was ported from the old glib implementation
			// could be removed when exceptions are handle up the call stack
			try {
				isSymlink = fileSystem.File.IsSymlink (rootPath);
			} catch (Exception) {
				if (!CatchErrors)
					throw;
				yield break;
			}

			if (isSymlink && IgnoreSymlinks)
				yield break;

			if (fileSystem.File.Exists (rootPath)) {
				yield return rootPath;
			} else if (fileSystem.Directory.Exists (rootPath)) {
				if (!firstLevel && !Recurse)
					yield break;

				var sortedFileEnumerator = new SortedFileEnumerator (fileSystem.Directory.Enumerate (rootPath), fileSystem);
				foreach (var child in sortedFileEnumerator) {
					foreach (var file in ScanForFiles (child, false)) {
						yield return file;
					}
				}
			}
		}

		public IEnumerator<SafeUri> GetEnumerator ()
		{
			return ScanForFiles (root, true).GetEnumerator ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}
	}
}
