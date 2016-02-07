//
// PhotoFileTracker.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
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

		public PhotoFileTracker(IFileSystem fileSystem){
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

		public void CopyIfNeeded (IPhoto item, SafeUri destination)
		{
			var source = item.DefaultVersion.Uri;

			if (source.Equals (destination))
				return;

			// Copy image
			file_system.File.Copy (source, destination, false);
			copied_files.Add (destination);
			original_files.Add (source);
			item.DefaultVersion.Uri = destination;

			// Copy XMP sidecar
			var xmp_original = source.ReplaceExtension(".xmp");
			if (file_system.File.Exists (xmp_original)) {
				var xmp_destination = destination.ReplaceExtension (".xmp");
				file_system.File.Copy (xmp_original, xmp_destination, true);
				copied_files.Add (xmp_destination);
				original_files.Add (xmp_original);
			}
		}
	}
}
