//
// CommandLineOptionsTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests.CommandLine
{
	// The same behaviours CommandLineParserGoldenTests pins on Hyena's parser, asserted
	// against the replacement, so the equivalence is structural rather than claimed.
	// Where the two deliberately differ there is a test saying so.
	[TestClass]
	public class CommandLineOptionsTests
	{
		static CommandLineOptions Parse (params string[] args) => CommandLineOptions.Parse (args);

		[TestMethod]
		public void EqualsForm_SplitsNameFromValue ()
		{
			var options = Parse ("--basedir=/photos");

			Assert.AreEqual ("/photos", options.BaseDir);
			Assert.IsEmpty (options.Files);
		}

		// Hyena's parser did not accept this at all -- the value became a file, which is
		// why FixArgs had to rewrite every legacy form into --name=value. Now both work.
		[TestMethod]
		public void SpaceSeparatedValue_IsAcceptedWhereHyenaTreatedItAsAFile ()
		{
			var options = Parse ("--basedir", "/photos");

			Assert.AreEqual ("/photos", options.BaseDir);
			Assert.IsEmpty (options.Files);
		}

		[TestMethod]
		[DataRow ("-b")]
		[DataRow ("-basedir")]
		[DataRow ("--basedir")]
		public void LegacyAliases_ReplaceWhatFixArgsRewrote (string alias)
		{
			Assert.AreEqual ("/photos", Parse (alias, "/photos").BaseDir);
		}

		[TestMethod]
		[DataRow ("-h")]
		[DataRow ("-help")]
		[DataRow ("-usage")]
		[DataRow ("--help")]
		public void HelpAliases_AllRequestHelp (string alias)
		{
			Assert.IsTrue (Parse (alias).Help);
		}

		[TestMethod]
		[DataRow ("-V")]
		[DataRow ("-version")]
		[DataRow ("--version")]
		public void VersionAliases_AllRequestVersion (string alias)
		{
			Assert.IsTrue (Parse (alias).Version);
		}

		[TestMethod]
		[DataRow ("-v")]
		[DataRow ("-view")]
		[DataRow ("--view")]
		public void ViewAliases_AllRequestView (string alias)
		{
			Assert.IsTrue (Parse (alias).View);
		}

		[TestMethod]
		public void Flags_AreFalseWhenAbsent ()
		{
			var options = Parse ();

			Assert.IsFalse (options.Slideshow);
			Assert.IsFalse (options.Shutdown);
			Assert.IsFalse (options.Debug);
			Assert.IsFalse (options.DebugSql);
			Assert.IsNull (options.BaseDir);
		}

		[TestMethod]
		public void BareTokens_BecomeFilesInOrder ()
		{
			Assert.AreSequenceEqual (
				new[] { "a.jpg", "b.jpg" },
				Parse ("a.jpg", "--slideshow", "b.jpg").Files.ToArray ());
		}

		[TestMethod]
		public void Files_KeepUriForms ()
		{
			Assert.AreSequenceEqual (
				new[] { "file:///photos/a.jpg" },
				Parse ("file:///photos/a.jpg").Files.ToArray ());
		}

		// Preserved from Hyena: an unrecognised option is ignored rather than rejected,
		// and must not be mistaken for a file. f-spot --bogus photo.jpg opens photo.jpg.
		[TestMethod]
		public void UnknownOption_IsIgnoredAndKeptOutOfFiles ()
		{
			var options = Parse ("--bogus", "photo.jpg");

			Assert.AreSequenceEqual (new[] { "photo.jpg" }, options.Files.ToArray ());
			Assert.IsEmpty (options.InvalidHelpArguments);
		}

		[TestMethod]
		public void RepeatedOption_KeepsTheLastValue ()
		{
			Assert.AreEqual ("/second", Parse ("--basedir=/first", "--basedir=/second").BaseDir);
		}

		// Replaces ContainsStart ("help"): a misspelled --help* argument is named back to
		// the user rather than silently ignored or treated as a file.
		[TestMethod]
		public void MisspelledHelpArgument_IsReportedByName ()
		{
			var options = Parse ("--helpx", "--helpy");

			Assert.AreSequenceEqual (new[] { "helpx", "helpy" }, options.InvalidHelpArguments.ToArray ());
			Assert.IsEmpty (options.Files);
		}

		[TestMethod]
		public void RecognisedHelpArguments_AreNotReportedAsInvalid ()
		{
			var options = Parse ("--help", "--help-all", "--help-options");

			Assert.IsTrue (options.Help);
			Assert.IsTrue (options.HelpAll);
			Assert.IsTrue (options.HelpOptions);
			Assert.IsEmpty (options.InvalidHelpArguments);
		}

		[TestMethod]
		public void DebugSql_IsAcceptedThoughItIsUndocumented ()
		{
			Assert.IsTrue (Parse ("--debug-sql").DebugSql);
		}

		// Hyena tolerated an empty value; so does this, rather than erroring the way
		// System.CommandLine would for a missing required argument.
		[TestMethod]
		public void EmptyValue_IsTolerated ()
		{
			Assert.AreEqual (string.Empty, Parse ("--basedir=").BaseDir);
		}

		[TestMethod]
		public void EqualsForm_KeepsLaterEqualsSignsInTheValue ()
		{
			Assert.AreEqual ("/photos/a=b", Parse ("--basedir=/photos/a=b").BaseDir);
		}

		// Hyena required the '=' at index greater than 1, so a one-character option name
		// never split and "--a=b" was an option literally named "a=b". Nothing in F-Spot
		// has a one-character option, so this only records that the quirk is gone rather
		// than carried forward: an unknown "--a=b" is now ignored like any other unknown.
		[TestMethod]
		public void OneCharacterOptionName_NoLongerSwallowsItsValue ()
		{
			var options = Parse ("--a=b", "photo.jpg");

			Assert.AreSequenceEqual (new[] { "photo.jpg" }, options.Files.ToArray ());
			Assert.IsEmpty (options.InvalidHelpArguments);
		}

		// Only "--" followed by a name is an option. A lone dash, a bare "--" and any
		// path are files, exactly as Hyena's IsOption decided. The single-dash legacy
		// forms are the deliberate exception: they are aliases now.
		[TestMethod]
		[DataRow ("-")]
		[DataRow ("photo.jpg")]
		[DataRow ("file:///photos/a.jpg")]
		[DataRow ("/absolute/path.jpg")]
		public void BareDashesAndPaths_AreFilesNotOptions (string token)
		{
			Assert.AreSequenceEqual (new[] { token }, Parse (token).Files.ToArray ());
		}

		// A deliberate difference. Hyena treated a lone "--" as a filename, because its
		// IsOption wanted more than two characters. System.CommandLine gives it the POSIX
		// meaning -- everything after it is an operand, not an option -- which is worth
		// more than the ability to open a file named "--": it is how you would open one
		// named "--slideshow".
		[TestMethod]
		public void DoubleDash_EndsTheOptionsInsteadOfBeingAFile ()
		{
			Assert.IsEmpty (Parse ("--").Files);

			var options = Parse ("--", "--slideshow");

			Assert.IsFalse (options.Slideshow);
			Assert.AreSequenceEqual (new[] { "--slideshow" }, options.Files.ToArray ());
		}

		// Consumers below the GTK client read Current -- FSpotDatabaseConnection reads
		// DebugSql -- so a null would fail at startup, far from the ordering mistake that
		// caused it.
		[TestMethod]
		public void Current_RejectsNull ()
		{
			var saved = CommandLineOptions.Current;

			try {
				Assert.ThrowsExactly<ArgumentNullException> (() => CommandLineOptions.Current = null);
				Assert.IsNotNull (CommandLineOptions.Current);
			} finally {
				CommandLineOptions.Current = saved;
			}
		}

		// The default has to match a parse of no arguments without running the parser,
		// which builds fourteen options and a root command -- too much for a static
		// initialiser, where a throw would surface as a TypeInitializationException from
		// whatever touched the type first.
		[TestMethod]
		public void Current_DefaultsToTheSameStateAsParsingNoArguments ()
		{
			var parsed = Parse ();
			var initial = CommandLineOptions.Current;

			Assert.AreEqual (parsed.Help, initial.Help);
			Assert.AreEqual (parsed.Version, initial.Version);
			Assert.AreEqual (parsed.Debug, initial.Debug);
			Assert.AreEqual (parsed.DebugSql, initial.DebugSql);
			Assert.AreEqual (parsed.BaseDir, initial.BaseDir);
			Assert.AreEqual (parsed.PhotoDir, initial.PhotoDir);
			Assert.AreEqual (parsed.Import, initial.Import);
			Assert.IsEmpty (initial.Files);
			Assert.IsEmpty (initial.InvalidHelpArguments);
			Assert.IsFalse (initial.WantsAnyHelp);
		}

		// A boolean option given a value System.CommandLine cannot convert to bool cannot
		// consume it, so the token fell through into the greedy files argument -- with no
		// parse error recorded. "f-spot --view=1" then reached Startup with View set and
		// Files=["1"], and tried to open a photo named 1 that nobody asked for. Hyena
		// dropped the value and kept Files empty.
		// Every boolean option, in every spelling it accepts -- ten options and their
		// aliases. Sampling four of them is what let the single-dash half of this go
		// unswept the first time.
		[TestMethod]
		[DataRow ("--help=x")]
		[DataRow ("--help-options=x")]
		[DataRow ("--help-all=x")]
		[DataRow ("--version=x")]
		[DataRow ("--versions=x")]
		[DataRow ("--shutdown=x")]
		[DataRow ("--slideshow=x")]
		[DataRow ("--view=1")]
		[DataRow ("--debug=1")]
		[DataRow ("--debug=garbage")]
		[DataRow ("--debug-sql=x")]
		public void BooleanOptionWithANonBooleanValue_DoesNotBecomeAFile (string arg)
		{
			Assert.IsEmpty (Parse (arg).Files);
		}

		[TestMethod]
		public void BooleanOptionWithANonBooleanValue_LeavesRealFilesAlone ()
		{
			var options = Parse ("--slideshow=x", "a.jpg");

			Assert.IsTrue (options.Slideshow);
			Assert.AreSequenceEqual (new[] { "a.jpg" }, options.Files.ToArray ());
		}

		// Presence is what counts, so a value that does convert changes nothing either:
		// Hyena had no notion of a boolean value and "--debug=false" meant debugging on.
		[TestMethod]
		[DataRow ("--debug=false")]
		[DataRow ("--debug=true")]
		[DataRow ("--debug")]
		public void BooleanOptionValue_NeverTurnsTheOptionOff (string arg)
		{
			var options = Parse (arg);

			Assert.IsTrue (options.Debug);
			Assert.IsEmpty (options.Files);
		}

		// The mirror of the above: a boolean option must not eat the following token
		// either, which it did whenever that token happened to parse as a bool. A file
		// named "true" after --view was swallowed, and Startup then failed with "-view
		// option takes at least one argument".
		[TestMethod]
		public void BooleanOption_DoesNotSwallowAFollowingTrueOrFalse ()
		{
			Assert.AreSequenceEqual (new[] { "true" }, Parse ("--view", "true").Files.ToArray ());
			Assert.AreSequenceEqual (
				new[] { "false", "a.jpg" },
				Parse ("--view", "false", "a.jpg").Files.ToArray ());
		}

		// BANSHEE_DEBUG is the third source the old Debugging getter OR'd together, and
		// the rewrite silenced it: Main assigned Debugging unconditionally before anything
		// read it, so the getter's lazy env-var fallback could never evaluate. Both its
		// consumers -- slow-query logging in DatabaseCommand and the row-count warning in
		// ArrayDataReader -- went quiet. Combined here instead of in a lazy fallback so
		// it cannot be latched off, and so it is testable at all.
		[TestMethod]
		public void DebuggingRequested_ComesFromTheEnvironmentWhenNoFlagWasGiven ()
		{
			WithBansheeDebug ("1", () => {
				var options = Parse ();

				Assert.IsFalse (options.Debug);
				Assert.IsFalse (options.DebugSql);
				Assert.IsTrue (options.DebuggingRequested);
			});
		}

		[TestMethod]
		public void DebuggingRequested_IsFalseWithNeitherFlagNorEnvironment ()
		{
			WithBansheeDebug (null, () => Assert.IsFalse (Parse ().DebuggingRequested));
		}

		[TestMethod]
		[DataRow ("--debug")]
		[DataRow ("--debug-sql")]
		public void DebuggingRequested_ComesFromEitherFlagWithoutTheEnvironment (string arg)
		{
			WithBansheeDebug (null, () => Assert.IsTrue (Parse (arg).DebuggingRequested));
		}

		// An empty value is not "set", matching what Hyena's EnvironmentIsSet decided.
		[TestMethod]
		public void DebuggingRequested_TreatsAnEmptyEnvironmentVariableAsUnset ()
		{
			WithBansheeDebug (string.Empty, () => Assert.IsFalse (Parse ().DebuggingRequested));
		}

		static void WithBansheeDebug (string value, Action body)
		{
			string saved = Environment.GetEnvironmentVariable ("BANSHEE_DEBUG");
			Environment.SetEnvironmentVariable ("BANSHEE_DEBUG", value);
			try {
				body ();
			} finally {
				Environment.SetEnvironmentVariable ("BANSHEE_DEBUG", saved);
			}
		}

		// A difference, pinned so the next reader is not misled by the word "last".
		// GetResult aggregates every occurrence into one result, so a later valueless
		// occurrence contributes no token and the earlier value survives. Hyena gave the
		// empty string here and Main exited 1 with "-basedir option takes one argument".
		// Nobody types this; the test exists because the comment claimed otherwise.
		[TestMethod]
		public void RepeatedOption_KeepsTheLastValueThatCarriedOne ()
		{
			Assert.AreEqual ("/a", Parse ("--basedir=/a", "--basedir").BaseDir);
		}

		// Hyena's gate was a case-sensitive StartsWith, so "--Help" was an unknown option
		// and the app started normally. Now it is reported as an invalid help argument and
		// startup short-circuits -- an improvement, but a behaviour change, so it is
		// pinned rather than left to be rediscovered.
		[TestMethod]
		[DataRow ("--Help", "Help")]
		[DataRow ("--HELP-ALL", "HELP-ALL")]
		[DataRow ("--HelpNope", "HelpNope")]
		public void MisspelledHelpArgument_IsMatchedWithoutRegardToCase (string arg, string reported)
		{
			var options = Parse (arg);

			Assert.AreSequenceEqual (new[] { reported }, options.InvalidHelpArguments.ToArray ());
			Assert.IsTrue (options.WantsAnyHelp);
		}

		// A missing value must not swallow the next option. "--basedir --view a.jpg"
		// used to store the catalog directory as the literal "--view" and drop view mode.
		[TestMethod]
		[DataRow ("--basedir", "--view")]
		[DataRow ("--basedir=", "--view")]
		[DataRow ("-basedir=", "--view")]
		[DataRow ("-b=", "--view")]
		[DataRow ("--photodir", "--slideshow")]
		[DataRow ("--import", "--debug")]
		[DataRow ("-basedir", "--view")]
		public void ValueTakingOption_DoesNotEatAFollowingOption (string option, string following)
		{
			var options = Parse (option, following, "a.jpg");

			Assert.AreEqual (string.Empty, options.BaseDir ?? options.PhotoDir ?? options.Import);
			Assert.AreSequenceEqual (new[] { "a.jpg" }, options.Files.ToArray ());
			if (following == "--view")
				Assert.IsTrue (options.View);
			if (following == "--slideshow")
				Assert.IsTrue (options.Slideshow);
			if (following == "--debug")
				Assert.IsTrue (options.Debug);
		}

		// The twin of BooleanOptionWithANonBooleanValue_DoesNotBecomeAFile, missed when
		// that was fixed: the rewrite only looked at tokens starting with "--", so every
		// single-dash alias still leaked its value. "f-spot -view=1 a.jpg" reached Startup
		// in view mode with Files=["1","a.jpg"] and tried to open a photo named 1 ahead of
		// the real one.
		[TestMethod]
		[DataRow ("-help=x")]
		[DataRow ("-usage=x")]
		[DataRow ("-h=x")]
		[DataRow ("-version=x")]
		[DataRow ("-V=x")]
		[DataRow ("-versions=x")]
		[DataRow ("-shutdown=x")]
		[DataRow ("-slideshow=x")]
		[DataRow ("-view=x")]
		[DataRow ("-v=1")]
		public void SingleDashBooleanAliasWithAValue_DoesNotBecomeAFile (string arg)
		{
			Assert.IsEmpty (Parse (arg).Files);
		}

		[TestMethod]
		public void SingleDashBooleanAliasWithAValue_StillSetsTheOptionAndKeepsRealFiles ()
		{
			var options = Parse ("-view=1", "a.jpg");

			Assert.IsTrue (options.View);
			Assert.AreSequenceEqual (new[] { "a.jpg" }, options.Files.ToArray ());
		}

		// Exact membership in the option set is what keeps the widened guard safe: a file
		// whose name merely looks like a single-dash assignment is not an option.
		[TestMethod]
		[DataRow ("-x=y")]
		[DataRow ("-")]
		[DataRow ("a=b")]
		[DataRow ("-notanoption=1")]
		public void TokensThatOnlyLookLikeSingleDashOptions_AreStillFiles (string token)
		{
			Assert.AreSequenceEqual (new[] { token }, Parse (token).Files.ToArray ());
		}

		// A value-taking option's single-dash alias keeps its value -- only booleans are
		// rewritten.
		[TestMethod]
		[DataRow ("-b=/photos")]
		[DataRow ("-basedir=/photos")]
		public void SingleDashValueTakingAlias_KeepsItsValue (string arg)
		{
			Assert.AreEqual ("/photos", Parse (arg).BaseDir);
			Assert.IsEmpty (Parse (arg).Files);
		}

		[TestMethod]
		public void EveryOptionTheHelpTextAdvertises_IsAccepted ()
		{
			var options = Parse ("--basedir=/b", "--photodir=/p", "--import=/i",
				"--view", "--shutdown", "--slideshow", "--debug", "--versions");

			Assert.AreEqual ("/b", options.BaseDir);
			Assert.AreEqual ("/p", options.PhotoDir);
			Assert.AreEqual ("/i", options.Import);
			Assert.IsTrue (options.View);
			Assert.IsTrue (options.Shutdown);
			Assert.IsTrue (options.Slideshow);
			Assert.IsTrue (options.Debug);
			Assert.IsTrue (options.Versions);
			Assert.IsEmpty (options.InvalidHelpArguments);
		}
	}
}
