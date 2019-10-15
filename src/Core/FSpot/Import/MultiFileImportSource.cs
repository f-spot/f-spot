//
//  MultiFileImportSource.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Ruben Vermeersch <ruben@savanne.be>
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2014 Daniel Köb
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

using System.Collections.Generic;
using FSpot.FileSystem;
using FSpot.Imaging;
using Hyena;

namespace FSpot.Import
{
	// Multi root version for drag and drop import.
	public class MultiFileImportSource : FileImportSource
	{
		readonly IEnumerable<SafeUri> uris;

		public MultiFileImportSource (IEnumerable<SafeUri> uris, IImageFileFactory factory, IFileSystem fileSystem)
			: base (null, factory, fileSystem)
		{
			this.uris = uris;
		}

		public override IEnumerable<FileImportInfo> ScanPhotos (bool recurseSubdirectories, bool mergeRawAndJpeg)
		{
			foreach (var uri in uris) {
				Log.Debug ("Scanning " + uri);
				foreach (var info in ScanPhotoDirectory (recurseSubdirectories, mergeRawAndJpeg, uri)) {
					yield return info;
				}
			}
		}
	}
}
