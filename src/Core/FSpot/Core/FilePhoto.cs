// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using FSpot.Utils;
using FSpot.Models;

using Hyena;

namespace FSpot.Core
{
	public class FilePhoto : IPhoto, IInvalidPhotoCheck
	{
		bool metadata_parsed;

		readonly List<IPhotoVersion> versions;

		public FilePhoto (SafeUri uri, string name = null)
		{
			versions = new List<IPhotoVersion> {
				new FilePhotoVersion {Uri = uri, Name = name}
			};
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

			using (var metadata = MetadataUtils.Parse (DefaultVersion.Uri)) {
				if (metadata != null) {
					var date = metadata.ImageTag.DateTime;
					time = date ?? CreateDate;
					description = metadata.ImageTag.Comment;
				} else {
					throw new Exception ("Corrupt File!");
				}
			}

			metadata_parsed = true;
		}

		DateTime CreateDate {
			get {
				var info = new FileInfo (DefaultVersion.Uri.AbsolutePath);
				return info.CreationTime;
			}
		}

		public List<Tag> Tags {
			get { return null; }
		}

		DateTime time;
		public DateTime UtcTime {
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

		public List<IPhotoVersion> Versions {
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

		public long Rating {
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

			public bool Protected {
				get { return true; }
			}

			public string BaseUri {
				get { return Uri.GetBaseUri (); }
			}
			public string Filename {
				get { return Uri.GetFilename (); }
			}
			public SafeUri Uri { get; set; }

			string import_md5 = string.Empty;
			public string ImportMd5 {
				get {
					if (string.IsNullOrEmpty (import_md5))
						import_md5 = HashUtils.GenerateMD5 (Uri);
					return import_md5;
				}
			}
		}
	}
}
