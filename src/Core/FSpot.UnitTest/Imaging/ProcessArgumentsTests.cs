//
// ProcessArgumentsTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Imaging.UnitTest
{
	[TestClass]
	public class ProcessArgumentsTests
	{
		[TestMethod]
		public void Join_LeavesPlainArgumentsUnquoted ()
		{
			Assert.AreEqual ("-h -w -c", ProcessArguments.JoinWindows (new[] { "-h", "-w", "-c" }));
		}

		[TestMethod]
		public void Join_QuotesArgumentsContainingSpaces ()
		{
			Assert.AreEqual ("\"/my photos/a.nef\"",
				ProcessArguments.JoinWindows (new[] { "/my photos/a.nef" }));
		}

		[TestMethod]
		public void Join_QuotesArgumentsContainingTabs ()
		{
			Assert.AreEqual ("\"a\tb\"", ProcessArguments.JoinWindows (new[] { "a\tb" }));
		}

		[TestMethod]
		public void Join_EscapesEmbeddedQuotes ()
		{
			Assert.AreEqual ("\"say \\\"hi\\\"\"", ProcessArguments.JoinWindows (new[] { "say \"hi\"" }));
		}

		[TestMethod]
		public void Join_DoublesBackslashesOnlyBeforeAQuote ()
		{
			Assert.AreEqual ("\"a\\\\\\\"b\"", ProcessArguments.JoinWindows (new[] { "a\\\"b" }));
		}

		[TestMethod]
		public void Join_DoublesTrailingBackslashesInsideQuotes ()
		{
			Assert.AreEqual ("\"c:\\dir with space\\\\\"",
				ProcessArguments.JoinWindows (new[] { "c:\\dir with space\\" }));
		}

		[TestMethod]
		public void Join_LeavesInteriorBackslashesAlone ()
		{
			Assert.AreEqual ("c:\\dir\\file.nef", ProcessArguments.JoinWindows (new[] { "c:\\dir\\file.nef" }));
		}

		[TestMethod]
		public void Join_QuotesTheEmptyArgument ()
		{
			Assert.AreEqual ("-c \"\"", ProcessArguments.JoinWindows (new[] { "-c", string.Empty }));
		}

		[TestMethod]
		public void Join_LeavesNonAsciiUnquoted ()
		{
			Assert.AreEqual ("café_日本.nef", ProcessArguments.JoinWindows (new[] { "café_日本.nef" }));
		}

		[TestMethod]
		public void Join_ReturnsEmptyForNoArguments ()
		{
			Assert.AreEqual (string.Empty, ProcessArguments.JoinWindows (new string[0]));
		}

		// Mono on Unix -- which is what net472 means on Linux and macOS -- shell-parses
		// ProcessStartInfo.Arguments with g_shell_parse_argv, not CommandLineToArgvW.
		// Single-quoted content is literal there, so everything is wrapped and an
		// embedded apostrophe is closed, escaped and reopened.

		[TestMethod]
		public void JoinPosix_QuotesEveryArgument ()
		{
			Assert.AreEqual ("'-h' '-w' '-c'", ProcessArguments.JoinPosix (new[] { "-h", "-w", "-c" }));
		}

		[TestMethod]
		public void JoinPosix_SurvivesAnApostropheInAPath ()
		{
			Assert.AreEqual ("'/photos/it'\\''s.nef'",
				ProcessArguments.JoinPosix (new[] { "/photos/it's.nef" }));
		}

		[TestMethod]
		public void JoinPosix_KeepsBackslashesLiteral ()
		{
			Assert.AreEqual (@"'/photos/a.nef'", ProcessArguments.JoinPosix (new[] { @"/photos/a.nef" }));
		}

		[TestMethod]
		public void JoinPosix_ContainsCarriageReturnsAndFormFeeds ()
		{
			// g_shell_parse_argv splits on every ASCII space, \r and \f included, so an
			// unquoted filename holding one becomes extra dcraw flags.
			Assert.AreEqual ("'a\r-Z\r/tmp/owned'", ProcessArguments.JoinPosix (new[] { "a\r-Z\r/tmp/owned" }));
			Assert.AreEqual ("'a\f-Z'", ProcessArguments.JoinPosix (new[] { "a\f-Z" }));
		}

		[TestMethod]
		public void JoinPosix_QuotesSpacesAndDoubleQuotes ()
		{
			Assert.AreEqual ("'/my photos/say \"hi\".nef'",
				ProcessArguments.JoinPosix (new[] { "/my photos/say \"hi\".nef" }));
		}

		[TestMethod]
		public void JoinPosix_QuotesTheEmptyArgument ()
		{
			Assert.AreEqual ("'-c' ''", ProcessArguments.JoinPosix (new[] { "-c", string.Empty }));
		}

		[TestMethod]
		public void JoinPosix_ReturnsEmptyForNoArguments ()
		{
			Assert.AreEqual (string.Empty, ProcessArguments.JoinPosix (new string[0]));
		}


		[TestMethod]
		public void JoinPosix_RejectsANullArgument ()
		{
			// InternalProcess used to grow argv with a trailing null for the C call; a
			// null now just means a caller bug, and silently passing "" would hide it.
			Assert.ThrowsExactly<ArgumentException> (
				() => ProcessArguments.JoinPosix (new[] { "-c", null }));
		}

		[TestMethod]
		public void JoinWindows_RejectsANullArgument ()
		{
			Assert.ThrowsExactly<ArgumentException> (
				() => ProcessArguments.JoinWindows (new[] { "-c", null }));
		}

	}
}
