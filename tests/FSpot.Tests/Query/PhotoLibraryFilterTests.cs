// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Core;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace FSpot.Query.Tests
{
	[TestClass]
	public class PhotoLibraryFilterTests
	{
		[TestMethod]
		[DataRow (null)]
		[DataRow ("")]
		[DataRow ("   ")]
		public void MatchesText_EmptyNeedle_AcceptsEveryPhoto (string needle)
		{
			var photo = Photo ("beach.jpg", "vacation", new DateTime (2026, 6, 15));

			Assert.IsTrue (PhotoLibraryFilter.MatchesText (photo, needle));
		}

		[TestMethod]
		public void MatchesText_Name_IsCaseInsensitive ()
		{
			var photo = Photo ("Beach.JPG", null, new DateTime (2026, 6, 15));

			Assert.IsTrue (PhotoLibraryFilter.MatchesText (photo, "beach"));
			Assert.IsFalse (PhotoLibraryFilter.MatchesText (photo, "mountain"));
		}

		[TestMethod]
		public void MatchesText_TagName_IsCaseInsensitive ()
		{
			var photo = Photo ("img.jpg", "Family", new DateTime (2026, 6, 15));

			Assert.IsTrue (PhotoLibraryFilter.MatchesText (photo, "family"));
			Assert.IsFalse (PhotoLibraryFilter.MatchesText (photo, "work"));
		}

		[TestMethod]
		public void MatchesText_Description_IsCaseInsensitive ()
		{
			var photo = Photo ("img.jpg", null, new DateTime (2026, 6, 15), "Sunset over the lake");

			Assert.IsTrue (PhotoLibraryFilter.MatchesText (photo, "LAKE"));
			Assert.IsFalse (PhotoLibraryFilter.MatchesText (photo, "ocean"));
		}

		[TestMethod]
		public void MatchesText_NullNameTagsAndDescription_DoesNotThrow ()
		{
			var photo = new Mock<IPhoto> ();
			photo.Setup (p => p.Name).Returns ((string)null);
			photo.Setup (p => p.Description).Returns ((string)null);
			photo.Setup (p => p.Tags).Returns ((Tag[])null);
			photo.Setup (p => p.Time).Returns (new DateTime (2026, 1, 1));

			Assert.IsFalse (PhotoLibraryFilter.MatchesText (photo.Object, "x"));
		}

		[TestMethod]
		public void MatchesRange_NullBounds_AcceptsEveryPhoto ()
		{
			var photo = Photo ("img.jpg", null, new DateTime (2026, 6, 15));

			Assert.IsTrue (PhotoLibraryFilter.MatchesRange (photo, null, null));
		}

		[TestMethod]
		public void MatchesRange_IncludesStartAndExcludesEnd ()
		{
			var start = new DateTime (2026, 6, 1);
			var end = new DateTime (2026, 7, 1);

			Assert.IsTrue (PhotoLibraryFilter.MatchesRange (
				Photo ("a.jpg", null, start), start, end));
			Assert.IsTrue (PhotoLibraryFilter.MatchesRange (
				Photo ("b.jpg", null, new DateTime (2026, 6, 30, 23, 59, 59)), start, end));
			Assert.IsFalse (PhotoLibraryFilter.MatchesRange (
				Photo ("c.jpg", null, end), start, end));
			Assert.IsFalse (PhotoLibraryFilter.MatchesRange (
				Photo ("d.jpg", null, start.AddTicks (-1)), start, end));
		}

		[TestMethod]
		public void MatchesTags_Or_AcceptsAPhotoWithAnyListedTag ()
		{
			var vacation = new Tag (null, 1, "Vacation");
			var work = new Tag (null, 2, "Work");
			var family = new Tag (null, 3, "Family");
			var photo = Photo ("img.jpg", "Vacation", new DateTime (2026, 6, 15));

			Assert.IsTrue (PhotoLibraryFilter.MatchesTags (photo, new[] { vacation, work }, requireAll: false));
			Assert.IsFalse (PhotoLibraryFilter.MatchesTags (photo, new[] { work, family }, requireAll: false));
		}

		[TestMethod]
		public void MatchesTags_Category_IncludesPhotosTaggedWithDescendants ()
		{
			var root = new Category (null, 1, "Root");
			var vacation = new Tag (root, 2, "Vacation");
			var photo = PhotoWithTags ("beach.jpg", new DateTime (2026, 6, 15), vacation);

			Assert.IsTrue (PhotoLibraryFilter.MatchesTags (photo, new[] { root }, requireAll: false));
			Assert.IsFalse (PhotoLibraryFilter.MatchesTags (photo, new[] { new Tag (null, 9, "Work") }, requireAll: false));
		}

		[TestMethod]
		public void MatchesTags_And_RequiresEveryListedTag ()
		{
			var vacation = new Tag (null, 1, "Vacation");
			var family = new Tag (null, 3, "Family");
			var photo = PhotoWithTags ("img.jpg", new DateTime (2026, 6, 15), vacation, family);

			Assert.IsTrue (PhotoLibraryFilter.MatchesTags (photo, new[] { vacation, family }, requireAll: true));
			Assert.IsFalse (PhotoLibraryFilter.MatchesTags (photo, new[] { vacation, new Tag (null, 2, "Work") }, requireAll: true));
		}

		[TestMethod]
		public void MatchesRating_HonorsInclusiveBounds ()
		{
			var three = Photo ("a.jpg", null, new DateTime (2026, 6, 15), rating: 3);
			Assert.IsTrue (PhotoLibraryFilter.MatchesRating (three, 3, 5));
			Assert.IsTrue (PhotoLibraryFilter.MatchesRating (three, 1, 3));
			Assert.IsFalse (PhotoLibraryFilter.MatchesRating (three, 4, 5));
			Assert.IsTrue (PhotoLibraryFilter.MatchesRating (three, null, null));
		}

		[TestMethod]
		public void MatchesRoll_HonorsMembership ()
		{
			Assert.IsTrue (PhotoLibraryFilter.MatchesRoll (7, new uint[] { 7, 9 }));
			Assert.IsFalse (PhotoLibraryFilter.MatchesRoll (3, new uint[] { 7, 9 }));
			Assert.IsTrue (PhotoLibraryFilter.MatchesRoll (3, Array.Empty<uint> ()));
		}

		[TestMethod]
		public void Matches_LibraryFind_AndVsOrRatingDateAndRollEachChangeTheSet ()
		{
			var vacation = new Tag (null, 1, "Vacation");
			var work = new Tag (null, 2, "Work");
			var juneVacation = PhotoWithTags ("beach.jpg", new DateTime (2026, 6, 15), vacation);
			var julyWork = PhotoWithTags ("desk.jpg", new DateTime (2026, 7, 2), work);
			SetupRating (juneVacation, 5);
			SetupRating (julyWork, 2);

			var orFind = new LibraryFind {
				Tags = new[] { vacation, work },
				RequireAllTags = false
			};
			Assert.IsTrue (PhotoLibraryFilter.Matches (juneVacation, orFind, rollId: 1));
			Assert.IsTrue (PhotoLibraryFilter.Matches (julyWork, orFind, rollId: 2));

			var andFind = new LibraryFind {
				Tags = new[] { vacation, work },
				RequireAllTags = true
			};
			Assert.IsFalse (PhotoLibraryFilter.Matches (juneVacation, andFind, rollId: 1));

			var rated = new LibraryFind { MinRating = 4, MaxRating = 5 };
			Assert.IsTrue (PhotoLibraryFilter.Matches (juneVacation, rated, rollId: 1));
			Assert.IsFalse (PhotoLibraryFilter.Matches (julyWork, rated, rollId: 2));

			var june = new LibraryFind {
				Start = new DateTime (2026, 6, 1),
				End = new DateTime (2026, 7, 1)
			};
			Assert.IsTrue (PhotoLibraryFilter.Matches (juneVacation, june, rollId: 1));
			Assert.IsFalse (PhotoLibraryFilter.Matches (julyWork, june, rollId: 2));

			var roll = new LibraryFind { RollIds = new uint[] { 1 } };
			Assert.IsTrue (PhotoLibraryFilter.Matches (juneVacation, roll, rollId: 1));
			Assert.IsFalse (PhotoLibraryFilter.Matches (julyWork, roll, rollId: 2));
		}

		[TestMethod]
		public void Matches_LibraryFind_EmptySetIsDistinguishable ()
		{
			var photo = Photo ("img.jpg", "Work", new DateTime (2026, 1, 1), rating: 1);
			var find = new LibraryFind { MinRating = 5, MaxRating = 5 };
			Assert.IsFalse (PhotoLibraryFilter.Matches (photo, find, rollId: 1));
		}

		[TestMethod]
		public void Matches_TextAndRange_MustBothPass ()
		{
			var start = new DateTime (2026, 6, 1);
			var end = new DateTime (2026, 7, 1);
			var juneBeach = Photo ("beach.jpg", "vacation", new DateTime (2026, 6, 15));
			var julyBeach = Photo ("beach.jpg", "vacation", new DateTime (2026, 7, 2));
			var juneWork = Photo ("desk.jpg", "work", new DateTime (2026, 6, 15));

			Assert.IsTrue (PhotoLibraryFilter.Matches (juneBeach, "beach", start, end));
			Assert.IsFalse (PhotoLibraryFilter.Matches (julyBeach, "beach", start, end));
			Assert.IsFalse (PhotoLibraryFilter.Matches (juneWork, "beach", start, end));
		}

		static IPhoto Photo (string name, string tag, DateTime time, string description = null, uint rating = 0)
			=> PhotoWithTags (name, time, description, rating,
				tag == null ? Array.Empty<Tag> () : new[] { new Tag (null, 1, tag) });

		static IPhoto PhotoWithTags (string name, DateTime time, params Tag[] tags)
			=> PhotoWithTags (name, time, null, 0, tags);

		static IPhoto PhotoWithTags (string name, DateTime time, string description, uint rating, params Tag[] tags)
		{
			var mock = new Mock<IPhoto> ();
			mock.Setup (p => p.Name).Returns (name);
			mock.Setup (p => p.Description).Returns (description);
			mock.Setup (p => p.Time).Returns (time);
			mock.Setup (p => p.Rating).Returns (rating);
			mock.Setup (p => p.Tags).Returns (tags ?? Array.Empty<Tag> ());
			return mock.Object;
		}

		static void SetupRating (IPhoto photo, uint rating)
			=> Mock.Get (photo).Setup (p => p.Rating).Returns (rating);
	}
}
