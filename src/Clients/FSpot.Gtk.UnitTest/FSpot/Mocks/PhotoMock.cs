//
// PhotoMock.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;
using FSpot.Core;
using Hyena;
using Moq;

namespace FSpot.Mocks
{
	public static class PhotoMock
	{
		static Mock<IPhoto> CreateMock (SafeUri uri, string name)
		{
			var versionMock = new Mock<IPhotoVersion> ();
			versionMock.Setup (v => v.BaseUri).Returns (uri);
			versionMock.Setup (v => v.Filename).Returns (Path.GetFileName (uri));
			versionMock.Setup (v => v.Name).Returns (name);

			var photoMock = new Mock<IPhoto> ();
			photoMock.Setup (p => p.DefaultVersion).Returns (versionMock.Object);

			return photoMock;
		}

		public static IPhoto Create (SafeUri uri, string name)
		{
			return CreateMock(uri, name).Object;
		}

		public static IPhoto CreateWithVersion (SafeUri originalUri, string originalName, SafeUri versionUri, string versionName)
		{
			var photoMock = CreateMock (originalUri, originalName);

			var versionMock = new Mock<IPhotoVersion> ();
			versionMock.Setup (v => v.Name).Returns (versionName);
			versionMock.Setup (v => v.BaseUri).Returns (versionUri);
			versionMock.Setup (v => v.Filename).Returns (Path.GetFileName (versionUri));

			var versions = new[] { photoMock.Object.DefaultVersion, versionMock.Object };

			photoMock.Setup (p => p.Versions).Returns (versions);
			return photoMock.Object;
		}
	}
}
