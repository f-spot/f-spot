//
// XdgDirectoryServiceTests.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using FSpot.FileSystem;
using Mocks;
using Moq;
using NUnit.Framework;

namespace FSpot.Thumbnail.UnitTest
{
	[TestFixture]
	public class XdgDirectoryServiceTests
	{
		string PathToCache = Path.Combine ("/", "path", "to", ".cache");
		string HomeUserPath = Path.Combine ("/", "home", "user");
		#region Tests

		#region GetUserCacheDir tests

		[Test]
		// Analysis disable once InconsistentNaming
		public void GetUserCacheDir_ReturnsXdgCacheHome_WhenXdgCacheHomeAndHomeAreSet ()
		{
			var environment = new EnvironmentMock ("XDG_CACHE_HOME", PathToCache);
			environment.SetVariable ("HOME", HomeUserPath);
			var xdg = new XdgDirectoryService (null, environment.Object);

			var result = xdg.GetUserCacheDir ();

			Assert.AreEqual (PathToCache, result);
		}

		[Test]
		// Analysis disable once InconsistentNaming
		public void GetUserCacheDir_ReturnsXdgCacheHome_WhenXdgCacheHomeSet ()
		{
			var environment = new EnvironmentMock ("XDG_CACHE_HOME", PathToCache);
			var xdg = new XdgDirectoryService (null, environment.Object);

			var result = xdg.GetUserCacheDir ();

			Assert.AreEqual (PathToCache, result);
		}

		[Test]
		// Analysis disable once InconsistentNaming
		public void GetUserCacheDir_ReturnsHome_WhenXdgCacheHomeNotSet ()
		{
			var environment = new EnvironmentMock ("HOME", HomeUserPath);
			var xdg = new XdgDirectoryService (null, environment.Object);

			var result = xdg.GetUserCacheDir ();

			Assert.AreEqual (Path.Combine (HomeUserPath, ".cache"), result);
		}

		[Test]
		// Analysis disable once InconsistentNaming
		public void GetUserCacheDir_ReturnsTempPath_WhenXdgCacheHomeAndHomeNotSet ()
		{
			var environment = EnvironmentMock.Create ("user");
			var fileSystem = new Mock<IFileSystem> { DefaultValue = DefaultValue.Mock };
			var path = fileSystem.Object.Path;
			var pathMock = Mock.Get (path);
			pathMock.Setup (p => p.GetTempPath ()).Returns (Path.Combine ("/", "temp", "path"));

			var result = new XdgDirectoryService (fileSystem.Object, environment).GetUserCacheDir ();

			Assert.AreEqual (Path.Combine ("/", "temp", "path", "user", ".cache"), result);
		}

		[Test]
		// Analysis disable once InconsistentNaming
		public void GetUserCacheDir_ReturnsCachedValue_WhenXdgCacheHomeIsChanged ()
		{
			var fistPathCache = Path.Combine ("/", "first", "path", ".cache");
			var secondPathCache = Path.Combine ("/", "second", "path", ".cache");

			var environment = new EnvironmentMock ("XDG_CACHE_HOME", fistPathCache);
			var xdg = new XdgDirectoryService (null, environment.Object);
			xdg.GetUserCacheDir ();
			environment.SetVariable ("XDG_CACHE_HOME", secondPathCache);

			var result = xdg.GetUserCacheDir ();

			Assert.AreEqual (fistPathCache, result);
		}

		#endregion

		#region GetThumbnailsDir tests

		[Test]
		// Analysis disable once InconsistentNaming
		public void GethThumbnailsDir_ReturnsFolderForLargeThumbnails ()
		{
			var environment = new EnvironmentMock ("XDG_CACHE_HOME", PathToCache);
			var xdg = new XdgDirectoryService (null, environment.Object);

			var result = xdg.GetThumbnailsDir (ThumbnailSize.Large);

			Assert.AreEqual (Path.Combine (PathToCache, "thumbnails", "large"), result);
		}

		[Test]
		// Analysis disable once InconsistentNaming
		public void GethThumbnailsDir_ReturnsFolderForNormalThumbnails ()
		{
			var environment = new EnvironmentMock ("XDG_CACHE_HOME", PathToCache);
			var xdg = new XdgDirectoryService (null, environment.Object);

			var result = xdg.GetThumbnailsDir (ThumbnailSize.Normal);

			Assert.AreEqual (Path.Combine (PathToCache, "thumbnails", "normal"), result);
		}

		#endregion

		#endregion
	}
}
