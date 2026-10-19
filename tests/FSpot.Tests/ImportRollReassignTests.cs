// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Utils;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	[TestClass]
	public sealed class ImportRollReassignTests
	{
		[TestMethod]
		public void Assign_DatesTheRollFromTheNewestPhotoAndMovesEveryPhotoOntoIt ()
		{
			Photo older = PhotoAt ("/photos/older.jpg");
			older.Time = new DateTime (2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			older.RollId = 4;
			older.Changes.RollIdChanged = false;
			Photo newer = PhotoAt ("/photos/newer.jpg");
			newer.Time = new DateTime (2020, 6, 1, 12, 0, 0, DateTimeKind.Utc);
			newer.RollId = 9;
			newer.Changes.RollIdChanged = false;
			DateTime created = default;
			int calls = 0;

			bool assigned = ImportRollReassign.TryAssign (new[] { older, newer }, time => {
				calls++;
				created = time;
				return 15;
			});

			Assert.IsTrue (assigned);
			Assert.AreEqual (1, calls);
			Assert.AreEqual (newer.Time, created);
			Assert.AreEqual (15u, older.RollId);
			Assert.AreEqual (15u, newer.RollId);
			Assert.IsTrue (older.Changes.RollIdChanged);
			Assert.IsTrue (newer.Changes.RollIdChanged);
		}

		[TestMethod]
		public void Assign_KeepsTheFirstTimeWhenTwoPhotosShareIt ()
		{
			Photo first = PhotoAt ("/photos/first.jpg");
			DateTime shared = new DateTime (2020, 3, 1, 0, 0, 0, DateTimeKind.Utc);
			first.Time = shared;
			Photo second = PhotoAt ("/photos/second.jpg");
			second.Time = shared;
			DateTime created = default;

			ImportRollReassign.TryAssign (new[] { first, second }, time => {
				created = time;
				return 8;
			});

			Assert.AreEqual (shared, created);
			Assert.AreEqual (8u, first.RollId);
			Assert.AreEqual (8u, second.RollId);
		}

		[TestMethod]
		public void Assign_LeavesPhotosAloneWhenNothingIsSelected ()
		{
			int calls = 0;

			bool assigned = ImportRollReassign.TryAssign (Array.Empty<Photo> (), time => {
				calls++;
				return 1;
			});

			Assert.IsFalse (assigned);
			Assert.AreEqual (0, calls);
			Assert.IsFalse (ImportRollReassign.TryAssign (null, time => 1));
		}

		static Photo PhotoAt (string absolutePath)
		{
			SafeUri uri = SafeUri.FromPath (absolutePath);
			var photo = new Photo (null, null, 1, DateTimeUtil.FromDateTime (new DateTime (2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
			photo.AddVersionUnsafely (Photo.OriginalVersionId, uri.GetBaseUri (), uri.GetFilename (), string.Empty, "Original", true);
			return photo;
		}
	}
}
