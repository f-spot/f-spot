//
// CommandLineHelpTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	// Goldens captured from Hyena.CommandLine.Layout before it was replaced, so the
	// help text a user sees is byte-identical across the change. Line endings come from
	// Environment.NewLine because Layout built its output with StringBuilder.AppendLine.
	[TestClass]
	public class CommandLineHelpTests
	{
		static string Lines (params string[] lines) => string.Join (Environment.NewLine, lines) + Environment.NewLine;

		static readonly string HelpGolden = Lines (
			"Help Options",
			"",
			"  --help              Show this help",
			"  --help-options      Show command line options",
			"  --help-all          Show all options",
			"  --version           Show version information",
			"  --versions          Show detailed version information");

		static readonly string OptionsGolden = Lines (
			"General options",
			"",
			"  --basedir=DIR       Path to the photo database folder",
			"  --import=URI        Import from the given uri",
			"  --photodir=DIR      Default import folder",
			"  --view ITEM         View file(s) or directories",
			"  --shutdown          Shut down a running instance of F-Spot",
			"  --slideshow         Display a slideshow",
			"  --debug             Run in debug mode");

		[TestMethod]
		public void HelpSection_MatchesTheTextLayoutProduced ()
		{
			Assert.AreEqual (HelpGolden, CommandLineHelp.Section ("help"));
		}

		[TestMethod]
		public void OptionsSection_MatchesTheTextLayoutProduced ()
		{
			Assert.AreEqual (OptionsGolden, CommandLineHelp.Section ("options"));
		}

		// Layout separated the two groups with one blank line and aligned descriptions
		// across both, which happens to be the same column each section uses alone.
		[TestMethod]
		public void Everything_MatchesTheTextLayoutProduced ()
		{
			Assert.AreEqual (HelpGolden + Environment.NewLine + OptionsGolden, CommandLineHelp.Everything ());
		}

		// The real sections both have a twelve-character longest option, so neither
		// Section nor Everything can distinguish aligning across all sections from
		// aligning within each -- this drives Render with sections that differ.
		[TestMethod]
		public void Render_AlignsEverySectionToTheWidestOptionAcrossAllOfThem ()
		{
			var sections = new[] {
				("Short", new[] { ("a", "first") }),
				("Long", new[] { ("a-much-longer-option", "second") }),
			};

			Assert.AreEqual (Lines (
				"Short",
				"",
				"  --a                         first",
				"",
				"Long",
				"",
				"  --a-much-longer-option      second"), CommandLineHelp.Render (sections));
		}

		[TestMethod]
		public void Section_ForAnUnknownGroupIsEmpty ()
		{
			Assert.AreEqual (string.Empty, CommandLineHelp.Section ("nonesuch"));
		}
	}
}
