// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Gtk.Tests
{
	// Gtk.Widget.Show is not modal. `using var w = …; w.Show ();` (or Add) disposes
	// the C# wrapper when the method returns. For the Import thumbnail strip that
	// was BrowseablePointerGridView: native expose still ran, the pixbuf cache was
	// already torn down, ScaleSimple returned null, DrawPhoto NRE'd.
	[TestClass]
	public class DialogLifetimeTests
	{
		[TestMethod]
		public void UsingVarDoesNotDisposeAWidgetThatIsShownOrParented ()
		{
			var failures = new List<string> ();
			string gtk = Path.Combine (RepoRoot (), "src", "Clients", "FSpot.Gtk");

			foreach (string path in Directory.GetFiles (gtk, "*.cs", SearchOption.AllDirectories)) {
				string source = File.ReadAllText (path);
				foreach (Match match in Regex.Matches (source,
					@"using var (\w+) = new (\w+)\s*\((?<args>[\s\S]*?)\);(?<body>[\s\S]{0,400})")) {
					string name = match.Groups[1].Value;
					string type = match.Groups[2].Value;
					string body = match.Groups["body"].Value;
					if (Regex.IsMatch (body, $@"\b{name}\.Run\s*\("))
						continue;
					bool shown = Regex.IsMatch (body, $@"\b{name}\.Show\s*\(");
					bool parented = Regex.IsMatch (body, $@"\.Add\s*\(\s*{name}\b");
					if (!shown && !parented)
						continue;

					failures.Add ($"{Relative (path)}: using var {name} = new {type} is disposed after Show/Add");
				}
			}

			Assert.IsEmpty (failures, string.Join ("; ", failures));
		}

		static string Relative (string path)
		{
			string root = RepoRoot ();
			return path.StartsWith (root, StringComparison.Ordinal)
				? path.Substring (root.Length).TrimStart (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
				: path;
		}

		static string RepoRoot ()
		{
			for (var dir = new DirectoryInfo (AppContext.BaseDirectory); dir != null; dir = dir.Parent) {
				if (File.Exists (Path.Combine (dir.FullName, "F-Spot.slnx")))
					return dir.FullName;
			}

			Assert.Fail ("F-Spot.slnx not found walking up from " + AppContext.BaseDirectory);
			return null;
		}
	}
}
