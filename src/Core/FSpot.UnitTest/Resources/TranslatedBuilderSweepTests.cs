// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.UnitTest.Resources
{
	[TestClass]
	public class TranslatedBuilderSweepTests
	{
		[TestMethod]
		public void AppCode_DoesNotLoadUiXmlThroughUntranslatedBuilder ()
		{
			var root = FindRepoRoot ();
			var src = Path.Combine (root, "src");
			Assert.IsTrue (Directory.Exists (src), src);

			var hits = new List<string> ();
			foreach (var file in Directory.EnumerateFiles (src, "*.cs", SearchOption.AllDirectories)) {
				if (file.Contains ("UnitTest", StringComparison.Ordinal)
					|| file.EndsWith ($"{Path.DirectorySeparatorChar}TranslatedBuilder.cs", StringComparison.Ordinal))
					continue;

				var text = File.ReadAllText (file);
				if (text.Contains ("new GtkBeans.Builder", StringComparison.Ordinal)
					|| text.Contains ("new Builder (\"", StringComparison.Ordinal))
					hits.Add (Path.GetRelativePath (root, file));
			}

			Assert.IsEmpty (hits);
		}

		static string FindRepoRoot ()
		{
			foreach (var start in new[] { Directory.GetCurrentDirectory (), AppContext.BaseDirectory }) {
				for (var dir = new DirectoryInfo (start); dir != null; dir = dir.Parent) {
					if (File.Exists (Path.Combine (dir.FullName, "F-Spot.slnx")))
						return dir.FullName;
				}
			}

			Assert.Fail ("Could not find the repository root (F-Spot.slnx).");
			return null;
		}
	}
}
