//
// PhotoVersion.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Core;
using FSpot.Utils;

using Hyena;

namespace FSpot
{
	public class PhotoVersion : IPhotoVersion
	{
		public string Name { get; set; }
		public IPhoto Photo { get; private set; }
		public SafeUri BaseUri { get; set; }
		public string Filename { get; set; }

		public SafeUri Uri {
			get { return BaseUri.Append (Filename); }
			set {
				BaseUri = value.GetBaseUri ();
				Filename = value.GetFilename ();
			}
		}

		public string ImportMD5 { get; set; }
		public uint VersionId { get; private set; }
		public bool IsProtected { get; private set; }

		public PhotoVersion (IPhoto photo, uint versionId, SafeUri baseUri, string filename, string md5Sum, string name, bool isProtected)
		{
			Photo = photo;
			VersionId = versionId;
			BaseUri = baseUri;
			Filename = filename;
			ImportMD5 = md5Sum;
			Name = name;
			IsProtected = isProtected;
		}
	}
}
