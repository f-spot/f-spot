/*
 * FileBrowsableItem.cs
 *
 * Author(s):
 *	Larry Ewing  (lewing@novell.com)
 *	Stephane Delcroix  (stephane@delcroix.org)
 *
 * This is free software. See COPYING for details
 */


using System;
using System.IO;
using System.Collections;
using System.Xml;

using Hyena;
using FSpot.Utils;
using Mono.Unix.Native;

namespace FSpot {
	public class FileBrowsableItem : IBrowsableItem
	{
		bool metadata_parsed = false;

		public FileBrowsableItem (SafeUri uri)
		{
			DefaultVersion = new FileBrowsableItemVersion () {
                Uri = uri
            };
		}

		private void EnsureMetadataParsed ()
		{
			if (metadata_parsed)
				return;

            var res = new GIOTagLibFileAbstraction () { Uri = DefaultVersion.Uri };
            var metadata_file = TagLib.File.Create (res) as TagLib.Image.File;
            var date = metadata_file.ImageTag.DateTime;
            time = date.HasValue ? date.Value : CreateDate;
            description = metadata_file.ImageTag.Comment;

			metadata_parsed = true;
		}

		public Tag [] Tags {
			get {
				return null;
			}
		}

		private DateTime time;
		public DateTime Time {
			get {
				EnsureMetadataParsed ();
				return time;
			}
		}

        private DateTime CreateDate {
            get {
                var info = GLib.FileFactory.NewForUri (DefaultVersion.Uri).QueryInfo ("time::created", GLib.FileQueryInfoFlags.None, null);
                return NativeConvert.ToDateTime ((long)info.GetAttributeULong ("time::created"));
            }
        }

		public IBrowsableItemVersion DefaultVersion { get; private set; }

		private string description;
		public string Description {
			get {
				EnsureMetadataParsed ();
				return description;
			}
		}

		public string Name {
			get {
				return DefaultVersion.Uri.GetFilename ();
			}
		}

		public uint Rating {
			get {
				return 0; //FIXME ndMaxxer: correct?
			}
		}

		private class FileBrowsableItemVersion : IBrowsableItemVersion {
			public string Name { get { return String.Empty; } }
			public bool IsProtected { get { return true; } }

			public SafeUri BaseUri { get { return Uri.GetBaseUri (); } }
			public string Filename { get { return Uri.GetFilename (); } }
			public SafeUri Uri { get; set; }

			private string import_md5 = String.Empty;
			public string ImportMD5 {
				get {
					if (import_md5 == String.Empty)
						import_md5 = Photo.GenerateMD5 (Uri);
					return import_md5;
				}
			}
		}
	}
}
