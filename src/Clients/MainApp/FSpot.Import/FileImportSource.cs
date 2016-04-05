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

using System;
using System.Collections.Generic;
using System.Threading;
using FSpot.Imaging;
using FSpot.Utils;
using Gtk;
using Hyena;
using Mono.Unix;

namespace FSpot.Import
{
	class FileImportSource : IImportSource
	{
		public string Name { get; set; }

		public string IconName { get; set; }

		public SafeUri Root { get; set; }

		public event EventHandler<PhotoFoundEventArgs> PhotoFoundEvent;
		public event EventHandler<PhotoScanFinishedEventArgs> PhotoScanFinishedEvent;

		public Thread PhotoScanner;
		bool run_photoscanner;

		IImageFileFactory factory;

		public FileImportSource (SafeUri root, string name, string iconName, IImageFileFactory factory)
		{
			this.factory = factory;

			Root = root;
			Name = name;

			if (root != null) {
				if (IsIPodPhoto) {
					IconName = "multimedia-player";
				} else if (IsCamera) {
					IconName = "media-flash";
				} else {
					IconName = iconName;
				}
			}
		}

		public void StartPhotoScan (bool recurseSubdirectories, bool mergeRawAndJpeg)
		{
			if (PhotoScanner != null) {
				run_photoscanner = false;
				PhotoScanner.Join ();
			}

			run_photoscanner = true;
			PhotoScanner = ThreadAssist.Spawn (() => ScanPhotos (recurseSubdirectories, mergeRawAndJpeg));
		}

		protected virtual void ScanPhotos (bool recurseSubdirectories, bool mergeRawAndJpeg)
		{
			ScanPhotoDirectory (recurseSubdirectories, mergeRawAndJpeg, Root);
			FirePhotoScanFinished ();
		}

		protected void ScanPhotoDirectory (bool recurseSubdirectories, bool mergeRawAndJpeg, SafeUri uri)
		{
			var enumerator = (new RecursiveFileEnumerator (uri) {
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

				ThreadAssist.ProxyToMain (() => {
						if (PhotoFoundEvent != null) {
							PhotoFoundEvent.Invoke (this, new PhotoFoundEventArgs { FileImportInfo = info });
						}
					});

				if (!run_photoscanner)
					return;
			}
		}

		SafeUri NextImageFileOrNull(IEnumerator<GLib.File> enumerator)
		{
			SafeUri nextImageFile;
			do {
				if (enumerator.MoveNext ())
					nextImageFile = new SafeUri (enumerator.Current.Uri, true);
				else
					return null;
			} while (!factory.HasLoader (nextImageFile));
			return nextImageFile;
		}

		public void Deactivate ()
		{
			if (PhotoScanner != null) {
				run_photoscanner = false;
				PhotoScanner.Join ();

				// Make sure all photos are added. This is needed to prevent
				// a race condition where a source is deactivated, yet photos
				// are still added to the collection because they are
				// queued on the mainloop.
				while (Application.EventsPending ()) {
					Application.RunIteration (false);
				}

				PhotoScanner = null;
			}
		}

		protected void FirePhotoScanFinished()
		{
			ThreadAssist.ProxyToMain (() => {
				if (PhotoScanFinishedEvent != null) {
					PhotoScanFinishedEvent.Invoke (this, new PhotoScanFinishedEventArgs ());
				}
			});
		}

		bool IsCamera {
			get {
				try {
					var file = GLib.FileFactory.NewForUri (Root.Append ("DCIM"));
					return file.Exists;
				} catch {
					return false;
				}
			}
		}

		bool IsIPodPhoto {
			get {
				try {
					var file = GLib.FileFactory.NewForUri (Root.Append ("Photos"));
					var file2 = GLib.FileFactory.NewForUri (Root.Append ("iPod_Control"));
					return file.Exists && file2.Exists;
				} catch {
					return false;
				}
			}
		}
	}
}
