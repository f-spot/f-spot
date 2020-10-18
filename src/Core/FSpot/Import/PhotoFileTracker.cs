//
// PhotoFileTracker.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Core;
using FSpot.FileSystem;
using FSpot.Utils;

using Hyena;

namespace FSpot.Import
{
	public class PhotoFileTracker
	{
		readonly IFileSystem fileSystem;

		public List<SafeUri> OriginalFiles { get; } = new List<SafeUri> ();

		public List<SafeUri> CopiedFiles { get; } = new List<SafeUri> ();


		public PhotoFileTracker (IFileSystem fileSystem)
		{
			this.fileSystem = fileSystem;
		}

		public void CopyIfNeeded (IPhoto item, SafeUri destinationBase)
		{
			if (item == null)
				throw new ArgumentNullException (nameof (item));

			// remember source uri for copying xmp file
			SafeUri defaultVersionUri = item.DefaultVersion.Uri;

			foreach (IPhotoVersion version in item.Versions) {
				// Copy into photo folder and update IPhotoVersion uri
				var source = version.Uri;
				var destination = destinationBase.Append (source.GetFilename ());
				if (!source.Equals (destination)) {
					destination = GetUniqueFilename (destination);
					fileSystem.File.Copy (source, destination, false);
					CopiedFiles.Add (destination);
					OriginalFiles.Add (source);
					version.Uri = destination;
				}
			}

			// Copy XMP sidecar
			var xmpOriginal = defaultVersionUri.ReplaceExtension (".xmp");
			if (fileSystem.File.Exists (xmpOriginal)) {
				var xmpDestination = item.DefaultVersion.Uri.ReplaceExtension (".xmp");
				fileSystem.File.Copy (xmpOriginal, xmpDestination, true);
				CopiedFiles.Add (xmpDestination);
				OriginalFiles.Add (xmpOriginal);
			}
		}

		SafeUri GetUniqueFilename (SafeUri dest)
		{
			// Find an unused name
			int i = 1;
			var baseUri = dest.GetBaseUri ();
			var filename = dest.GetFilenameWithoutExtension ();
			var extension = dest.GetExtension ();

			while (fileSystem.File.Exists (dest)) {
				dest = baseUri.Append ($"{filename}-{i++}{extension}");
			}

			return dest;
		}
	}
}
