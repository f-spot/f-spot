//
// PhotoFileTracker.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using FSpot.Core;
using FSpot.FileSystem;
using FSpot.Utils;

using Hyena;

namespace FSpot.Import
{
	public class PhotoFileTracker
	{
		readonly IFileSystem file_system;

		readonly List<SafeUri> original_files = new List<SafeUri> ();
		readonly List<SafeUri> copied_files = new List<SafeUri> ();

		public PhotoFileTracker (IFileSystem fileSystem)
		{
			file_system = fileSystem;
		}

		public IEnumerable<SafeUri> OriginalFiles {
			get {
				return original_files;
			}
		}

		public IEnumerable<SafeUri> CopiedFiles {
			get {
				return copied_files;
			}
		}

		public void CopyIfNeeded (IPhoto item, SafeUri destinationBase)
		{
			// remember source uri for copying xmp file
			SafeUri defaultVersionUri = item.DefaultVersion.Uri;

			foreach (IPhotoVersion version in item.Versions) {
				// Copy into photo folder and update IPhotoVersion uri
				var source = version.Uri;
				var destination = destinationBase.Append (source.GetFilename ());
				if (!source.Equals (destination)) {
					destination = GetUniqueFilename (destination);
					file_system.File.Copy (source, destination, false);
					copied_files.Add (destination);
					original_files.Add (source);
					version.Uri = destination;
				}
			}

			// Copy XMP sidecar
			var xmp_original = defaultVersionUri.ReplaceExtension(".xmp");
			if (file_system.File.Exists (xmp_original)) {
				var xmp_destination = item.DefaultVersion.Uri.ReplaceExtension (".xmp");
				file_system.File.Copy (xmp_original, xmp_destination, true);
				copied_files.Add (xmp_destination);
				original_files.Add (xmp_original);
			}
		}

		SafeUri GetUniqueFilename (SafeUri dest)
		{
			// Find an unused name
			int i = 1;
			var base_uri = dest.GetBaseUri ();
			var filename = dest.GetFilenameWithoutExtension ();
			var extension = dest.GetExtension ();
			while (file_system.File.Exists (dest)) {
				dest = base_uri.Append (string.Format ("{0}-{1}{2}", filename, i++, extension));
			}

			return dest;
		}
	}
}
