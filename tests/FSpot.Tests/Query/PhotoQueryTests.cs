// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using FSpot.Core;
using FSpot.Database;
using FSpot.Query;

using Hyena;

using Mocks;

using Moq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Query.Tests
{
	[TestClass]
	public sealed class PhotoQueryTests
	{
		string database;

		[TestInitialize]
		public void Setup ()
		{
			database = Path.Combine (Path.GetTempPath (), $"f-spot-photoquery-{Guid.NewGuid ()}.db");
		}

		[TestCleanup]
		public void Cleanup ()
		{
			if (database != null && File.Exists (database))
				File.Delete (database);
			HiddenTag.Tag = null;
		}

		[TestMethod]
		public void SameTimeAndFilename_OrdersByIdDescending ()
		{
			WithStore (store => {
				var time = new DateTime (2020, 1, 1);
				store.CreateFrom (Named ("/a/same.jpg", time), true, 1);
				store.CreateFrom (Named ("/b/same.jpg", time), true, 1);
				var query = new PhotoQuery (store);

				Assert.IsGreaterThan (((Photo)query[1]).Id, ((Photo)query[0]).Id);
			});
		}

		[TestMethod]
		public void APagePastTheFirstHundred_ReturnsThatPhoto ()
		{
			WithStore (store => {
				var time = new DateTime (2020, 3, 1);
				for (int i = 0; i < 250; i++)
					store.CreateFrom (Named ($"/photos/p{i:000}.jpg", time), true, 1);
				var query = new PhotoQuery (store);

				Assert.AreEqual ("p000.jpg", query[0].Name);
				Assert.AreEqual ("p100.jpg", query[100].Name);
				Assert.AreEqual ("p249.jpg", query[249].Name);
				Assert.AreEqual (2, query.CachedPageCount);
			});
		}

		[TestMethod]
		public void JumpingTwoPages_DropsThePageLeftBehind ()
		{
			WithStore (store => {
				var time = new DateTime (2020, 3, 1);
				for (int i = 0; i < 250; i++)
					store.CreateFrom (Named ($"/photos/p{i:000}.jpg", time), true, 1);
				var query = new PhotoQuery (store);

				_ = query[0];
				_ = query[200];

				Assert.AreEqual (1, query.CachedPageCount);
				Assert.AreEqual ("p200.jpg", query[200].Name);
			});
		}

		[TestMethod]
		public void LookupItem_UsesTheOrderedIdList ()
		{
			WithStore (store => {
				store.CreateFrom (Named ("/photos/jan.jpg", new DateTime (2020, 1, 15)), true, 1);
				store.CreateFrom (Named ("/photos/jun.jpg", new DateTime (2020, 6, 15)), true, 1);
				var query = new PhotoQuery (store);

				int index = query.LookupItem (new DateTime (2020, 6, 15));

				Assert.AreEqual ("jun.jpg", query[index].Name);
			});
		}

		[TestMethod]
		public void NewStore_IndexesTheLibraryOrderAndTagLookup ()
		{
			WithStore (store => {
				var names = new List<string> ();
				using var peek = new FSpotDatabaseConnection (database);
				using (var reader = peek.Query ("SELECT name FROM sqlite_master WHERE type = 'index'")) {
					while (reader.Read ())
						names.Add (reader["name"].ToString ());
				}

				Assert.Contains ("idx_photo_tags_tag", names);
				Assert.Contains ("idx_photos_time_desc", names);
				Assert.Contains ("idx_photos_time_asc", names);
			});
		}

		[TestMethod]
		public void PageClause_UsesTheOrderedRowId ()
		{
			Assert.AreEqual (" WHERE ids.rowid >= 201 AND ids.rowid < 301", PhotoStore.PageClause (200, 100));
			Assert.AreEqual ("", PhotoStore.PageClause (0, -1));
		}

		[TestMethod]
		public void Count_EmptyStore_IsZero ()
		{
			WithStore (store => {
				var query = new PhotoQuery (store);
				Assert.AreEqual (0, query.Count);
			});
		}

		[TestMethod]
		public void Count_StaysStaleUntilRequestReloadAfterInsert ()
		{
			WithStore (store => {
				var query = new PhotoQuery (store);
				store.CreateFrom (Named ("/photos/a.jpg", new DateTime (2020, 1, 1)), true, 1);

				Assert.AreEqual (0, query.Count);

				query.RequestReload ();
				Assert.AreEqual (1, query.Count);
			});
		}

		[TestMethod]
		public void Range_KeepsOnlyPhotosInsideTheBounds ()
		{
			WithStore (store => {
				store.CreateFrom (Named ("/photos/jan.jpg", new DateTime (2020, 1, 15)), true, 1);
				store.CreateFrom (Named ("/photos/jun.jpg", new DateTime (2020, 6, 15)), true, 1);
				var query = new PhotoQuery (store);

				query.Range = new DateRange (2020, 6);

				Assert.AreEqual (1, query.Count);
				Assert.AreEqual ("jun.jpg", query[0].Name);
			});
		}

		[TestMethod]
		public void RatingRange_FiltersCommittedRatings ()
		{
			WithStore (store => {
				var low = store.CreateFrom (Named ("/photos/low.jpg", new DateTime (2020, 1, 1)), true, 1);
				var high = store.CreateFrom (Named ("/photos/high.jpg", new DateTime (2020, 1, 2)), true, 1);
				low.Rating = 1;
				high.Rating = 5;
				store.Commit (new[] { low, high });

				var query = new PhotoQuery (store) {
					RatingRange = new RatingRange (4, 5)
				};

				Assert.AreEqual (1, query.Count);
				Assert.AreEqual ("high.jpg", query[0].Name);
			});
		}

		[TestMethod]
		public void Terms_ApplyAsExtraSql ()
		{
			WithStore (store => {
				store.CreateFrom (Named ("/photos/a.jpg", new DateTime (2020, 1, 1)), true, 1);
				var query = new PhotoQuery (store);
				Assert.AreEqual (1, query.Count);

				query.Terms = new ConditionWrapper (" photos.id < 0 ");
				Assert.AreEqual (0, query.Count);

				query.Terms = null;
				Assert.AreEqual (1, query.Count);
			});
		}

		[TestMethod]
		public void TimeOrderAsc_PutsEarlierPhotosFirst ()
		{
			WithStore (store => {
				store.CreateFrom (Named ("/photos/later.jpg", new DateTime (2020, 6, 1)), true, 1);
				store.CreateFrom (Named ("/photos/earlier.jpg", new DateTime (2020, 1, 1)), true, 1);
				var query = new PhotoQuery (store);

				Assert.AreEqual ("later.jpg", query[0].Name);

				query.TimeOrderAsc = true;
				Assert.AreEqual ("earlier.jpg", query[0].Name);
				Assert.AreEqual ("later.jpg", query[1].Name);
				Assert.IsTrue (query.TimeOrderAsc);
			});
		}

		[TestMethod]
		public void Range_ClearsWhenSetToNull ()
		{
			WithStore (store => {
				store.CreateFrom (Named ("/photos/jan.jpg", new DateTime (2020, 1, 15)), true, 1);
				var query = new PhotoQuery (store) {
					Range = new DateRange (2020, 6)
				};
				Assert.AreEqual (0, query.Count);

				query.Range = null;
				Assert.AreEqual (1, query.Count);
			});
		}

		[TestMethod]
		public void SetCondition_Null_Throws ()
		{
			WithStore (store => {
				var query = new PhotoQuery (store);
				var ex = Assert.ThrowsExactly<ArgumentNullException> (() => query.SetCondition (null));
				Assert.AreEqual ("condition", ex.ParamName);
			});
		}

		[TestMethod]
		public void SetCondition_SameInstance_ReturnsFalse ()
		{
			WithStore (store => {
				var query = new PhotoQuery (store);
				Assert.IsFalse (query.SetCondition (OrderByTime.OrderByTimeDesc));
			});
		}

		[TestMethod]
		public void UnSetCondition_MissingType_ReturnsFalse ()
		{
			WithStore (store => {
				var query = new PhotoQuery (store);
				Assert.IsFalse (query.UnSetCondition<RatingRange> ());
			});
		}

		[TestMethod]
		public void Contains_AndIndexOf_UseTheTempTable ()
		{
			WithStore (store => {
				var photo = store.CreateFrom (Named ("/photos/a.jpg", new DateTime (2020, 1, 1)), true, 1);
				var query = new PhotoQuery (store);
				query.RequestReload ();

				Assert.IsTrue (query.Contains (photo));
				Assert.AreEqual (0, query.IndexOf (photo));
				Assert.IsFalse (query.Contains (PhotoMock.CreateNamed (new SafeUri ("file:///missing.jpg"), "missing")));
			});
		}

		[TestMethod]
		public void Items_Throws ()
		{
			WithStore (store => {
				var query = new PhotoQuery (store);
#pragma warning disable CS0618
				Assert.ThrowsExactly<NotImplementedException> (() => _ = query.Items);
#pragma warning restore CS0618
			});
		}

		[TestMethod]
		public void RequestReload_RaisesChanged ()
		{
			WithStore (store => {
				var query = new PhotoQuery (store);
				int fires = 0;
				query.Changed += _ => fires++;

				query.RequestReload ();
				Assert.AreEqual (1, fires);
			});
		}

		[TestMethod]
		public void FolderSet_Empty_DoesNotHidePhotos ()
		{
			WithStore (store => {
				store.CreateFrom (Named ("/photos/a.jpg", new DateTime (2020, 1, 1)), true, 1);
				var query = new PhotoQuery (store) {
					FolderSet = new FolderSet ()
				};
				Assert.AreEqual (1, query.Count);
			});
		}

		void WithStore (Action<PhotoStore> body)
		{
			using var connection = new FSpotDatabaseConnection (database);
			var db = new Mock<IDb> ();
			db.Setup (m => m.Database).Returns (connection);
			body (new PhotoStore (null, null, db.Object, true));
		}

		static IPhoto Named (string path, DateTime time)
		{
			var uri = new SafeUri ("file://" + path);
			var version = new Mock<IPhotoVersion> ();
			version.Setup (v => v.Uri).Returns (uri);
			version.Setup (v => v.BaseUri).Returns (uri);
			version.Setup (v => v.Filename).Returns (Path.GetFileName (path));
			version.Setup (v => v.Name).Returns ("Original");
			version.Setup (v => v.ImportMD5).Returns (string.Empty);

			var photo = new Mock<IPhoto> ();
			photo.Setup (p => p.DefaultVersion).Returns (version.Object);
			photo.Setup (p => p.Versions).Returns (new[] { version.Object });
			photo.Setup (p => p.Time).Returns (time);
			photo.Setup (p => p.Name).Returns (Path.GetFileName (path));
			photo.Setup (p => p.Description).Returns (string.Empty);
			return photo.Object;
		}
	}
}
