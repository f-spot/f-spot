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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.FileSystem;
using FSpot.Imaging;

using Hyena;

using Mono.Unix;

namespace FSpot.Import
{
	public class FileImportSource : IImportSource
	{
		readonly SafeUri root;
		readonly IFileSystem fileSystem;
		readonly IImageFileFactory factory;

		public FileImportSource (SafeUri root, IImageFileFactory factory, IFileSystem fileSystem)
		{
			this.root = root;
			this.fileSystem = fileSystem;
			this.factory = factory;
		}

		public virtual IEnumerable<FileImportInfo> ScanPhotos (ImportPreferences preferences)
		{
			return ScanPhotoDirectory (preferences, root);
		}

		protected IEnumerable<FileImportInfo> ScanPhotoDirectory (ImportPreferences preferences, SafeUri uri)
		{
			if (preferences == null)
				throw new ArgumentNullException (nameof (preferences));

			var enumerator = new RecursiveFileEnumerator (uri, fileSystem) {
				Recurse = preferences.RecurseSubdirectories,
				CatchErrors = true,
				IgnoreSymlinks = true
			}.GetEnumerator ();

			SafeUri file = null;

			while (true) {
				if (file == null) {
					file = NextImageFileOrNull (enumerator);
					if (file == null)
						break;
				}

				// peek the next file to see if we have a RAW+JPEG combination
				// skip any non-image files
				SafeUri nextFile = NextImageFileOrNull (enumerator);

				SafeUri original;
				SafeUri version = null;
				if (preferences.MergeRawAndJpeg && nextFile != null && factory.IsJpegRawPair (file, nextFile)) {
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
					info = new FileImportInfo (original, Catalog.GetString ("Original"));
				} else {
					info = new FileImportInfo (original, Catalog.GetString ("Original RAW"));
					info.AddVersion (version, Catalog.GetString ("Original JPEG"));
				}

				yield return info;
			}
		}

		SafeUri NextImageFileOrNull (IEnumerator<SafeUri> enumerator)
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
	}
}
