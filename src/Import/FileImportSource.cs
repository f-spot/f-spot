using Hyena;
using System;
using System.Threading;
using System.Collections.Generic;
using FSpot.Utils;

namespace FSpot.Import
{
    internal class FileImportSource : ImportSource {
        public string Name { get; set; }
        public string IconName { get; set; }
        public SafeUri Root { get; set; }

        public Thread PhotoScanner;

        public FileImportSource (SafeUri root, string name, string icon_name)
        {
            Log.Debug ("Added source "+Name);
            Root = root;
            Name = name;

            if (IsIPodPhoto) {
                IconName = "multimedia-player";
            } else if (IsCamera) {
                IconName = "media-flash";
            } else {
                IconName = icon_name;
            }
        }

        public void StartPhotoScan (ImportController controller)
        {
            if (PhotoScanner != null)
                PhotoScanner.Abort ();

            PhotoScanner = ThreadAssist.Spawn (() => ScanPhotos (controller));
        }

        void ScanPhotos (ImportController controller)
        {
            var infos = new List<FileImportInfo> ();
			var enumerator = new RecursiveFileEnumerator (Root, controller.RecurseSubdirectories, true);
			foreach (var file in enumerator) {
				if (ImageFile.HasLoader (new SafeUri (file.Uri, true))) {
					infos.Add (new FileImportInfo (new SafeUri(file.Uri, true)));
                }

                if (infos.Count % 10 == 0) {
                    var to_add = infos; // prevents race condition
                    ThreadAssist.ProxyToMain (() => controller.Photos.Add (to_add.ToArray ()));
                    infos = new List<FileImportInfo> ();
                }
			}

            if (infos.Count > 0) {
                ThreadAssist.ProxyToMain (() => controller.Photos.Add (infos.ToArray ()));
            }

            controller.PhotoScanFinished ();
        }

        public void Deactivate ()
        {
            if (PhotoScanner != null) {
                PhotoScanner.Abort (); // FIXME: abort is not nice
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

	internal class FileImportInfo : IBrowsableItem {
		public FileImportInfo (SafeUri original)
		{
			DefaultVersion = new ImportInfoVersion () {
				BaseUri = original.GetBaseUri (),
				Filename = original.GetFilename ()
			};

			try {
				using (FSpot.ImageFile img = FSpot.ImageFile.Create (original)) {
					Time = img.Date;
				}
			} catch (Exception) {
				Time = DateTime.Now;
			}
		}

		public IBrowsableItemVersion DefaultVersion { get; private set; }
		public SafeUri DestinationUri { get; set; }

		public System.DateTime Time { get; private set; }

		public Tag [] Tags { get { throw new NotImplementedException (); } }
		public string Description { get { throw new NotImplementedException (); } }
		public string Name { get { throw new NotImplementedException (); } }
		public uint Rating { get { return 0; } }

		internal uint PhotoId { get; set; }
	}
}
