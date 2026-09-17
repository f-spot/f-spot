// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Resources.Lang;
using FSpot.Widgets;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Query.Tests
{
	[TestClass]
	public sealed class CompletionLogicTests
	{
		[TestMethod]
		[DataRow ("T", 0, true)]
		[DataRow ("t", 0, true)]
		[DataRow ("Ta", 1, true)]
		[DataRow ("tA", 1, true)]
		[DataRow ("tagnam", 5, true)]
		[DataRow ("Tagnam", 3, true)]
		[DataRow ("", 0, false)]
		[DataRow ("a", 0, false)]
		[DataRow ("tagname", 6, false)]
		[DataRow ("Tagname ", 7, false)]
		public void MatchFunc_ComparesOnlyThePrefixOfTheTagName (string key, int pos, bool expected)
		{
			Assert.AreEqual (expected, new CompletionLogic ().MatchFunc ("Tagname", key, pos));
		}

		[TestMethod]
		[DataRow ("àáâãäåa", "àáâãäå", 5)]
		[DataRow ("ÒÓÔÕÖØO", "òóôõöø", 5)]
		[DataRow ("àáâãäåa", "aaaaaa", 5)]
		[DataRow ("ÒÓÔÕÖØO", "oooooo", 5)]
		public void MatchFunc_FoldsDiacriticsInTheName (string name, string key, int pos)
		{
			Assert.IsTrue (new CompletionLogic ().MatchFunc (name, key, pos));
		}

		[TestMethod]
		public void MatchFunc_AllowsSpacesInsideTheNameButNotAMidWordToken ()
		{
			var logic = new CompletionLogic ();

			Assert.IsTrue (logic.MatchFunc ("Tag with spaces", "Tag with", 7));
			Assert.IsFalse (logic.MatchFunc ("Tag with spaces", "Tag with spaces", 14));
			Assert.IsFalse (logic.MatchFunc ("Tag with spaces", "wit", 2));
		}

		[TestMethod]
		public void MatchFunc_IgnoresParenthesesAroundTheToken ()
		{
			var logic = new CompletionLogic ();

			Assert.IsTrue (logic.MatchFunc ("Tagname", "(T", 1));
			Assert.IsTrue (logic.MatchFunc ("Tagname", "(Ta)", 2));
		}

		[TestMethod]
		public void MatchFunc_SplitsOnLocalizedAndOrOperators ()
		{
			var logic = new CompletionLogic ();
			string andQuery = $"XY {Strings.And} T";
			string orQuery = $"XY {Strings.Or} T";
			string between = $"XY {Strings.And} T {Strings.And} AB";

			Assert.IsTrue (logic.MatchFunc ("Tagname", andQuery, andQuery.IndexOf ('T')));
			Assert.IsTrue (logic.MatchFunc ("Tagname", $"T {Strings.And} XY", 0));
			Assert.IsTrue (logic.MatchFunc ("Tagname", orQuery, orQuery.IndexOf ('T')));
			Assert.IsTrue (logic.MatchFunc ("Tagname", $"T {Strings.Or} XY", 0));
			Assert.IsTrue (logic.MatchFunc ("Tagname", between, between.IndexOf ('T')));
		}

		[TestMethod]
		public void ReplaceKey_ReplacesOnlyTheCurrentToken ()
		{
			var logic = new CompletionLogic ();
			int pos = 0;
			logic.MatchFunc ("Tagname", "T", pos);

			string replaced = logic.ReplaceKey ("T", "Tagname", ref pos);

			Assert.AreEqual ("Tagname", replaced);
			Assert.AreEqual (7, pos);
		}

		[TestMethod]
		public void ReplaceKey_KeepsTheRestOfACompoundQuery ()
		{
			var logic = new CompletionLogic ();
			string query = $"(Tagn {Strings.And} XY)";
			int pos = 4;
			logic.MatchFunc ("Tagname", query, pos);

			string replaced = logic.ReplaceKey (query, "Tagname", ref pos);

			Assert.AreEqual ($"(Tagname {Strings.And} XY)", replaced);
			Assert.AreEqual (8, pos);
		}
	}
}
