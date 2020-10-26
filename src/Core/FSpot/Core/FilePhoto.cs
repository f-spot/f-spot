//
// FilePhoto.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using FSpot.Utils;

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

		public void AddVersion (SafeUri uri, string name)
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

			string importMd5 = string.Empty;
			public string ImportMD5 {
				get {
					if (string.IsNullOrEmpty (importMd5))
						importMd5 = HashUtils.GenerateMD5 (Uri);
					return importMd5;
				}
			}
		}
	}
}
