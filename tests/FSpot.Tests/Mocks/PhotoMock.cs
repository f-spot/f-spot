//
// PhotoMock.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using FSpot.Core;

using Hyena;

using Moq;

namespace Mocks
{
	public static class PhotoMock
	{
		public static IPhoto Create (SafeUri uri, params SafeUri[] versionUris)
		{
			// don't care about time
			return Create (uri, DateTime.MinValue, versionUris);
		}

		public static IPhoto Create (SafeUri uri, DateTime time, params SafeUri[] versionUris)
		{
			var defaultVersionMock = new Mock<IPhotoVersion> ();
			defaultVersionMock.SetupProperty (v => v.Uri, uri);
			var defaultVersion = defaultVersionMock.Object;

			var versions = versionUris.Select (u => {
				var mock = new Mock<IPhotoVersion> ();
				mock.SetupProperty (m => m.Uri, u);
				return mock.Object;
			}).ToList ();

			var allVersions = new[] { defaultVersion }.Concat (versions);

			var photo = new Mock<IPhoto> ();
			photo.Setup (p => p.DefaultVersion).Returns (defaultVersion);
			photo.Setup (p => p.Time).Returns (time);
			photo.Setup (p => p.Versions).Returns (allVersions);
			return photo.Object;
		}

		static Mock<IPhoto> CreateNamedMock (SafeUri uri, string name)
		{
			var versionMock = new Mock<IPhotoVersion> ();
			versionMock.Setup (v => v.BaseUri).Returns (uri);
			versionMock.Setup (v => v.Filename).Returns (Path.GetFileName (uri));
			versionMock.Setup (v => v.Name).Returns (name);

			var photoMock = new Mock<IPhoto> ();
			photoMock.Setup (p => p.DefaultVersion).Returns (versionMock.Object);
			return photoMock;
		}

		public static IPhoto CreateNamed (SafeUri uri, string name)
			=> CreateNamedMock (uri, name).Object;

		public static IPhoto CreateWithVersion (SafeUri originalUri, string originalName, SafeUri versionUri, string versionName)
		{
			var photoMock = CreateNamedMock (originalUri, originalName);

			var versionMock = new Mock<IPhotoVersion> ();
			versionMock.Setup (v => v.Name).Returns (versionName);
			versionMock.Setup (v => v.BaseUri).Returns (versionUri);
			versionMock.Setup (v => v.Filename).Returns (Path.GetFileName (versionUri));

			var versions = new List<IPhotoVersion> { photoMock.Object.DefaultVersion, versionMock.Object };
			photoMock.Setup (p => p.Versions).Returns (versions);
			return photoMock.Object;
		}
	}
}
