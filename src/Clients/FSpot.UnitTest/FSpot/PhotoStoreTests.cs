//
// PhotoStoreTests.cs
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

using System.IO;
using System.Linq;
using FSpot.Database;
using FSpot.Mocks;
using Hyena;
using Mono.Unix;
using Moq;
using NUnit.Framework;

namespace FSpot
{
	[TestFixture]
	public class PhotoStoreTests
	{
		// PhotoStore automatically creates a new SQLite database.
		// Thus, these tests should be considered as integration/system tests instead of unit tests.

		readonly string database = Path.Combine (Directory.GetCurrentDirectory (), "unit-test.db");

		readonly SafeUri uri = new SafeUri ("file:///1.jpg");
		readonly string originalName = "Original Name";

		readonly SafeUri modifiedUri = new SafeUri ("file:///2.jpg");
		const string modifiedName = "Modified Name";

		[TearDown]
		public void Cleanup () {
			if (File.Exists (database))
				File.Delete (database);
		}

		[Test]
		public void CreateFrom()
		{
			var databaseConnection = new FSpotDatabaseConnection (database);
			var dbMock = new Mock<IDb> ();
			dbMock.Setup (m => m.Database).Returns (databaseConnection);
			var store = new PhotoStore (null, null, dbMock.Object, true);
			var photoMock = PhotoMock.Create (uri, originalName);

			var photo = store.CreateFrom (photoMock, true, 1);

			// default version name is ignored on import
			Assert.AreEqual (Catalog.GetString ("Original"), photo.DefaultVersion.Name);
			Assert.AreEqual (uri, photo.DefaultVersion.BaseUri);
			Assert.AreEqual (1, photo.Versions.Count ());

			Assert.AreEqual (1, store.TotalPhotos);
		}

		[Test]
		public void CreateFromWithVersionIgnored()
		{
			var databaseConnection = new FSpotDatabaseConnection (database);
			var dbMock = new Mock<IDb> ();
			dbMock.Setup (m => m.Database).Returns (databaseConnection);
			var store = new PhotoStore (null, null, dbMock.Object, true);
			var photoMock = PhotoMock.CreateWithVersion (uri, originalName, modifiedUri, modifiedName);

			var photo = store.CreateFrom (photoMock, true, 1);

			Assert.AreEqual (Catalog.GetString ("Original"), photo.DefaultVersion.Name);
			Assert.AreEqual (uri, photo.DefaultVersion.BaseUri);
			// CreateFrom ignores any versions except the default version
			Assert.AreEqual (1, photo.Versions.Count ());

			Assert.AreEqual (1, store.TotalPhotos);
		}

		[Test]
		public void CreateFromWithVersionAdded()
		{
			var databaseConnection = new FSpotDatabaseConnection (database);
			var dbMock = new Mock<IDb> ();
			dbMock.Setup (m => m.Database).Returns (databaseConnection);
			var store = new PhotoStore (null, null, dbMock.Object, true);
			var photoMock = PhotoMock.CreateWithVersion (uri, originalName, modifiedUri, modifiedName);

			var photo = store.CreateFrom (photoMock, false, 1);

			Assert.AreEqual (modifiedName, photo.DefaultVersion.Name);
			Assert.AreEqual (modifiedUri, photo.DefaultVersion.BaseUri);
			Assert.AreEqual (2, photo.Versions.Count ());
			// version id 1 is the first photo added - the original photo
			Assert.AreEqual (originalName, photo.GetVersion(1).Name);
			Assert.AreEqual (uri, photo.GetVersion(1).BaseUri);

			Assert.AreEqual (1, store.TotalPhotos);
		}
	}
}
