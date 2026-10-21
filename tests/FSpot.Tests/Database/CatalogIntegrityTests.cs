// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;

using FSpot.Core;
using FSpot.Database.Jobs;
using FSpot.Imaging;
using FSpot.Thumbnail;

using Hyena;

using Microsoft.Data.Sqlite;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace FSpot.Database.Tests
{
	[TestClass]
	public sealed class CatalogIntegrityTests
	{
		string root;
		Db database;

		[TestInitialize]
		public void Open ()
		{
			root = Path.Combine (Path.GetTempPath (), "fspot-catalog-" + Guid.NewGuid ().ToString ("n"));
			Directory.CreateDirectory (root);
			database = new Db (new Mock<IImageFileFactory> ().Object, new Mock<IThumbnailService> ().Object, new Mock<IUpdaterUI> ().Object);
			database.Init (Path.Combine (root, "photos.db"), true);
		}

		[TestCleanup]
		public void Close ()
		{
			database?.Dispose ();
			if (root != null && Directory.Exists (root))
				Directory.Delete (root, true);
		}

		[TestMethod]
		public void Init_MissingFile_DoesNotCreateIt ()
		{
			database.Dispose ();
			database = null;
			string missing = Path.Combine (root, "absent.db");
			var unopened = new Db (new Mock<IImageFileFactory> ().Object, new Mock<IThumbnailService> ().Object, new Mock<IUpdaterUI> ().Object);

			Assert.ThrowsExactly<Exception> (() => unopened.Init (missing, false));

			Assert.IsFalse (File.Exists (missing));
		}

		[TestMethod]
		public void Repair_LeavesTheOldFileBesideTheNewCatalog ()
		{
			string path = Path.Combine (root, "photos.db");
			database.Dispose ();
			var repaired = new Db (new Mock<IImageFileFactory> ().Object, new Mock<IThumbnailService> ().Object, new Mock<IUpdaterUI> ().Object);
			repaired.Init (path, false);

			string backup = repaired.Repair ();
			database = repaired;

			Assert.AreEqual (root, Path.GetDirectoryName (backup));
			Assert.IsTrue (File.Exists (backup));
			Assert.AreNotEqual (path, backup);
			Assert.IsTrue (database.Empty);
			Assert.AreEqual (0, Count ("photos"));
		}

		[TestMethod]
		public void Init_RejectsAFileThatIsNotACatalog ()
		{
			database.Dispose ();
			database = null;
			string path = Path.Combine (root, "notes.db");
			File.WriteAllText (path, "this is not a catalog");
			var unopened = new Db (new Mock<IImageFileFactory> ().Object, new Mock<IThumbnailService> ().Object, new Mock<IUpdaterUI> ().Object);

			Assert.ThrowsExactly<SqliteException> (() => unopened.Init (path, false));

			Assert.AreEqual ("this is not a catalog", File.ReadAllText (path));
		}

		[TestMethod]
		public void Rollback_DropsThePhotoAndTagWrittenInsideIt ()
		{
			Tag kept = database.Tags.CreateTag (null, "Kept", false);
			database.BeginTransaction ();
			database.Tags.CreateTag (null, "Gone", false);
			AddPhoto ("inside.jpg");
			database.RollbackTransaction ();
			string path = Path.Combine (root, "photos.db");
			database.Dispose ();
			database = Reopen (path);

			Assert.AreEqual (kept.Name, database.Tags.GetTagByName ("Kept").Name);
			Assert.IsNull (database.Tags.GetTagByName ("Gone"));
			Assert.AreEqual (0, Count ("photos"));
			Assert.AreEqual (1, Count ("tags", "name = 'Kept'"));
		}

		[TestMethod]
		public void Rollback_SameSession_ForgetsTheUndoneTagRatingAndRoll ()
		{
			Photo photo = AddPhoto ("kept.jpg");
			database.BeginTransaction ();
			database.Tags.CreateTag (null, "Gone", false);
			photo.Rating = 4;
			database.Photos.Commit (photo);
			uint rollId = database.Rolls.Create ().Id;
			Photo seen = database.Photos.Get (photo.Id);
			Assert.AreEqual (4u, seen.Rating);

			database.RollbackTransaction ();

			Assert.IsNull (database.Tags.GetTagByName ("Gone"));
			Assert.AreEqual (0u, database.Photos.Get (photo.Id).Rating);
			Assert.IsNull (database.Rolls.Get (rollId));
			Assert.AreEqual ("kept.jpg", database.Photos.Get (photo.Id).Name);
		}

		[TestMethod]
		public void CalculateMD5Sum_MissingFile_LeavesTheStoredHashAlone ()
		{
			Photo photo = AddPhoto ("gone.jpg");
			var version = (PhotoVersion)photo.DefaultVersion;
			string hash = version.ImportMD5;
			Assert.IsFalse (string.IsNullOrEmpty (hash));
			version.ImportMD5 = string.Empty;
			File.Delete (Path.Combine (root, "gone.jpg"));

			Assert.ThrowsExactly<FileNotFoundException> (() => database.Photos.CalculateMD5Sum (photo));

			Assert.AreEqual (hash, database.Photos.Get (photo.Id).DefaultVersion.ImportMD5);
			Assert.AreEqual ("gone.jpg", database.Photos.Get (photo.Id).Name);
		}

		[TestMethod]
		public void Commit_RollChange_SurvivesReopenAndLeavesTheOtherPhoto ()
		{
			Photo changed = database.Photos.Get (AddPhoto ("changed.jpg").Id);
			Photo other = database.Photos.Get (AddPhoto ("other.jpg").Id);
			uint previous = changed.RollId;
			uint kept = other.RollId;
			var when = new DateTime (2020, 6, 1, 12, 0, 0, DateTimeKind.Utc);
			uint next = database.Rolls.Create (when).Id;
			changed.RollId = next;
			database.Photos.Commit (changed);
			string path = Path.Combine (root, "photos.db");
			database.Dispose ();
			database = Reopen (path);

			Assert.AreNotEqual (previous, next);
			Assert.AreEqual (next, database.Photos.Get (changed.Id).RollId);
			Assert.AreEqual (kept, database.Photos.Get (other.Id).RollId);
			Assert.AreEqual (when, database.Rolls.Get (next).Time);
		}

		[TestMethod]
		public void Commit_RatingAndTag_SurviveReopenAndLeaveTheOtherPhotoAlone ()
		{
			Photo changed = AddPhoto ("changed.jpg");
			Photo other = AddPhoto ("other.jpg");
			Tag beach = database.Tags.CreateTag (null, "Beach", false);
			changed.Rating = 4;
			changed.Description = "Sun & shore";
			changed.AddTag (beach);
			database.Photos.Commit (changed);
			string path = Path.Combine (root, "photos.db");
			database.Dispose ();
			database = Reopen (path);

			Photo loaded = database.Photos.Get (changed.Id);
			Photo untouched = database.Photos.Get (other.Id);
			Assert.AreEqual (4u, loaded.Rating);
			Assert.AreEqual ("Sun & shore", loaded.Description);
			Assert.AreEqual ("Beach", loaded.Tags.Single ().Name);
			Assert.AreEqual (0u, untouched.Rating);
			Assert.IsEmpty (untouched.Tags);
		}

		[TestMethod]
		public void RemovePhoto_DeletesItsRowsAndLeavesTheTagAndTheOtherPhoto ()
		{
			Photo gone = AddPhoto ("gone.jpg");
			Photo stays = AddPhoto ("stays.jpg");
			Tag beach = database.Tags.CreateTag (null, "Beach", false);
			gone.AddTag (beach);
			stays.AddTag (beach);
			database.Photos.Commit (new[] { gone, stays });
			database.Exports.Create (gone.Id, gone.DefaultVersionId, ExportStore.FlickrExportType, "token");
			database.Exports.Create (stays.Id, stays.DefaultVersionId, ExportStore.FolderExportType, "folder");

			database.Photos.Remove (gone);

			Assert.IsNull (database.Photos.Get (gone.Id));
			Assert.AreEqual (stays.Id, database.Photos.Get (stays.Id).Id);
			Assert.AreEqual ("Beach", database.Tags.GetTagByName ("Beach").Name);
			Assert.AreEqual (0, Count ("photo_tags", "photo_id = " + gone.Id));
			Assert.AreEqual (0, Count ("photo_versions", "photo_id = " + gone.Id));
			Assert.AreEqual (1, Count ("photo_tags", "photo_id = " + stays.Id));
			Assert.AreEqual (0, Count ("exports", "image_id = " + gone.Id));
			Assert.AreEqual (1, Count ("exports", "image_id = " + stays.Id));
		}

		[TestMethod]
		public void RemoveTag_DeletesTheLinkAndLeavesThePhoto ()
		{
			Photo photo = AddPhoto ("kept.jpg");
			Tag beach = database.Tags.CreateTag (null, "Beach", false);
			photo.AddTag (beach);
			database.Photos.Commit (photo);

			database.Tags.Remove (beach);

			Photo loaded = database.Photos.Get (photo.Id);
			Assert.AreEqual ("kept.jpg", loaded.Name);
			Assert.IsEmpty (loaded.Tags);
			Assert.IsNull (database.Tags.GetTagByName ("Beach"));
			Assert.AreEqual (0, Count ("photo_tags"));
			Assert.AreEqual (1, Count ("photos"));
		}

		[TestMethod]
		public void CreateTag_DuplicateName_ThrowsAndLeavesTheFirst ()
		{
			database.Tags.CreateTag (null, "Beach", false);

			Assert.ThrowsExactly<SqliteException> (() => database.Tags.CreateTag (null, "Beach", false));

			Assert.AreEqual (1, Count ("tags", "name = 'Beach'"));
			Assert.AreEqual ("Beach", database.Tags.GetTagByName ("Beach").Name);
		}

		[TestMethod]
		public void RemoveCategory_WithAChild_ThrowsAndLeavesBoth ()
		{
			Category places = database.Tags.CreateCategory (null, "Shelf", false);
			database.Tags.CreateTag (places, "Paris", false);

			Assert.ThrowsExactly<InvalidTagOperationException> (() => database.Tags.Remove (places));

			Assert.AreEqual ("Shelf", database.Tags.GetTagByName ("Shelf").Name);
			Assert.AreEqual ("Paris", database.Tags.GetTagByName ("Paris").Name);
		}

		[TestMethod]
		public void HasDuplicate_MatchesTheSameFileAndNotADifferentEmptyHash ()
		{
			Photo photo = AddPhoto ("same.jpg");
			var same = new FilePhoto (photo.VersionUri (Photo.OriginalVersionId));
			string otherPath = Path.Combine (root, "other.jpg");
			File.WriteAllText (otherPath, "different");
			var other = new FilePhoto (SafeUri.FromPath (otherPath));

			Assert.IsTrue (database.Photos.HasDuplicate (same));
			Assert.IsFalse (database.Photos.HasDuplicate (other));
		}

		[TestMethod]
		public void Export_CommitAndRemove_DoNotTouchTheOtherVersion ()
		{
			Photo photo = AddPhoto ("one.jpg");
			ExportItem item = database.Exports.Create (photo.Id, photo.DefaultVersionId, ExportStore.FlickrExportType, "first");
			item.ExportToken = "second";
			database.Exports.Commit (item);

			Assert.AreEqual ("second", database.Exports.GetByImageId (photo.Id, photo.DefaultVersionId).Single ().ExportToken);
			Assert.IsEmpty (database.Exports.GetByImageId (photo.Id, photo.DefaultVersionId + 1));

			database.Exports.Remove (item);

			Assert.IsEmpty (database.Exports.GetByImageId (photo.Id, photo.DefaultVersionId));
		}

		[TestMethod]
		public void CreatePersistent_UnknownJob_ThrowsAndLeavesNoRow ()
		{
			Assert.ThrowsExactly<InvalidOperationException> (() => database.Jobs.CreatePersistent ("not-a-job", "1"));

			Assert.AreEqual (0, Count ("jobs"));
		}

		[TestMethod]
		public void Init_Reopen_RunsAJobThatWasOnlyInTheTable ()
		{
			string path = Path.Combine (root, "photos.db");
			database.Database.Execute (new DatabaseCommand (
				"INSERT INTO jobs (job_type, job_options, run_at, job_priority) VALUES (?, ?, ?, ?)",
				CalculateHashJob.JobName,
				"1",
				DateTimeUtil.FromDateTime (DateTime.Now),
				0));
			Assert.AreEqual (1, Count ("jobs"));

			database.Dispose ();
			database = Reopen (path);

			var deadline = DateTime.UtcNow.AddSeconds (5);
			while (DateTime.UtcNow < deadline && Count ("jobs") > 0)
				System.Threading.Thread.Sleep (50);

			Assert.AreEqual (0, Count ("jobs"));
		}

		[TestMethod]
		public void CreatePersistent_LegacyTypeName_BuildsTheCurrentJob ()
		{
			var legacy = database.Jobs.CreatePersistent ("FSpot.Database.Jobs.CalculateHashJob", "1");
			var current = database.Jobs.CreatePersistent (SyncMetadataJob.JobName, "2");

			Assert.IsInstanceOfType<CalculateHashJob> (legacy);
			Assert.IsInstanceOfType<SyncMetadataJob> (current);
			Assert.AreEqual (2, Count ("jobs"));
		}

		Photo AddPhoto (string name)
		{
			string path = Path.Combine (root, name);
			File.WriteAllText (path, "photo");
			return database.Photos.CreateFrom (new FilePhoto (SafeUri.FromPath (path)), true, database.Rolls.Create ().Id);
		}

		Db Reopen (string path)
		{
			var opened = new Db (new Mock<IImageFileFactory> ().Object, new Mock<IThumbnailService> ().Object, new Mock<IUpdaterUI> ().Object);
			opened.Init (path, false);
			return opened;
		}

		int Count (string table, string where = null)
		{
			string sql = "SELECT COUNT(*) FROM " + table;
			if (where != null)
				sql += " WHERE " + where;
			using var reader = database.Database.Query (sql);
			reader.Read ();
			return Convert.ToInt32 (reader[0]);
		}
	}
}
