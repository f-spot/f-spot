//
// FindBarTests.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Daniel Köb
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

using NUnit.Framework;

namespace FSpot.Widgets.Tests
{
	[TestFixture]
	public class FindBarTests
	{
		[Test]
		public void SuccessfulCompletionTest ()
		{
			var logic = new CompletionLogic ();

			Assert.IsTrue (logic.MatchFunc ("Tagname", "T", 0), "first char");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "t", 0), "first char, different casing");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "Ta", 1), "two chars");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "tA", 1), "two chars, different casing");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "tagnam", 5), "except one char, different casing");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "Tagnam", 3), "except one char, caret in between");
		}

		[Test]
		public void MatchTagsWithDiacritics ()
		{
			var logic = new CompletionLogic ();

			Assert.IsTrue (logic.MatchFunc ("àáâãäåa", "àáâãäå", 5), "chars with diacritics");
			Assert.IsTrue (logic.MatchFunc ("ÒÓÔÕÖØO", "òóôõöø", 5), "chars with diacritics, different casing");
			Assert.IsTrue (logic.MatchFunc ("àáâãäåa", "aaaaaa", 5), "remove diacritics");
			Assert.IsTrue (logic.MatchFunc ("ÒÓÔÕÖØO", "oooooo", 5), "remove diacritics, different casing");
		}

		[Test]
		public void MatchTagsWithSpaces ()
		{
			var logic = new CompletionLogic ();

			Assert.IsTrue (logic.MatchFunc ("Tag with spaces", "Ta", 1), "");
			Assert.IsTrue (logic.MatchFunc ("Tag with spaces", "Tag", 2), "");
			Assert.IsTrue (logic.MatchFunc ("Tag with spaces", "Tag ", 3), "");
			Assert.IsTrue (logic.MatchFunc ("Tag with spaces", "Tag w", 4), "");
			Assert.IsTrue (logic.MatchFunc ("Tag with spaces", "Tag with", 7), "");

			Assert.IsFalse (logic.MatchFunc ("Tag with spaces", "Tag with spaces", 14), "");

			Assert.IsFalse (logic.MatchFunc ("Tag with spaces", "wit", 2), "");
			Assert.IsFalse (logic.MatchFunc ("Tag with spaces", "with s", 5), "");
		}

		[Test]
		public void UnsuccessfulCompletionTest ()
		{
			var logic = new CompletionLogic ();

			Assert.IsFalse (logic.MatchFunc ("Tagname", "", 0), "empty string");
			Assert.IsFalse (logic.MatchFunc ("Tagname", "a", 0), "single char, no match");
			Assert.IsFalse (logic.MatchFunc ("Tagname", "tagname", 6), "complete tag name");
			Assert.IsFalse (logic.MatchFunc ("Tagname", "Tagname ", 7), "complete tag with space");
		}

		[Test]
		public void SuccessfulCompletionTestWithParenthesis ()
		{
			var logic = new CompletionLogic ();

			Assert.IsTrue (logic.MatchFunc ("Tagname", "(T", 1), "first char");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "(tagnam", 6), "except one char, different casing");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "(Ta)", 2), "with closing parenthesis");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "(Tagnam)", 2), "with closing parenthesis, caret in between");
		}

		[Test]
		public void SuccessfulCompletionTestWithAndOperator ()
		{
			var logic = new CompletionLogic ();

			Assert.IsTrue (logic.MatchFunc ("Tagname", "XY and T", 7), "first char, after operator");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "XY and tagnam", 12), "except one char, different casing, after operator");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "T and XY", 0), "first char, before operator");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "tagnam and XY", 5), "except one char, different casing, before operator");
		}

		[Test]
		public void SuccessfulCompletionTestWithOrOperator ()
		{
			var logic = new CompletionLogic ();

			Assert.IsTrue (logic.MatchFunc ("Tagname", "XY or T", 6), "first char");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "XY or tagnam", 11), "except one char, different casing");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "T or XY", 0), "first char");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "tagnam or XY", 5), "except one char, different casing");
		}

		[Test]
		public void SuccessfulCompletionTestInBetween ()
		{
			var logic = new CompletionLogic ();

			Assert.IsTrue (logic.MatchFunc ("Tagname", "XY and T and AB", 7), "first char");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "XY and tagnam and AB", 12), "except one char, different casing");
		}

		[Test]
		public void ReplaceKeyTest ()
		{
			int pos;
			var logic = new CompletionLogic ();

			pos = 0;
			logic.MatchFunc ("Tagname", "T", pos);
			Assert.AreEqual (logic.ReplaceKey ("T", "Tagname", ref pos), "Tagname");
			Assert.AreEqual (pos, 7);

			pos = 2;
			logic.MatchFunc ("Tagname", "Tagn", pos);
			Assert.AreEqual (logic.ReplaceKey ("Tagn", "Tagname", ref pos), "Tagname");
			Assert.AreEqual (pos, 7);

			pos = 4;
			logic.MatchFunc ("Tagname", "(Tagn and XY)", pos);
			Assert.AreEqual (logic.ReplaceKey ("(Tagn and XY)", "Tagname", ref pos), "(Tagname and XY)");
			Assert.AreEqual (pos, 8);
		}
	}
}
