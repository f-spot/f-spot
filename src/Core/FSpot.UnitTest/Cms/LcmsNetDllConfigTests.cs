//
// LcmsNetDllConfigTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Cms.UnitTest
{
	// lcmsNET imports the bare name "lcms2". Mono resolves that through a dllmap,
	// and the dllmap must sit in the declaring assembly's own config -- an entry in
	// FSpot.dll.config is never consulted for lcmsNET's imports. The package ships
	// no config, so F-Spot has to, and it has to name exactly what lcmsNET imports.
	[TestClass]
	public class LcmsNetDllConfigTests
	{
		static readonly Assembly lcmsNetAssembly = typeof (lcmsNET.Profile).Assembly;

		static string ConfigPath => Path.Combine (AppContext.BaseDirectory, "lcmsNET.dll.config");

		[TestMethod]
		public void LcmsNetDllConfigIsDeployedNextToTheAssembly ()
		{
			Assert.IsTrue (File.Exists (ConfigPath),
				$"{ConfigPath} is missing: without it Mono cannot find liblcms2 on any box that "
				+ "only has the versioned soname.");
		}

		[TestMethod]
		public void LcmsNetDllConfigMapsExactlyTheLibrariesLcmsNetImports ()
		{
			var mapped = Dllmaps ()
				.Select (element => (string)element.Attribute ("dll"))
				.Distinct (StringComparer.Ordinal)
				.OrderBy (name => name, StringComparer.Ordinal)
				.ToList ();

			var imported = ImportedLibraries (lcmsNetAssembly);

			Assert.IsNotEmpty (imported, "lcmsNET declares no DllImports; the reflection sweep is broken");
			Assert.AreSequenceEqual (imported, mapped,
				$"dllmap drift: lcmsNET.dll.config maps [{string.Join (", ", mapped)}] "
				+ $"but lcmsNET.dll imports [{string.Join (", ", imported)}]");
		}

		[TestMethod]
		public void LcmsNetDllConfigCoversLinuxAndMacWithDistinctTargets ()
		{
			var maps = Dllmaps ().ToList ();

			var mac = maps.Where (m => ((string)m.Attribute ("os") ?? string.Empty).Split (',').Contains ("osx")).ToList ();
			var linux = maps.Where (m => ((string)m.Attribute ("os") ?? string.Empty).StartsWith ("!", StringComparison.Ordinal)).ToList ();

			Assert.HasCount (1, mac, "expected exactly one osx dllmap");
			Assert.HasCount (1, linux, "expected exactly one dllmap for everything that is not osx/windows");
			Assert.EndsWith (".dylib", (string)mac[0].Attribute ("target"));
			Assert.Contains (".so.", (string)linux[0].Attribute ("target"));
			Assert.Contains ("osx", (string)linux[0].Attribute ("os"),
				"the Linux mapping must exclude osx or Mono on a Mac tries the .so first");
		}

		static IEnumerable<XElement> Dllmaps ()
		{
			Assert.IsTrue (File.Exists (ConfigPath), $"{ConfigPath} is missing");
			return XDocument.Load (ConfigPath).Descendants ("dllmap");
		}

		static List<string> ImportedLibraries (Assembly assembly)
		{
			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic
				| BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

			return assembly.GetTypes ()
				.SelectMany (type => type.GetMethods (flags))
				.Select (method => method.GetCustomAttribute<DllImportAttribute> ())
				.Where (attribute => attribute != null)
				.Select (attribute => attribute.Value)
				.Distinct (StringComparer.Ordinal)
				.OrderBy (name => name, StringComparer.Ordinal)
				.ToList ();
		}
	}
}
