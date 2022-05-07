// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations.Schema;

using FSpot.Core;
using FSpot.Utils;

using Hyena;

namespace FSpot.Models
{
	public partial class PhotoVersion : BaseDbSet, IPhotoVersion
	{
		public Photo Photo { get; set; }
		//public IPhoto Photo { get; private set; }

		public Guid PhotoId { get; set; }
		[NotMapped]
		public long OldPhotoId { get; set; }
		public long VersionId { get; set; }
		public string Name { get; set; }
		public string BaseUri { get; set; }
		public string Filename { get; set; }
		public string ImportMd5 { get; set; }
		public bool Protected { get; set; }

		[NotMapped]
		public SafeUri Uri {
			get => new SafeUri ($"{BaseUri}{Filename}");
			set {
				BaseUri = value.GetBaseUri ();
				Filename = value.GetFilename ();
			}
		}

		public PhotoVersion ()
		{ }

		public PhotoVersion (IPhoto photo, long versionId, SafeUri baseUri,
			string filename, string md5Sum, string name, bool isProtected)
		{
			Photo = (Photo)photo;
			VersionId = versionId;
			BaseUri = baseUri.ToString ();
			Filename = filename;
			ImportMd5 = md5Sum;
			Name = name;
			Protected = isProtected;
		}

	}
}
