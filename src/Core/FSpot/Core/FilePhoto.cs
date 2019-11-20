//
// FilePhoto.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
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
using System.Linq;
using FSpot.Utils;
using Hyena;
using Mono.Unix.Native;

namespace FSpot.Core
{
	public class FilePhoto : IPhoto, IInvalidPhotoCheck
	{
		bool metadata_parsed;

		readonly List<IPhotoVersion> versions;

		public FilePhoto (SafeUri uri) : this (uri, null)
		{
		}

		public FilePhoto (SafeUri uri, string name)
		{
			versions = new List<IPhotoVersion> ();
			versions.Add (new FilePhotoVersion { Uri = uri, Name = name });
		}

		public bool IsInvalid {
			get {
				if (metadata_parsed)
					return false;

				try {
					EnsureMetadataParsed ();
					return false;
				} catch (Exception) {
					return true;
				}
			}
		}

		void EnsureMetadataParsed ()
		{
			if (metadata_parsed)
				return;

			using (var metadata = Metadata.Parse (DefaultVersion.Uri)) {
				if (metadata != null) {
					var date = metadata.ImageTag.DateTime;
					time = date.HasValue ? date.Value : CreateDate;
					description = metadata.ImageTag.Comment;
				} else {
					throw new Exception ("Corrupt File!");
				}
			}

			metadata_parsed = true;
		}

		DateTime CreateDate {
			get {
				var info = GLib.FileFactory.NewForUri (DefaultVersion.Uri).QueryInfo ("time::changed", GLib.FileQueryInfoFlags.None, null);
				return NativeConvert.ToDateTime ((long)info.GetAttributeULong ("time::changed"));
			}
		}

		public Tag[] Tags {
			get { return null; }
		}

		DateTime time;
		public DateTime Time {
			get {
				EnsureMetadataParsed ();
				return time;
			}
		}

		public IPhotoVersion DefaultVersion {
			get {
				return versions.First ();
			}
		}

		public IEnumerable<IPhotoVersion> Versions {
			get {
				return versions;
			}
		}

		string description;
		public string Description {
			get {
				EnsureMetadataParsed ();
				return description;
			}
		}

		public string Name {
			get { return DefaultVersion.Uri.GetFilename (); }
		}

		public uint Rating {
			//FIXME ndMaxxer: correct?
			get { return 0; }
		}

		public void AddVersion(SafeUri uri, string name)
		{
			versions.Add (new FilePhotoVersion { Uri = uri, Name = name });
		}

		class FilePhotoVersion : IPhotoVersion
		{
			public string Name { get; set; }

			public bool IsProtected {
				get { return true; }
			}

			public SafeUri BaseUri {
				get { return Uri.GetBaseUri (); }
			}
			public string Filename {
				get { return Uri.GetFilename (); }
			}
			public SafeUri Uri { get; set; }

			string import_md5 = string.Empty;
			public string ImportMD5 {
				get {
					if (import_md5 == string.Empty)
						import_md5 = HashUtils.GenerateMD5 (Uri);
					return import_md5;
				}
			}
		}
	}
}
