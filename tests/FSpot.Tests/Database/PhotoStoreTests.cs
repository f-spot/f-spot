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

using FSpot.Core;
using FSpot.Database;
using FSpot.Resources.Lang;
using FSpot.Utils;

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
		public void GetByUri_FindsADirectoryStoredWithATrailingSlash ()
		{
			using var databaseConnection = new FSpotDatabaseConnection (database);
			var dbMock = new Mock<IDb> ();
			dbMock.Setup (m => m.Database).Returns (databaseConnection);
			var store = new PhotoStore (null, null, dbMock.Object, true);
			var version = new Mock<IPhotoVersion> ();
			var directory = new SafeUri ("file:///tmp/database/", true);
			version.Setup (v => v.BaseUri).Returns (directory);
			version.Setup (v => v.Filename).Returns ("sample.jpg");
			version.Setup (v => v.Name).Returns ("Original");
			var photo = new Mock<IPhoto> ();
			photo.Setup (p => p.DefaultVersion).Returns (version.Object);
			photo.Setup (p => p.Versions).Returns (new[] { version.Object });
			photo.Setup (p => p.Time).Returns (new DateTime (2020, 1, 2));
			store.CreateFrom (photo.Object, true, 1);

			string path = directory.Append ("sample.jpg").LocalPath;
			Assert.IsNotNull (store.GetByUri (SafeUri.FromPath (path)));
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
		public void HasDuplicate_DoesNotTreatImportMd5AsAColumnName ()
		{
			using (var databaseConnection = new FSpotDatabaseConnection (database)) {
				var dbMock = new Mock<IDb> ();
				dbMock.Setup (m => m.Database).Returns (databaseConnection);
				var store = new PhotoStore (null, null, dbMock.Object, true);

				Assert.IsFalse (store.HasDuplicate (PhotoWithMd5 (uri, "d1dfafd3d330eac79618309d2708c7ac")));
			}
		}

		[TestMethod]
		public void HasDuplicate_FindsAnExistingPhotoByImportMd5 ()
		{
			using (var databaseConnection = new FSpotDatabaseConnection (database)) {
				var dbMock = new Mock<IDb> ();
				dbMock.Setup (m => m.Database).Returns (databaseConnection);
				var store = new PhotoStore (null, null, dbMock.Object, true);

				const string hash = "d1dfafd3d330eac79618309d2708c7ac";
				store.CreateFrom (PhotoWithMd5 (uri, hash), true, 1);

				Assert.IsTrue (store.HasDuplicate (PhotoWithMd5 (modifiedUri, hash)));
			}
		}

		static IPhoto PhotoWithMd5 (SafeUri photoUri, string hash)
		{
			var version = new Mock<IPhotoVersion> ();
			version.Setup (v => v.Uri).Returns (photoUri);
			version.Setup (v => v.BaseUri).Returns (photoUri.GetBaseUri ());
			version.Setup (v => v.Filename).Returns (photoUri.GetFilename ());
			version.Setup (v => v.ImportMD5).Returns (hash);
			version.Setup (v => v.Name).Returns ("Original");

			var photo = new Mock<IPhoto> ();
			photo.Setup (p => p.DefaultVersion).Returns (version.Object);
			photo.Setup (p => p.Versions).Returns (new[] { version.Object });
			photo.Setup (p => p.Time).Returns (DateTime.UnixEpoch);
			return photo.Object;
		}

		[TestMethod]
		public void ListFolderCounts_GroupsPhotosByBaseUri ()
		{
			using (var databaseConnection = new FSpotDatabaseConnection (database)) {
				var dbMock = new Mock<IDb> ();
				dbMock.Setup (m => m.Database).Returns (databaseConnection);
				var store = new PhotoStore (null, null, dbMock.Object, true);
				store.CreateFrom (PhotoInFolder ("file:///photos/2020/01/", "a.jpg"), true, 1);
				store.CreateFrom (PhotoInFolder ("file:///photos/2020/01/", "b.jpg"), true, 1);
				store.CreateFrom (PhotoInFolder ("file:///photos/2020/06/", "c.jpg"), true, 1);

				var folders = store.ListFolderCounts ();

				Assert.HasCount (2, folders);
				Assert.AreEqual (new SafeUri ("file:///photos/2020/01/", true), folders[0].Uri);
				Assert.AreEqual (2, folders[0].Count);
				Assert.AreEqual (new SafeUri ("file:///photos/2020/06/", true), folders[1].Uri);
				Assert.AreEqual (1, folders[1].Count);
			}
		}

		[TestMethod]
		public void Commit_KeepsAnEmptyDirectoryEmpty ()
		{
			using (var databaseConnection = new FSpotDatabaseConnection (database)) {
				var dbMock = new Mock<IDb> ();
				dbMock.Setup (m => m.Database).Returns (databaseConnection);
				var store = new PhotoStore (null, null, dbMock.Object, true);
				Photo created = store.CreateFrom (PhotoInFolder ("file:///photos/2020/01/", "a.jpg"), true, 1);
				databaseConnection.Execute ("UPDATE photos SET base_uri = '' WHERE id = " + created.Id);
				databaseConnection.Execute ("UPDATE photo_versions SET base_uri = '' WHERE photo_id = " + created.Id);
				store.ForgetCached ();
				Photo loaded = store.Get (created.Id);

				loaded.Rating = 3;
				loaded.Description = "kept";
				store.Commit (loaded);

				Assert.AreEqual ("", StoredDirectory (databaseConnection, "photos", created.Id));
				Assert.AreEqual ("", StoredDirectory (databaseConnection, "photo_versions", created.Id));
			}
		}

		static string StoredDirectory (FSpotDatabaseConnection connection, string table, uint id)
		{
			string column = table == "photos" ? "id" : "photo_id";
			using var reader = connection.Query ("SELECT base_uri FROM " + table + " WHERE " + column + " = " + id);
			reader.Read ();
			return reader["base_uri"]?.ToString () ?? "";
		}

		[TestMethod]
		public void Get_LoadsAPhotoWhoseDirectoryIsEmpty ()
		{
			using (var databaseConnection = new FSpotDatabaseConnection (database)) {
				var dbMock = new Mock<IDb> ();
				dbMock.Setup (m => m.Database).Returns (databaseConnection);
				var store = new PhotoStore (null, null, dbMock.Object, true);
				Photo created = store.CreateFrom (PhotoInFolder ("file:///photos/2020/01/", "a.jpg"), true, 1);
				databaseConnection.Execute ("UPDATE photos SET base_uri = '' WHERE id = " + created.Id);
				databaseConnection.Execute ("UPDATE photo_versions SET base_uri = '' WHERE photo_id = " + created.Id);
				store.ForgetCached ();

				Photo loaded = store.Get (created.Id);

				Assert.AreEqual ("a.jpg", loaded.Name);
			}
		}

		[TestMethod]
		public void ListFolderCounts_SkipsAnEmptyDirectory ()
		{
			using (var databaseConnection = new FSpotDatabaseConnection (database)) {
				var dbMock = new Mock<IDb> ();
				dbMock.Setup (m => m.Database).Returns (databaseConnection);
				var store = new PhotoStore (null, null, dbMock.Object, true);
				store.CreateFrom (PhotoInFolder ("file:///photos/2020/01/", "a.jpg"), true, 1);
				Photo empty = store.CreateFrom (PhotoInFolder ("file:///photos/2020/01/", "gone.jpg"), true, 1);
				databaseConnection.Execute ("UPDATE photos SET base_uri = '' WHERE id = " + empty.Id);
				databaseConnection.Execute ("UPDATE photo_versions SET base_uri = '' WHERE photo_id = " + empty.Id);

				var folders = store.ListFolderCounts ();

				Assert.HasCount (1, folders);
				Assert.AreEqual (1, folders[0].Count);
			}
		}

		static IPhoto PhotoInFolder (string directory, string filename)
		{
			var dir = new SafeUri (directory, true);
			var uri = dir.Append (filename);
			var version = new Mock<IPhotoVersion> ();
			version.Setup (v => v.Uri).Returns (uri);
			version.Setup (v => v.BaseUri).Returns (dir);
			version.Setup (v => v.Filename).Returns (filename);
			version.Setup (v => v.Name).Returns ("Original");
			version.Setup (v => v.ImportMD5).Returns (string.Empty);

			var photo = new Mock<IPhoto> ();
			photo.Setup (p => p.DefaultVersion).Returns (version.Object);
			photo.Setup (p => p.Versions).Returns (new[] { version.Object });
			photo.Setup (p => p.Time).Returns (DateTime.UnixEpoch);
			return photo.Object;
		}

		[TestMethod]
		public void Get_WhenVersionNameIsNull_DoesNotThrow ()
		{
			using (var databaseConnection = new FSpotDatabaseConnection (database)) {
				var dbMock = new Mock<IDb> ();
				dbMock.Setup (m => m.Database).Returns (databaseConnection);
				var store = new PhotoStore (null, null, dbMock.Object, true);
				var created = store.CreateFrom (PhotoMock.CreateNamed (uri, originalName), true, 1);

				databaseConnection.Execute ("UPDATE photo_versions SET name = NULL");

				var photo = store.Get (created.Id);

				Assert.IsNotNull (photo);
				Assert.IsNull (photo.DefaultVersion.Name);
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
