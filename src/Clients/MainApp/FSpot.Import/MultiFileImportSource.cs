//
//  MultiFileImportSource.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//   Mike Gemünde <mike@gemuende.de>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2014 Daniel Köb
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
using System.Threading;
using System.Collections.Generic;

using Hyena;

using FSpot.Core;
using FSpot.Utils;
using FSpot.Imaging;

using Gtk;

namespace FSpot.Import
{
	// Multi root version for drag and drop import.
	internal class MultiFileImportSource : FileImportSource
	{
		private IEnumerable<SafeUri> uris;

		public MultiFileImportSource (IEnumerable<SafeUri> uris)
			: base (null, String.Empty, String.Empty)
		{
			this.uris = uris;
		}

		protected override void ScanPhotos (bool recurseSubdirectoris, bool mergeRawAndJpeg)
		{
			foreach (var uri in uris) {
				Log.Debug ("Scanning " + uri);
				ScanPhotoDirectory (recurseSubdirectoris, mergeRawAndJpeg, uri);
			}
			FirePhotoScanFinished ();
		}
	}
}
