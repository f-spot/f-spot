//
// PhotoStoreTests.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;

using FSpot.Database;

using FSpot.Resources.Lang;

using Hyena;

using Mocks;

using Moq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Database.Tests
{
	[TestClass]
	public class PhotoStoreTests
	{
		// PhotoStore automatically creates a new SQLite database.
		// Thus, these tests should be considered as integration/system tests instead of unit tests.

		string database;

		readonly SafeUri uri = new SafeUri ("file:///1.jpg");
		readonly string originalName = "Original Name";

		readonly SafeUri modifiedUri = new SafeUri ("file:///2.jpg");
		const string modifiedName = "Modified Name";

		[TestInitialize]
		public void Setup ()
		{
			database = Path.Combine (AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), "unit-test.db");
			Cleanup ();
		}

		[TestCleanup]
		public void TearDown () => Cleanup ();

		public void Cleanup ()
		{
			if (database != null && File.Exists (database))
				File.Delete (database);
		}

		[TestMethod]
		public void CreateFrom ()
		{
			using (var databaseConnection = new FSpotDatabaseConnection (database)) {
				var dbMock = new Mock<IDb> ();
				dbMock.Setup (m => m.Database).Returns (databaseConnection);
				var store = new PhotoStore (null, null, dbMock.Object, true);
				var photoMock = PhotoMock.CreateNamed (uri, originalName);

				var photo = store.CreateFrom (photoMock, true, 1);

				// default version name is ignored on import
				Assert.AreEqual (Strings.Original, photo.DefaultVersion.Name);
				Assert.AreEqual (uri, photo.DefaultVersion.BaseUri);
				Assert.HasCount (1, photo.Versions);

				Assert.AreEqual (1, store.TotalPhotos);
			}
		}

		[TestMethod]
		public void CreateFromWithVersionIgnored ()
		{
			using (var databaseConnection = new FSpotDatabaseConnection (database)) {
				var dbMock = new Mock<IDb> ();
				dbMock.Setup (m => m.Database).Returns (databaseConnection);
				var store = new PhotoStore (null, null, dbMock.Object, true);
				var photoMock = PhotoMock.CreateWithVersion (uri, originalName, modifiedUri, modifiedName);

				var photo = store.CreateFrom (photoMock, true, 1);

				Assert.AreEqual (Strings.Original, photo.DefaultVersion.Name);
				Assert.AreEqual (uri, photo.DefaultVersion.BaseUri);
				// CreateFrom ignores any versions except the default version
				Assert.HasCount (1, photo.Versions);

				Assert.AreEqual (1, store.TotalPhotos);
			}
		}

		[TestMethod]
		public void CreateFromWithVersionAdded ()
		{
			using (var databaseConnection = new FSpotDatabaseConnection (database)) {
				var dbMock = new Mock<IDb> ();
				dbMock.Setup (m => m.Database).Returns (databaseConnection);
				var store = new PhotoStore (null, null, dbMock.Object, true);
				var photoMock = PhotoMock.CreateWithVersion (uri, originalName, modifiedUri, modifiedName);

				var photo = store.CreateFrom (photoMock, false, 1);

				Assert.AreEqual (modifiedName, photo.DefaultVersion.Name);
				Assert.AreEqual (modifiedUri, photo.DefaultVersion.BaseUri);
				Assert.HasCount (2, photo.Versions);
				// version id 1 is the first photo added - the original photo
				Assert.AreEqual (originalName, photo.GetVersion (1).Name);
				Assert.AreEqual (uri, photo.GetVersion (1).BaseUri);

				Assert.AreEqual (1, store.TotalPhotos);
			}
		}
	}
}
