//
// ProcessArgumentsRoundTripTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Imaging.Tests
{
	// ProcessArgumentsTests asserts the joined string, which only ever proves the joiner
	// agrees with itself. These spawn a real child and compare the argv it actually
	// received, so a difference in how the parser on the other side behaves fails here
	// rather than on someone's photo. Both joiners get the same adversarial set: a
	// filename is user-supplied, and every entry below is a legal Unix filename.
	[TestClass]
	public class ProcessArgumentsRoundTripTests
	{
		// printf reuses its format for each remaining operand, so this prints one
		// <...> per argv element the shell handed it.
		const string PrintArgv = "printf '<%s>' \"$@\"";

		public static IEnumerable<(string name, string[] argv)> Adversarial =>
		[
			("apostrophe", ["/photos/Dad's.nef"]),
			("several apostrophes", ["it's a 'party'.nef"]),
			("carriage return", ["-c", "a\r-Z\r/tmp/owned"]),
			("form feed", ["-c", "a\f-Z"]),
			("interior backslash", [@"/photos/a\b.nef"]),
			("trailing backslash", [@"/photos/back\"]),
			("only backslashes", [@"\\"]),
			("embedded quotes", ["/my photos/say \"hi\".nef"]),
			("spaces", ["/my photos/plain.nef"]),
			("tab", ["a\tb"]),
			("newline", ["a\nb"]),
			("empty element", ["-c", ""]),
			("non-ascii", ["café_日本.nef"]),
			("shell metacharacters", ["$HOME`x;y*z#w.nef"]),
			("dcraw invocation", ["-h", "-w", "-c", "-t", "0", "/photos/Dad's raw.nef"]),
		];

		[TestMethod]
		[DynamicData (nameof (Adversarial))]
		[OSCondition (OperatingSystems.Linux | OperatingSystems.OSX)]
		public void JoinPosix_RoundTripsThroughAPosixShell (string name, string[] argv)
		{
			// g_shell_parse_argv, which is what Mono hands Arguments to, implements POSIX
			// shell quoting -- so a real shell is the closest parser reachable from a
			// net10 test host. ArgumentList carries the script across verbatim, leaving
			// only the joiner under test.
			//
			// It is a proxy, not the real thing, and it is weaker in one specific way:
			// sh splits on IFS (space, tab, newline) while g_shell_parse_argv splits on
			// g_ascii_isspace, which also includes \r and \f. Those two cases therefore
			// pass here even unquoted -- the argv-splitting vector they represent is
			// pinned by JoinPosix_ContainsCarriageReturnsAndFormFeeds instead. Verified
			// against Mono 6.12 by hand; reverting JoinPosix to the Windows joiner fails
			// 5 of the cases below, so the rest of this set does have teeth.
			var startInfo = new ProcessStartInfo ("/bin/sh") {
				RedirectStandardOutput = true,
				UseShellExecute = false
			};
			startInfo.ArgumentList.Add ("-c");
			startInfo.ArgumentList.Add ($"printf '<%s>' {ProcessArguments.JoinPosix (argv)}");

			Assert.AreEqual (Expected (argv), Run (startInfo), name);
		}

		[TestMethod]
		[DynamicData (nameof (Adversarial))]
		[OSCondition (OperatingSystems.Linux | OperatingSystems.OSX)]
		public void JoinWindows_RoundTripsThroughTheRuntimeParser (string name, string[] argv)
		{
			// .NET splits ProcessStartInfo.Arguments by CommandLineToArgvW's rules on
			// every platform, so setting it exercises the joiner against the parser it
			// was written for.
			var startInfo = new ProcessStartInfo ("/bin/sh") {
				RedirectStandardOutput = true,
				UseShellExecute = false,
				Arguments = ProcessArguments.JoinWindows (
					new[] { "-c", PrintArgv, "sh" }.Concat (argv))
			};

			Assert.AreEqual (Expected (argv), Run (startInfo), name);
		}

		static string Expected (IEnumerable<string> argv) =>
			string.Concat (argv.Select (argument => $"<{argument}>"));

		static string Run (ProcessStartInfo startInfo)
		{
			using var process = Process.Start (startInfo);
			string output = process.StandardOutput.ReadToEnd ();
			process.WaitForExit ();
			return output;
		}
	}
}
