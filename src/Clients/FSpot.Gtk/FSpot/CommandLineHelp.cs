//
// CommandLineHelp.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSpot
{
	// Replaces Hyena.CommandLine's Layout, LayoutGroup and LayoutOption: 242 lines of
	// terminal-width-aware wrapping that only ever formatted this fixed table of twelve
	// options. At the 80 columns it assumed when Console.WindowWidth was unavailable, the
	// longest description is 37 against a 58-column budget, so the wrapping never ran.
	//
	// Layout also carried a trap worth not reproducing: alongside the ToString () override
	// it declared ToString (params string[]), so writing commands.ToString () in C# bound
	// to the params overload with zero arguments and returned the empty string. Only
	// Console.WriteLine (commands), which dispatches ToString () virtually at runtime,
	// produced the help text.
	static class CommandLineHelp
	{
		// Layout put six spaces between the longest option and the descriptions.
		const int Gutter = 6;

		static readonly IReadOnlyDictionary<string, (string Title, (string Name, string Description)[] Options)> groups =
			new Dictionary<string, (string, (string, string)[])> {
				["help"] = ("Help Options", new[] {
					("help", "Show this help"),
					("help-options", "Show command line options"),
					("help-all", "Show all options"),
					("version", "Show version information"),
					("versions", "Show detailed version information"),
				}),
				["options"] = ("General options", new[] {
					("basedir=DIR", "Path to the photo database folder"),
					("import=URI", "Import from the given uri"),
					("photodir=DIR", "Default import folder"),
					("view ITEM", "View file(s) or directories"),
					("shutdown", "Shut down a running instance of F-Spot"),
					("slideshow", "Display a slideshow"),
					("debug", "Run in debug mode"),
				}),
			};

		public static string Section (string groupId)
		{
			return groups.TryGetValue (groupId, out var group)
				? Render (new[] { group })
				: string.Empty;
		}

		public static string Everything ()
		{
			return Render (groups.Values.ToArray ());
		}

		// Internal so the alignment rule can be tested against sections whose longest
		// options differ. The two real ones are both twelve characters wide, so nothing
		// driven through Section or Everything can tell the rule from a per-section one.
		internal static string Render (IReadOnlyList<(string Title, (string Name, string Description)[] Options)> sections)
		{
			// Aligned across every section being rendered, not per section, which is what
			// Layout did -- it measured the groups handed to one ToString call.
			int width = sections.SelectMany (s => s.Options).Max (o => o.Name.Length) + Gutter;

			var builder = new StringBuilder ();
			for (int i = 0; i < sections.Count; i++) {
				builder.AppendLine (sections[i].Title);
				builder.AppendLine ();

				foreach (var (name, description) in sections[i].Options)
					builder.AppendLine ($"  --{name.PadRight (width)}{description}");

				if (i < sections.Count - 1)
					builder.AppendLine ();
			}

			return builder.ToString ();
		}
	}
}
