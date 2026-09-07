//
// ProcessArguments.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FSpot.Imaging
{
	// net472 has no ProcessStartInfo.ArgumentList, so a child's argv has to be flattened
	// into one string -- and the parser on the other side is not the same everywhere.
	// .NET on any platform, and Mono on Windows, split Arguments by CommandLineToArgvW's
	// rules. Mono on Unix shell-parses it with g_shell_parse_argv instead, where a bare
	// apostrophe is an unterminated quote, a backslash escapes the next character, and
	// \r and \f separate arguments. net472 on Linux and macOS IS Mono, which is what
	// F-Spot ships, so both parsers are live and a photo filename reaches both.
	//
	// When FSpot.dll reaches net10.0 for the Avalonia port, add each argument to
	// ArgumentList and delete this class: the runtime then quotes for the real host.
	static class ProcessArguments
	{
		static readonly char[] WindowsNeedsQuoting = { ' ', '\t', '\n', '\v', '"' };

		// Mono on Windows goes through the real CreateProcess, so it wants the Windows
		// joiner; only Mono on a Unix host shell-parses. Asking RuntimeInformation
		// rather than Mono's PlatformID mapping keeps this honest if that mapping ever
		// changes.
		public static bool UsesShellParsing { get; } =
			Type.GetType ("Mono.Runtime") != null
			&& !RuntimeInformation.IsOSPlatform (OSPlatform.Windows);

		public static string Join (IEnumerable<string> arguments)
			=> UsesShellParsing ? JoinPosix (arguments) : JoinWindows (arguments);

		// g_shell_parse_argv treats single-quoted text as wholly literal -- no escapes
		// of any kind -- so every argument is wrapped, and an embedded apostrophe ends
		// the quote, escapes itself, and opens a new one.
		public static string JoinPosix (IEnumerable<string> arguments)
		{
			var builder = new StringBuilder ();

			foreach (var argument in arguments) {
				if (argument == null)
					throw new ArgumentException ("argv holds a null", nameof (arguments));

				if (builder.Length > 0)
					builder.Append (' ');

				builder.Append ('\'');
				foreach (char c in argument) {
					if (c == '\'')
						builder.Append ("'\\''");
					else
						builder.Append (c);
				}
				builder.Append ('\'');
			}

			return builder.ToString ();
		}

		public static string JoinWindows (IEnumerable<string> arguments)
		{
			var builder = new StringBuilder ();

			foreach (var argument in arguments) {
				if (argument == null)
					throw new ArgumentException ("argv holds a null", nameof (arguments));

				if (builder.Length > 0)
					builder.Append (' ');

				AppendWindows (builder, argument);
			}

			return builder.ToString ();
		}

		static void AppendWindows (StringBuilder builder, string argument)
		{
			if (argument.Length > 0 && argument.IndexOfAny (WindowsNeedsQuoting) < 0) {
				builder.Append (argument);
				return;
			}

			builder.Append ('"');

			int i = 0;
			while (i < argument.Length) {
				int backslashes = 0;
				while (i < argument.Length && argument[i] == '\\') {
					backslashes++;
					i++;
				}

				if (i == argument.Length) {
					// Trailing backslashes would escape the closing quote, so double them.
					builder.Append ('\\', backslashes * 2);
				} else if (argument[i] == '"') {
					// Backslashes before a quote are doubled, then the quote is escaped.
					builder.Append ('\\', backslashes * 2 + 1);
					builder.Append ('"');
					i++;
				} else {
					builder.Append ('\\', backslashes);
					builder.Append (argument[i]);
					i++;
				}
			}

			builder.Append ('"');
		}
	}
}
