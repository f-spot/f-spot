// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Core;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Query.Tests
{
	[TestClass]
	public class QueryConditionTests
	{
		[TestCleanup]
		public void ResetHiddenTag ()
		{
			HiddenTag.Tag = null;
		}

		[TestMethod]
		[DataRow (2026, 1, 2026, 2)]
		[DataRow (2026, 12, 2027, 1)]
		public void DateRange_MonthConstructor_EndsOnTheFirstOfTheNextMonth (int year, int month, int endYear, int endMonth)
		{
			var range = new DateRange (year, month);

			Assert.AreEqual (new DateTime (year, month, 1), range.Start);
			Assert.AreEqual (new DateTime (endYear, endMonth, 1), range.End);
			Assert.AreEqual (
				$" photos.time >= {DateTimeUtil.FromDateTime (range.Start)} AND photos.time <= {DateTimeUtil.FromDateTime (range.End)} ",
				range.SqlClause ());
		}

		[TestMethod]
		public void RatingRange_MinOnly_ExtendsToUintMax ()
		{
			var range = new RatingRange (3);

			Assert.AreEqual (3u, range.MinRating);
			Assert.AreEqual (uint.MaxValue, range.MaxRating);
			Assert.AreEqual (" photos.rating >= 3 AND photos.rating <= 4294967295 ", range.SqlClause ());
		}

		[TestMethod]
		public void RatingRange_MinAndMax_WritesBothBounds ()
		{
			Assert.AreEqual (" photos.rating >= 1 AND photos.rating <= 4 ", new RatingRange (1, 4).SqlClause ());
		}

		[TestMethod]
		public void UntaggedCondition_ExcludesRowsThatHaveTags ()
		{
			Assert.AreEqual (" photos.id NOT IN (SELECT DISTINCT photo_id FROM photo_tags) ",
				new UntaggedCondition ().SqlClause ());
		}

		[TestMethod]
		public void FolderSet_Empty_ReturnsNull ()
		{
			Assert.IsNull (new FolderSet ().SqlClause ());
		}

		[TestMethod]
		public void FolderSet_EscapesQuotesInTheFolderUri ()
		{
			var folders = new FolderSet {
				Folders = new[] { new SafeUri ("file:///photos/O'Brien", true) }
			};

			string sql = folders.SqlClause ();
			Assert.Contains ("base_uri = 'file:///photos/O''Brien'", sql);
			Assert.Contains ("base_uri LIKE 'file:///photos/O''Brien/%'", sql);
			Assert.DoesNotContain ("O'Brien%", sql);
		}

		[TestMethod]
		public void FolderSet_TrailingSlash_MatchesTheFolderItselfAndItsChildren ()
		{
			var folders = new FolderSet {
				Folders = new[] { new SafeUri ("file:///photos/trip/", true) }
			};

			string sql = folders.SqlClause ();
			Assert.Contains ("base_uri = 'file:///photos/trip'", sql);
			Assert.Contains ("base_uri LIKE 'file:///photos/trip/%'", sql);
		}

		[TestMethod]
		public void AndOperator_FlattensNestedAndsAndJoinsWithAnd ()
		{
			var t1 = new TagTerm (new Tag (null, 1, "one"));
			var t2 = new TagTerm (new Tag (null, 2, "two"));
			var t3 = new TagTerm (new Tag (null, 3, "three"));
			var combined = new AndOperator (new AndOperator (t1, t2), t3);

			Assert.HasCount (3, combined.Terms);
			string sql = combined.SqlClause ();
			Assert.Contains (" AND ", sql);
			Assert.Contains ("tag_id = 1", sql);
			Assert.Contains ("tag_id = 2", sql);
			Assert.Contains ("tag_id = 3", sql);
		}

		[TestMethod]
		public void NotTerm_NegatesTheInnerClause ()
		{
			LogicalTerm inner = new TagTerm (new Tag (null, 7, "hidden"));
			string sql = new NotTerm (inner).SqlClause ();

			Assert.StartsWith (" NOT (", sql);
			Assert.Contains (inner.SqlClause (), sql);
		}

		[TestMethod]
		public void HiddenTag_Hide_OmitsTheHiddenTagWhenItIsSet ()
		{
			Assert.IsNull (HiddenTag.HideHiddenTag.SqlClause ());

			HiddenTag.Tag = new Tag (null, 42, "Hidden");
			Assert.AreEqual (" photos.id NOT IN (SELECT photo_id FROM photo_tags WHERE tag_id = 42) ",
				HiddenTag.HideHiddenTag.SqlClause ());
			Assert.IsNull (HiddenTag.ShowHiddenTag.SqlClause ());
		}

		[TestMethod]
		public void OrderByTime_WritesDirectionAndFilenameTiebreaker ()
		{
			Assert.AreEqual (" time ASC, filename ASC ", OrderByTime.OrderByTimeAsc.SqlClause ());
			Assert.AreEqual (" time DESC, filename ASC ", OrderByTime.OrderByTimeDesc.SqlClause ());
		}

		[TestMethod]
		public void TextTerm_SearchMultiple_OrsEachField ()
		{
			var search = TextTerm.SearchMultiple ("cat", "name", "description");

			Assert.HasCount (2, search.Terms);
			string sql = search.SqlClause ();
			Assert.Contains (" name LIKE %cat% ", sql);
			Assert.Contains (" description LIKE %cat% ", sql);
			Assert.Contains (" OR ", sql);
		}
	}
}
