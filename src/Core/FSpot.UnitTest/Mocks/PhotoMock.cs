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
	}
}
