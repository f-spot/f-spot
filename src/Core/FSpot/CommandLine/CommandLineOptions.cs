//
// CommandLineOptions.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.Linq;

namespace FSpot
{
	// Replaces Hyena's CommandLineParser and the FixArgs switch that fed it. The legacy
	// single-dash forms FixArgs rewrote by hand -- -b, -basedir, -h, -usage, -V and the
	// rest -- are aliases here, which also means --basedir /photos works: Hyena only
	// accepted --basedir=/photos and turned the following token into a file.
	//
	// Lives in FSpot.dll rather than the GTK client because --debug-sql is read by
	// FSpotDatabaseConnection and --debug by FSpotConfiguration.Debugging, both below the
	// client. Current is set once by Main before anything reads it.
	public sealed class CommandLineOptions
	{
		static CommandLineOptions current = new CommandLineOptions ();

		// Set once by Main, before anything reads it, and never null: consumers below the
		// GTK client read this -- FSpotDatabaseConnection reads DebugSql -- so a null
		// would fail at startup rather than at the ordering mistake that caused it.
		public static CommandLineOptions Current {
			get { return current; }
			set { current = value ?? throw new ArgumentNullException (nameof (value)); }
		}

		// An instance with nothing set, not a parse of no arguments: Parse builds
		// fourteen options and a root command, which is more work than belongs in a
		// static initialiser, and a throw there would surface as a
		// TypeInitializationException from whatever touched the type first. The property
		// initialisers below already give exactly the state Parse (empty) produces, which
		// Current_DefaultsToTheSameStateAsParsingNoArguments pins.
		CommandLineOptions ()
		{
		}

		public bool Help { get; private set; }
		public bool HelpOptions { get; private set; }
		public bool HelpAll { get; private set; }
		public bool Version { get; private set; }
		public bool Versions { get; private set; }
		public bool Shutdown { get; private set; }
		public bool Slideshow { get; private set; }
		public bool Debug { get; private set; }
		public bool DebugSql { get; private set; }
		public bool View { get; private set; }
		public string BaseDir { get; private set; }
		public string PhotoDir { get; private set; }
		public string Import { get; private set; }

		public IReadOnlyList<string> Files { get; private set; } = Array.Empty<string> ();

		// Anything shaped like --help... that is not one of the three real help options.
		// Hyena surfaced these through ContainsStart ("help") so Main could name them
		// back to the user; without this they would be silently ignored or, worse, opened
		// as files.
		public IReadOnlyList<string> InvalidHelpArguments { get; private set; } = Array.Empty<string> ();

		public bool WantsAnyHelp => Help || HelpOptions || HelpAll || InvalidHelpArguments.Count > 0;

		// The three sources Hyena's Debugging getter OR'd together, combined here rather
		// than left to a lazy fallback there. Main assigns FSpotConfiguration.Debugging
		// before anything reads it, so a fallback inside that property can never evaluate
		// -- which is how BANSHEE_DEBUG stopped working. Reading it here also makes it
		// testable, which it was not.
		public bool DebuggingRequested =>
			Debug || DebugSql || !string.IsNullOrEmpty (Environment.GetEnvironmentVariable ("BANSHEE_DEBUG"));

		public static CommandLineOptions Parse (string[] args)
		{
			if (args == null)
				throw new ArgumentNullException (nameof (args));

			var basedir = new Option<string> ("--basedir", "-basedir", "-b");
			var photodir = new Option<string> ("--photodir", "-photodir", "-p");
			var import = new Option<string> ("--import", "-import", "-i");
			var help = new Option<bool> ("--help", "-help", "-usage", "-h");
			var helpOptions = new Option<bool> ("--help-options");
			var helpAll = new Option<bool> ("--help-all");
			var version = new Option<bool> ("--version", "-version", "-V");
			var versions = new Option<bool> ("--versions", "-versions");
			var shutdown = new Option<bool> ("--shutdown", "-shutdown");
			var slideshow = new Option<bool> ("--slideshow", "-slideshow");
			var view = new Option<bool> ("--view", "-view", "-v");
			var debug = new Option<bool> ("--debug");
			var debugSql = new Option<bool> ("--debug-sql");

			var files = new Argument<string[]> ("files") { Arity = ArgumentArity.ZeroOrMore };

			var root = new RootCommand ("Personal photo management") {
				basedir, photodir, import, help, helpOptions, helpAll, version, versions,
				shutdown, slideshow, view, debug, debugSql, files
			};

			// F-Spot prints its own help and version, in its own format and with three
			// separate help sections, so the built-in options that would short-circuit
			// them are removed.
			foreach (var option in root.Options.Where (o => o is HelpOption || o is VersionOption).ToList ())
				root.Options.Remove (option);

			// Derived from the command rather than listed by hand. A hand-maintained list
			// goes stale silently: a new boolean option would quietly regain the
			// phantom-file behaviour below with no test failing, because every test names
			// an option that was in the list. That is how the single-dash half of this
			// went unswept once already.
			//
			// Placed after the built-in removal for tidiness, not necessity: HelpOption
			// and VersionOption are not Option<bool> -- they derive from a non-bool
			// Option -- so OfType never sees them and the order does not matter. Checked,
			// because the reverse order passes every test and a comment claiming the
			// ordering is load-bearing would be false.
			//
			// Arity zero so a boolean cannot consume the token after it. Without it,
			// "--view true" ate the "true" -- a file by that name disappeared and Startup
			// then failed with "-view option takes at least one argument" -- because
			// Option<bool> is 0..1 and takes a following token whenever it happens to
			// convert. On its own this would push "--debug=false" into the files argument
			// instead; DropBooleanValues is what makes it safe.
			var booleans = root.Options.OfType<Option<bool>> ().ToArray ();
			foreach (var option in booleans)
				option.Arity = ArgumentArity.Zero;

			// Split at "--" before parsing rather than filtering afterwards. Everything
			// after it is an operand by POSIX convention, so a file may legitimately be
			// named "--slideshow"; the unrecognised-option filter below would otherwise
			// throw it away for looking like an option.
			int terminator = Array.IndexOf (args, "--");
			string[] toParse = DropBooleanValues (
				terminator < 0 ? args : args.Take (terminator).ToArray (), booleans);
			string[] operands = terminator < 0
				? Array.Empty<string> ()
				: args.Skip (terminator + 1).ToArray ();

			var result = root.Parse (toParse);
			var positional = TokensOf (result, files);

			// Unrecognised options land in the positional argument because it takes any
			// number of tokens. Hyena accepted and ignored them, and never mistook one for
			// a file, so they are split back out here.
			var unrecognised = positional.Where (LooksLikeAnOption).ToList ();

			return new CommandLineOptions {
				Help = IsPresent (result, help),
				HelpOptions = IsPresent (result, helpOptions),
				HelpAll = IsPresent (result, helpAll),
				Version = IsPresent (result, version),
				Versions = IsPresent (result, versions),
				Shutdown = IsPresent (result, shutdown),
				Slideshow = IsPresent (result, slideshow),
				View = IsPresent (result, view),
				Debug = IsPresent (result, debug),
				DebugSql = IsPresent (result, debugSql),
				BaseDir = ValueOf (result, basedir),
				PhotoDir = ValueOf (result, photodir),
				Import = ValueOf (result, import),
				Files = positional.Where (t => !LooksLikeAnOption (t)).Concat (operands).ToList (),
				InvalidHelpArguments = unrecognised
					.Where (t => Name (t).StartsWith ("help", StringComparison.OrdinalIgnoreCase))
					.Select (Name)
					.ToList (),
			};
		}

		// Read through the parse result rather than GetValue, which runs the conversion
		// machinery and throws on the two shapes Hyena tolerated: a repeated option
		// ("expects a single argument but 2 were provided") and one with no value
		// ("required argument missing"). The raw tokens give Hyena's semantics for every
		// input F-Spot is given: present-without-a-value is the empty string, and of
		// several occurrences the last one wins.
		//
		// Not quite "exactly", and the gap is worth stating rather than glossing.
		// GetResult aggregates every occurrence into one OptionResult, so a later
		// valueless occurrence contributes no token and cannot erase an earlier value:
		// "--basedir=/a --basedir" is /a here where Hyena gave the empty string and Main
		// exited 1. It is the last occurrence that carried a value that wins, not simply
		// the last occurrence.
		static string ValueOf (ParseResult result, Option<string> option)
		{
			var found = result.GetResult (option);
			if (found == null || found.Implicit)
				return null;

			return found.Tokens.Count > 0 ? found.Tokens[found.Tokens.Count - 1].Value : string.Empty;
		}

		// Hyena had no notion of a boolean value: the option was either on the command
		// line or it was not, so "--debug=false" would have been on. Presence matches it.
		static bool IsPresent (ParseResult result, Option<bool> option)
		{
			// GetResult is not a presence test on its own: an option with a default value
			// gets an implicit result even when it never appeared on the command line.
			var found = result.GetResult (option);

			return found != null && !found.Implicit;
		}

		static List<string> TokensOf (ParseResult result, Argument<string[]> argument)
		{
			var found = result.GetResult (argument);

			return found == null
				? new List<string> ()
				: found.Tokens.Select (t => t.Value).ToList ();
		}

		// Rewrites "--debug=anything" to "--debug". A boolean option cannot convert an
		// arbitrary value, and System.CommandLine records no parse error when it fails --
		// it routes the token into the greedy files argument instead, so "--view=1"
		// reached Startup with Files=["1"] and F-Spot tried to open a photo named 1.
		// Dropping the value is what the presence-only semantics already claim: the
		// option is either on the command line or it is not.
		static string[] DropBooleanValues (string[] args, Option<bool>[] booleans)
		{
			var names = new HashSet<string> (
				booleans.SelectMany (o => new[] { o.Name }.Concat (o.Aliases)), StringComparer.Ordinal);

			return args.Select (a => {
				// Any dash-prefixed token, not just "--": the single-dash aliases FixArgs
				// used to rewrite leak in exactly the same way, and the threshold has to
				// allow "=" at index 2 or "-v=1" stays unswept. Exact membership in names
				// is what keeps this safe -- a file called "-x=y" is still a file.
				if (a.Length < 2 || a[0] != '-')
					return a;

				int equals = a.IndexOf ('=');

				return equals > 1 && names.Contains (a.Substring (0, equals)) ? a.Substring (0, equals) : a;
			}).ToArray ();
		}

		// Matches Hyena's IsOption: two leading dashes and a non-empty name, so a bare
		// "--", a "-" and any single-dash token are files rather than options.
		static bool LooksLikeAnOption (string token)
		{
			return token.Length > 2 && token.StartsWith ("--", StringComparison.Ordinal);
		}

		static string Name (string token)
		{
			string name = token.Substring (2);
			int equals = name.IndexOf ('=');

			return equals > 0 ? name.Substring (0, equals) : name;
		}
	}
}
