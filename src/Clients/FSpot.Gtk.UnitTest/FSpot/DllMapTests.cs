//
// DllMapTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Gtk.UnitTest
{
	// Mono resolves f-spot.exe's native imports through these dllmaps. An entry with no
	// os attribute applies everywhere, so a Linux .so target with no attribute hands
	// macOS a library that cannot exist -- which is how three entries here were left
	// behind when their siblings gained quartz targets. Written as a sweep over the
	// whole file rather than a check of the three, so the next one is caught too.
	[TestClass]
	public class DllMapTests
	{
		[TestMethod]
		public void EveryElfTargetIsExcludedOnMacOs ()
		{
			var ungated = DllMaps ()
				.Where (map => IsElf ((string)map.Attribute ("target")))
				.Where (map => !ExcludesMacOs ((string)map.Attribute ("os")))
				.Select (map => $"{(string)map.Attribute ("dll")} -> {(string)map.Attribute ("target")}")
				.OrderBy (name => name, StringComparer.Ordinal)
				.ToList ();

			Assert.IsEmpty (ungated, "these map to an ELF library on macOS: " + string.Join (", ", ungated));
		}

		static IEnumerable<XElement> DllMaps ()
		{
			string path = Path.Combine (AppContext.BaseDirectory, "f-spot.exe.config");
			Assert.IsTrue (File.Exists (path), $"{path} is missing; the build should copy f-spot.exe.config.in over it.");

			return XDocument.Load (path).Descendants ("dllmap");
		}

		static bool IsElf (string target) =>
			target != null && (target.EndsWith (".so", StringComparison.Ordinal) || target.Contains (".so."));

		// Mono reads os as a set, negated as a whole when it starts with "!".
		static bool ExcludesMacOs (string os)
		{
			if (string.IsNullOrEmpty (os))
				return false;

			bool negated = os[0] == '!';
			bool listed = os.TrimStart ('!').Split (',').Any (name => name.Trim () == "osx");

			return negated ? listed : !listed;
		}
	}
}
