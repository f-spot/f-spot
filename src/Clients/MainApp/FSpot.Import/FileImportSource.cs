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
using System.Threading;
using System.Collections.Generic;

using Hyena;

using FSpot.Core;
using FSpot.Utils;
using FSpot.Imaging;

using Gtk;

namespace FSpot.Import
{
	internal class FileImportSource : IImportSource
	{
		public string Name { get; set; }

		public string IconName { get; set; }

		public SafeUri Root { get; set; }

		public event EventHandler<PhotoFoundEventArgs> PhotoFoundEvent;
		public event EventHandler<PhotoScanFinishedEventArgs> PhotoScanFinishedEvent;

		public Thread PhotoScanner;
		bool run_photoscanner = false;

		public FileImportSource (SafeUri root, string name, string icon_name)
		{
			Root = root;
			Name = name;

			if (root != null) {
				if (IsIPodPhoto) {
					IconName = "multimedia-player";
				} else if (IsCamera) {
					IconName = "media-flash";
				} else {
					IconName = icon_name;
				}
			}
		}

		public void StartPhotoScan (bool recurseSubdirectories)
		{
			if (PhotoScanner != null) {
				run_photoscanner = false;
				PhotoScanner.Join ();
			}

			run_photoscanner = true;
			PhotoScanner = ThreadAssist.Spawn (() => ScanPhotos (recurseSubdirectories));
		}

		protected virtual void ScanPhotos (bool recurseSubdirectories)
		{
			ScanPhotoDirectory (recurseSubdirectories, Root);
			FirePhotoScanFinished ();
		}

		protected void ScanPhotoDirectory (bool recurseSubdirectories, SafeUri uri)
		{
			var enumerator = new RecursiveFileEnumerator (uri) {
						Recurse = recurseSubdirectories,
						CatchErrors = true,
						IgnoreSymlinks = true
			};
			foreach (var file in enumerator) {
				if (ImageFile.HasLoader (new SafeUri (file.Uri.ToString(), true))) {
					var info = new FileImportInfo (new SafeUri (file.Uri.ToString (), true));
					ThreadAssist.ProxyToMain (() => {
						if (PhotoFoundEvent != null) {
							PhotoFoundEvent.Invoke (this, new PhotoFoundEventArgs { FileImportInfo = info });
						}
					});
				}
				if (!run_photoscanner)
					return;
			}
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

		private bool IsCamera {
			get {
				try {
					var file = GLib.FileFactory.NewForUri (Root.Append ("DCIM"));
					return file.Exists;
				} catch {
					return false;
				}
			}
		}

		private bool IsIPodPhoto {
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
