#if ENABLE_TESTS
using NUnit.Framework;

namespace FSpot.Widgets.Tests
{
	[TestFixture]
	public class FindBarTests
	{
		[Test]
		public void SuccessfulCompletionTest ()
		{
			CompletionLogic logic = new CompletionLogic ();

			Assert.IsTrue (logic.MatchFunc ("Tagname", "T", 0), "first char");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "t", 0), "first char, different casing");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "Ta", 1), "two chars");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "tA", 1), "two chars, different casing");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "tagnam", 5), "except one char, different casing");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "Tagnam", 3), "except one char, caret in between");
		}

		[Test]
		public void UnsuccessfulCompletionTest ()
		{
			CompletionLogic logic = new CompletionLogic ();

			Assert.IsFalse (logic.MatchFunc ("Tagname", "", 0), "empty string");
			Assert.IsFalse (logic.MatchFunc ("Tagname", "a", 0), "single char, no match");
			Assert.IsFalse (logic.MatchFunc ("Tagname", "tagname", 6), "complete tag name");
			Assert.IsFalse (logic.MatchFunc ("Tagname", "Tagname ", 7), "complete tag with space");
		}

		[Test]
		public void SuccessfulCompletionTestWithParenthesis ()
		{
			CompletionLogic logic = new CompletionLogic ();

			Assert.IsTrue (logic.MatchFunc ("Tagname", "(T", 1), "first char");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "(tagnam", 6), "except one char, different casing");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "(Ta)", 2), "with closing parenthesis");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "(Tagnam)", 2), "with closing parenthesis, caret in between");
		}

		[Test]
		public void SuccessfulCompletionTestWithAndOperator ()
		{
			CompletionLogic logic = new CompletionLogic ();

			Assert.IsTrue (logic.MatchFunc ("Tagname", "XY and T", 7), "first char");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "XY and tagnam", 12), "except one char, different casing");
		}

		[Test]
		public void SuccessfulCompletionTestInBetween ()
		{
			CompletionLogic logic = new CompletionLogic ();

			Assert.IsTrue (logic.MatchFunc ("Tagname", "XY and T and AB", 7), "first char");
			Assert.IsTrue (logic.MatchFunc ("Tagname", "XY and tagnam and AB", 12), "except one char, different casing");
		}

		[Test]
		public void ReplaceKeyTest ()
		{
			int pos;
			CompletionLogic logic = new CompletionLogic ();

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
#endif
