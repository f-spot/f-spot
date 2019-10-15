//
// FileImportSource.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2014 Daniel Köb
// Copyright (C) 2010 Novell, Inc.
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

using System.Collections.Generic;
using FSpot.FileSystem;
using FSpot.Imaging;
using Hyena;
using Mono.Unix;

namespace FSpot.Import
{
	public class FileImportSource : IImportSource
	{
		#region fields

		readonly SafeUri root;
		readonly IFileSystem fileSystem;
		readonly IImageFileFactory factory;

		#endregion

		#region ctors

		public FileImportSource (SafeUri root, IImageFileFactory factory, IFileSystem fileSystem)
		{
			this.root = root;
			this.fileSystem = fileSystem;
			this.factory = factory;
		}

		#endregion

		#region IImportSource

		public virtual IEnumerable<FileImportInfo> ScanPhotos (bool recurseSubdirectories, bool mergeRawAndJpeg)
		{
			return ScanPhotoDirectory (recurseSubdirectories, mergeRawAndJpeg, root);
		}

		#endregion

		#region private

		protected IEnumerable<FileImportInfo> ScanPhotoDirectory (bool recurseSubdirectories, bool mergeRawAndJpeg, SafeUri uri)
		{
			var enumerator = (new RecursiveFileEnumerator (uri, fileSystem) {
				Recurse = recurseSubdirectories,
				CatchErrors = true,
				IgnoreSymlinks = true
			}).GetEnumerator ();

			SafeUri file = null;

			while (true) {
				if (file == null) {
					file = NextImageFileOrNull(enumerator);
					if (file == null)
						break;
				}

				// peek the next file to see if we have a RAW+JPEG combination
				// skip any non-image files
				SafeUri nextFile = NextImageFileOrNull(enumerator);

				SafeUri original;
				SafeUri version = null;
				if (mergeRawAndJpeg && nextFile != null && factory.IsJpegRawPair (file, nextFile)) {
					// RAW+JPEG: import as one photo with versions
					original = factory.IsRaw (file) ? file : nextFile;
					version = factory.IsRaw (file) ? nextFile : file;
					// current and next files consumed in this iteration,
					// prepare to get next file on next iteration
					file = null;
				} else {
					// import current file as single photo
					original = file;
					// forward peeked file to next iteration of loop
					file = nextFile;
				}

				FileImportInfo info;
				if (version == null) {
					info  = new FileImportInfo (original, Catalog.GetString ("Original"));
				} else {
					info  = new FileImportInfo (original, Catalog.GetString ("Original RAW"));
					info.AddVersion (version, Catalog.GetString ("Original JPEG"));
				}

				yield return info;
			}
		}

		SafeUri NextImageFileOrNull(IEnumerator<SafeUri> enumerator)
		{
			SafeUri nextImageFile;
			do {
				if (enumerator.MoveNext ())
					nextImageFile = enumerator.Current;
				else
					return null;
			} while (!factory.HasLoader (nextImageFile));
			return nextImageFile;
		}

		#endregion
	}
}
