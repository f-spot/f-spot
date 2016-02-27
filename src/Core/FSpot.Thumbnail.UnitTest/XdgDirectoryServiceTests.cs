//
// XdgDirectoryServiceTests.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
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

using FSpot.FileSystem;
using FSpot.Utils;
using Mocks;
using Moq;
using NUnit.Framework;

namespace FSpot.Thumbnail.UnitTest
{
	[TestFixture]
	public class XdgDirectoryServiceTests
	{
		#region Tests

		#region GetUserCacheDir tests

		[Test]
		// Analysis disable once InconsistentNaming
		public void GetUserCacheDir_ReturnsXdgCacheHome_WhenXdgCacheHomeAndHomeAreSet ()
		{
			var environment = new EnvironmentMock ("XDG_CACHE_HOME", "/path/to/.cache");
			environment.SetVariable ("HOME", "/home/user");
			var xdg = new XdgDirectoryService (null, environment.Object);

			var result = xdg.GetUserCacheDir ();

			Assert.AreEqual ("/path/to/.cache", result);
		}

		[Test]
		// Analysis disable once InconsistentNaming
		public void GetUserCacheDir_ReturnsXdgCacheHome_WhenXdgCacheHomeSet ()
		{
			var environment = new EnvironmentMock ("XDG_CACHE_HOME", "/path/to/.cache");
			var xdg = new XdgDirectoryService (null, environment.Object);

			var result = xdg.GetUserCacheDir ();

			Assert.AreEqual ("/path/to/.cache", result);
		}

		[Test]
		// Analysis disable once InconsistentNaming
		public void GetUserCacheDir_ReturnsHome_WhenXdgCacheHomeNotSet ()
		{
			var environment = new EnvironmentMock ("HOME", "/home/user");
			var xdg = new XdgDirectoryService (null, environment.Object);

			var result = xdg.GetUserCacheDir ();

			Assert.AreEqual ("/home/user/.cache", result);
		}

		[Test]
		// Analysis disable once InconsistentNaming
		public void GetUserCacheDir_ReturnsTempPath_WhenXdgCacheHomeAndHomeNotSet ()
		{
			var environment = EnvironmentMock.Create ("user");
			var fileSystem = new Mock<IFileSystem> { DefaultValue = DefaultValue.Mock };
			var path = fileSystem.Object.Path;
			var pathMock = Mock.Get (path);
			pathMock.Setup (p => p.GetTempPath ()).Returns ("/temp/path");

			var result = new XdgDirectoryService (fileSystem.Object, environment).GetUserCacheDir ();

			Assert.AreEqual ("/temp/path/user/.cache", result);
		}

		[Test]
		// Analysis disable once InconsistentNaming
		public void GetUserCacheDir_ReturnsCachedValue_WhenXdgCacheHomeIsChanged ()
		{
			var environment = new EnvironmentMock ("XDG_CACHE_HOME", "/first/path/.cache");
			var xdg = new XdgDirectoryService (null, environment.Object);
			xdg.GetUserCacheDir ();
			environment.SetVariable ("XDG_CACHE_HOME", "/second/path/.cache");

			var result = xdg.GetUserCacheDir ();

			Assert.AreEqual ("/first/path/.cache", result);
		}

		#endregion

		#region GetThumbnailsDir tests

		[Test]
		// Analysis disable once InconsistentNaming
		public void GethThumbnailsDir_ReturnsFolderForLargeThumbnails ()
		{
			var environment = new EnvironmentMock ("XDG_CACHE_HOME", "/path/to/.cache");
			var xdg = new XdgDirectoryService (null, environment.Object);

			var result = xdg.GetThumbnailsDir (ThumbnailSize.Large);

			Assert.AreEqual ("/path/to/.cache/thumbnails/large", result);
		}

		[Test]
		// Analysis disable once InconsistentNaming
		public void GethThumbnailsDir_ReturnsFolderForNormalThumbnails ()
		{
			var environment = new EnvironmentMock ("XDG_CACHE_HOME", "/path/to/.cache");
			var xdg = new XdgDirectoryService (null, environment.Object);

			var result = xdg.GetThumbnailsDir (ThumbnailSize.Normal);

			Assert.AreEqual ("/path/to/.cache/thumbnails/normal", result);
		}

		#endregion

		#endregion
	}
}
