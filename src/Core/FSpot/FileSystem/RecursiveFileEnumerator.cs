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
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;
using Hyena;

namespace FSpot.FileSystem
{
	public class RecursiveFileEnumerator : IEnumerable<SafeUri>
	{
		SafeUri root;
		IFileSystem fileSystem;

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
			} catch (Exception e) {
				if (!CatchErrors)
					throw e;
				yield break;
			}

			if (isSymlink && IgnoreSymlinks) {
				yield break;
			}
			if (fileSystem.File.Exists (rootPath)) {
				yield return rootPath;
			} else if (fileSystem.Directory.Exists (rootPath)) {
				if (!firstLevel && ! Recurse) {
					yield break;
				}
				foreach (var child in new SortedFileEnumerator (fileSystem.Directory.Enumerate (rootPath), fileSystem)) {
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
