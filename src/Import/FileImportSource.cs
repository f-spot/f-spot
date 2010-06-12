using Hyena;
using System;
using System.Threading;
using System.Collections.Generic;
using FSpot.Utils;
using Gtk;

namespace FSpot.Import
{
    internal class FileImportSource : ImportSource {
        public string Name { get; set; }
        public string IconName { get; set; }
        public SafeUri Root { get; set; }

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

        public void StartPhotoScan (ImportController controller)
        {
            if (PhotoScanner != null) {
                run_photoscanner = false;
                PhotoScanner.Join ();
            }

            run_photoscanner = true;
            PhotoScanner = ThreadAssist.Spawn (() => ScanPhotos (controller));
        }

        protected virtual void ScanPhotos (ImportController controller)
        {
            ScanPhotoDirectory (controller, Root);
            ThreadAssist.ProxyToMain (() => controller.PhotoScanFinished ());
        }

        protected void ScanPhotoDirectory (ImportController controller, SafeUri uri)
        {
            var enumerator = new RecursiveFileEnumerator (uri) {
                Recurse = controller.RecurseSubdirectories,
                CatchErrors = true,
                IgnoreSymlinks = true
            };
            var infos = new List<FileImportInfo> ();
            foreach (var file in enumerator) {
                if (ImageFile.HasLoader (new SafeUri (file.Uri, true))) {
                    infos.Add (new FileImportInfo (new SafeUri(file.Uri, true)));
                }

                if (infos.Count % 10 == 0 || infos.Count < 10) {
                    var to_add = infos; // prevents race condition
                    ThreadAssist.ProxyToMain (() => controller.Photos.Add (to_add.ToArray ()));
                    infos = new List<FileImportInfo> ();
                }

                if (!run_photoscanner)
                    return;
            }

            if (infos.Count > 0) {
                var to_add = infos; // prevents race condition
                ThreadAssist.ProxyToMain (() => controller.Photos.Add (to_add.ToArray ()));
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
                while (Application.EventsPending ())
                    Application.RunIteration (false);

                PhotoScanner = null;
            }
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

    // Multi root version for drag and drop import.
    internal class MultiFileImportSource : FileImportSource {
        private IEnumerable<SafeUri> uris;

        public MultiFileImportSource (IEnumerable<SafeUri> uris)
            : base (null, String.Empty, String.Empty)
        {
            this.uris = uris;
        }

        protected override void ScanPhotos (ImportController controller)
        {
            foreach (var uri in uris) {
                Log.Debug ("Scanning "+uri);
                ScanPhotoDirectory (controller, uri);
            }
            ThreadAssist.ProxyToMain (() => controller.PhotoScanFinished ());
        }
    }

    internal class FileImportInfo : IBrowsableItem {
		bool metadata_parsed = false;

        public FileImportInfo (SafeUri original)
        {
            DefaultVersion = new ImportInfoVersion () {
                BaseUri = original.GetBaseUri (),
                Filename = original.GetFilename ()
            };
        }

		private void EnsureMetadataParsed ()
		{
			if (metadata_parsed)
				return;

			using (var img = ImageFile.Create (DefaultVersion.Uri)) {
				time = img.Date;
				description = img.Description;
			}

			metadata_parsed = true;
		}

        public IBrowsableItemVersion DefaultVersion { get; private set; }
        public SafeUri DestinationUri { get; set; }

        private DateTime time;
        public System.DateTime Time {
			get {
				EnsureMetadataParsed ();
				return time;
			}
        }

		private string description;
		public string Description {
			get {
				EnsureMetadataParsed ();
				return description;
			}
		}

        public Tag [] Tags { get { throw new NotImplementedException (); } }
        public string Name { get { throw new NotImplementedException (); } }
        public uint Rating { get { return 0; } }

        internal uint PhotoId { get; set; }
    }
}
