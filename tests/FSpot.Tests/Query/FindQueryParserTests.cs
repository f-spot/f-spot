// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Core;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace FSpot.Query.Tests
{
	[TestClass]
	public class FindQueryParserTests
	{
		static readonly Tag Beach = new Tag (null, 1, "Beach");
		static readonly Tag Family = new Tag (null, 2, "Family");
		static readonly Tag Work = new Tag (null, 3, "Work");

		static Tag ByName (string name)
		{
			foreach (var tag in new[] { Beach, Family, Work }) {
				if (string.Equals (tag.Name, name.Trim (), StringComparison.OrdinalIgnoreCase))
					return tag;
			}
			return null;
		}

		static FindExpression Parse (string text)
		{
			Assert.IsTrue (FindQueryParser.TryParse (text, ByName, out var expression), $"'{text}' should parse");
			return expression;
		}

		[TestMethod]
		[DataRow (null)]
		[DataRow ("")]
		[DataRow ("   ")]
		public void TryParse_Blank_IsValidAndEmpty (string text)
			=> Assert.IsNull (Parse (text));

		[TestMethod]
		public void TryParse_SingleWord_MatchesTagOrText ()
		{
			var expression = Parse ("beach");

			Assert.IsTrue (expression.Matches (Photo ("a.jpg", Beach)));
			Assert.IsTrue (expression.Matches (Photo ("my-beach.jpg")));
			Assert.IsFalse (expression.Matches (Photo ("desk.jpg", Work)));
		}

		[TestMethod]
		public void TryParse_Or_MatchesEither ()
		{
			var expression = Parse ("beach or work");

			Assert.IsInstanceOfType<FindOr> (expression);
			Assert.IsTrue (expression.Matches (Photo ("a.jpg", Beach)));
			Assert.IsTrue (expression.Matches (Photo ("b.jpg", Work)));
			Assert.IsFalse (expression.Matches (Photo ("c.jpg", Family)));
		}

		[TestMethod]
		[DataRow ("beach and family")]
		[DataRow ("beach, family")]
		[DataRow ("Beach AND Family")]
		public void TryParse_AndOrComma_RequiresBoth (string text)
		{
			var expression = Parse (text);

			Assert.IsInstanceOfType<FindAnd> (expression);
			Assert.IsTrue (expression.Matches (Photo ("a.jpg", Beach, Family)));
			Assert.IsFalse (expression.Matches (Photo ("b.jpg", Beach)));
		}

		[TestMethod]
		public void TryParse_Parentheses_GroupMixedOperators ()
		{
			var expression = Parse ("(beach or work) and family");

			Assert.IsTrue (expression.Matches (Photo ("a.jpg", Beach, Family)));
			Assert.IsTrue (expression.Matches (Photo ("b.jpg", Work, Family)));
			Assert.IsFalse (expression.Matches (Photo ("c.jpg", Beach)));
		}

		[TestMethod]
		public void TryParse_Not_NegatesTheGroup ()
		{
			var expression = Parse ("not (work)");

			Assert.IsTrue (expression.Matches (Photo ("a.jpg", Beach)));
			Assert.IsFalse (expression.Matches (Photo ("b.jpg", Work)));
		}

		[TestMethod]
		public void TryParse_NotCombined_ExcludesOnlyThatTerm ()
		{
			var expression = Parse ("beach and not (family)");

			Assert.IsTrue (expression.Matches (Photo ("a.jpg", Beach)));
			Assert.IsFalse (expression.Matches (Photo ("b.jpg", Beach, Family)));
			Assert.IsFalse (expression.Matches (Photo ("c.jpg", Work)));
		}

		[TestMethod]
		[DataRow ("beach and family or work")]
		[DataRow ("(beach")]
		[DataRow ("beach)")]
		[DataRow (")beach(")]
		public void TryParse_InvalidQuery_Fails (string text)
			=> Assert.IsFalse (FindQueryParser.TryParse (text, ByName, out _));

		[TestMethod]
		public void TryParse_CategoryTag_MatchesDescendants ()
		{
			var places = new Category (null, 10, "Places");
			var paris = new Tag (places, 11, "Paris");

			Assert.IsTrue (FindQueryParser.TryParse ("places", name => places, out var expression));
			Assert.IsTrue (expression.Matches (Photo ("a.jpg", paris)));
		}

		static IPhoto Photo (string name, params Tag[] tags)
		{
			var mock = new Mock<IPhoto> ();
			mock.Setup (p => p.Name).Returns (name);
			mock.Setup (p => p.Description).Returns (string.Empty);
			mock.Setup (p => p.Tags).Returns (tags);
			return mock.Object;
		}
	}

	[TestClass]
	public class FindEntryTextTests
	{
		[TestMethod]
		public void CloseParens_OpenParen_InsertsCloseAfterIt ()
			=> Assert.AreEqual ("beach and ()", FindEntryText.CloseParens ("beach and (", 11));

		[TestMethod]
		public void CloseParens_Balanced_LeavesTextAlone ()
			=> Assert.AreEqual ("(a)", FindEntryText.CloseParens ("(a)", 2));

		[TestMethod]
		public void CloseParens_InsideGroup_InsertsAtCaret ()
			=> Assert.AreEqual ("(a())", FindEntryText.CloseParens ("(a()", 3));

		[TestMethod]
		public void TabIntoParens_AtEnd_LetsFocusMove ()
			=> Assert.IsNull (FindEntryText.TabIntoParens ("beach", 5));

		[TestMethod]
		public void TabIntoParens_BeforeGroup_LandsAfterOpenParen ()
			=> Assert.AreEqual (7, FindEntryText.TabIntoParens ("a and (b)", 0));

		[TestMethod]
		public void TabIntoParens_NoParen_GoesToEnd ()
			=> Assert.AreEqual (5, FindEntryText.TabIntoParens ("beach", 1));
	}

	[TestClass]
	public class TagQueryTreeTests
	{
		static readonly Tag Beach = new Tag (null, 1, "Beach");
		static readonly Tag Family = new Tag (null, 2, "Family");
		static readonly Tag Work = new Tag (null, 3, "Work");

		[TestMethod]
		public void Empty_MatchesEverything ()
			=> Assert.IsTrue (new TagQueryTree ().Matches (Photo ()));

		[TestMethod]
		public void Include_SeparateDrops_AreOred ()
		{
			var query = new TagQueryTree ();
			query.Include (new[] { Beach });
			query.Include (new[] { Work });

			Assert.HasCount (2, query.Groups);
			Assert.IsTrue (query.Matches (Photo (Beach)));
			Assert.IsTrue (query.Matches (Photo (Work)));
			Assert.IsFalse (query.Matches (Photo (Family)));
		}

		[TestMethod]
		public void Include_SkipsTagsAlreadyInTheQuery ()
		{
			var query = new TagQueryTree ();
			query.Include (new[] { Beach });

			Assert.IsEmpty (query.Include (new[] { Beach }));
			Assert.HasCount (1, query.Groups);
		}

		[TestMethod]
		public void Insert_AfterLiteral_AndsWithItsGroup ()
		{
			var query = new TagQueryTree ();
			var beach = query.Include (new[] { Beach })[0];
			query.Insert (new[] { Family }, beach);

			Assert.HasCount (1, query.Groups);
			Assert.IsTrue (query.Matches (Photo (Beach, Family)));
			Assert.IsFalse (query.Matches (Photo (Beach)));
		}

		[TestMethod]
		public void Insert_SameTagIntoGroupTwice_IsIgnored ()
		{
			var query = new TagQueryTree ();
			var beach = query.Include (new[] { Beach })[0];

			Assert.IsEmpty (query.Insert (new[] { Beach }, beach));
		}

		[TestMethod]
		public void ToggleNegated_ExcludesTheTag ()
		{
			var query = new TagQueryTree ();
			var work = query.Include (new[] { Work })[0];
			query.ToggleNegated (work);

			Assert.IsTrue (work.IsNegated);
			Assert.IsFalse (query.Matches (Photo (Work)));
			Assert.IsTrue (query.Matches (Photo (Beach)));
		}

		[TestMethod]
		public void Remove_LastLiteral_DropsTheGroup ()
		{
			var query = new TagQueryTree ();
			var beach = query.Include (new[] { Beach })[0];
			int changes = 0;
			query.Changed += (_, _) => changes++;

			query.Remove (beach);

			Assert.IsTrue (query.IsEmpty);
			Assert.AreEqual (1, changes);
		}

		[TestMethod]
		public void Move_IntoAnotherGroup_KeepsNegation ()
		{
			var query = new TagQueryTree ();
			var beach = query.Include (new[] { Beach })[0];
			var work = query.Include (new[] { Work })[0];
			query.ToggleNegated (work);

			query.Move (work, beach);

			Assert.HasCount (1, query.Groups);
			var moved = query.Groups[0].Literals[1];
			Assert.AreEqual (Work.Id, moved.Tag.Id);
			Assert.IsTrue (moved.IsNegated);
			Assert.IsTrue (query.Matches (Photo (Beach)));
			Assert.IsFalse (query.Matches (Photo (Beach, Work)));
		}

		[TestMethod]
		public void Move_ToTheBar_StartsANewGroup ()
		{
			var query = new TagQueryTree ();
			var beach = query.Include (new[] { Beach })[0];
			var family = query.Insert (new[] { Family }, beach)[0];

			query.Move (family, null);

			Assert.HasCount (2, query.Groups);
			Assert.IsTrue (query.Matches (Photo (Family)));
		}

		[TestMethod]
		public void RemoveTag_DropsEveryLiteralOfTheTag ()
		{
			var query = new TagQueryTree ();
			var beach = query.Include (new[] { Beach })[0];
			query.Insert (new[] { Work }, beach);
			query.Insert (new[] { Work }, null);

			query.RemoveTag (Work);

			Assert.IsFalse (query.TagIncluded (Work));
			Assert.HasCount (1, query.Groups);
		}

		[TestMethod]
		public void LibraryFilter_AppliesMatchers ()
		{
			var query = new TagQueryTree ();
			query.Include (new[] { Beach });
			var find = new LibraryFind { Matchers = new List<IPhotoMatcher> { query } };

			Assert.IsTrue (PhotoLibraryFilter.Matches (Photo (Beach), find, 0));
			Assert.IsFalse (PhotoLibraryFilter.Matches (Photo (Work), find, 0));
		}

		static IPhoto Photo (params Tag[] tags)
		{
			var mock = new Mock<IPhoto> ();
			mock.Setup (p => p.Name).Returns ("a.jpg");
			mock.Setup (p => p.Tags).Returns (tags);
			return mock.Object;
		}
	}
}
