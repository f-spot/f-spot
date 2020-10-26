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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using FSpot.FileSystem;
using FSpot.Imaging;

using Hyena;

namespace FSpot.Import
{
	/// <summary>
	/// Multi root version for drag and drop import.
	/// </summary>
	public class MultiFileImportSource : FileImportSource
	{
		readonly IEnumerable<SafeUri> Uris;

		public MultiFileImportSource (IEnumerable<SafeUri> uris, IImageFileFactory factory, IFileSystem fileSystem) : base (null, factory, fileSystem)
		{
			Uris = uris;
		}

		public override IEnumerable<FileImportInfo> ScanPhotos (ImportPreferences preferences)
		{
			foreach (var uri in Uris) {
				Log.Debug ("Scanning " + uri);
				foreach (var info in ScanPhotoDirectory (preferences, uri)) {
					yield return info;
				}
			}
		}
	}
}
