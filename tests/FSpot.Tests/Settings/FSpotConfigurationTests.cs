//
// FSpotConfigurationTests.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2019 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Settings;

using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FSpot.Tests.Settings
{
	[TestClass]
	public class FSpotConfigurationTests
	{
		[TestMethod]
		public void CheckPackageName ()
		{
			// This shouldn't change, but, this test will catch someone changing it
			//  For now at least
			Assert.AreEqual ("f-spot", FSpotConfiguration.Package);
		}

		[TestMethod]
		public void CheckCurrentVersion ()
		{
			Assert.AreEqual ("0.9.0", FSpotConfiguration.Version);
		}

		[TestMethod]
		[OSCondition (OperatingSystems.Linux | OperatingSystems.OSX)]
		public void BaseDirectory_UsesXdgConfigNotApplicationSupport ()
		{
			string configHome = Environment.GetEnvironmentVariable ("XDG_CONFIG_HOME");
			if (string.IsNullOrWhiteSpace (configHome))
				configHome = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), ".config");

			string expected = Path.GetFullPath (Path.Combine (configHome, "f-spot"));
			Assert.AreEqual (expected, Path.GetFullPath (FSpotConfiguration.BaseDirectory));
		}

		[TestMethod]
		public void TryApplyBaseDirectory_UsesAPresentDirectoryAndRejectsAnEmptyOne ()
		{
			string previous = FSpotConfiguration.BaseDirectory;
			try {
				string dir = Path.Combine (Path.GetTempPath (), "fspot-basedir-" + Guid.NewGuid ().ToString ("n"));
				Assert.IsTrue (FSpotConfiguration.TryApplyBaseDirectory (CommandLineOptions.Parse (new[] { "--basedir", dir })));
				Assert.AreEqual (dir, FSpotConfiguration.BaseDirectory);

				Assert.IsFalse (FSpotConfiguration.TryApplyBaseDirectory (CommandLineOptions.Parse (new[] { "--basedir=" })));
				Assert.AreEqual (dir, FSpotConfiguration.BaseDirectory);

				Assert.IsTrue (FSpotConfiguration.TryApplyBaseDirectory (CommandLineOptions.Parse (new[] { "a.jpg" })));
				Assert.AreEqual (dir, FSpotConfiguration.BaseDirectory);
			} finally {
				FSpotConfiguration.BaseDirectory = previous;
			}
		}

		[TestMethod]
		public void AppDataDir_IsUnderTheAssemblyDirectory ()
		{
			var baseDir = Path.GetFullPath (AppContext.BaseDirectory)
				.TrimEnd (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			var appData = Path.GetFullPath (FSpotConfiguration.AppDataDir);
			Assert.IsTrue (
				appData.StartsWith (baseDir + Path.DirectorySeparatorChar, StringComparison.Ordinal)
				|| appData.Equals (baseDir, StringComparison.Ordinal),
				$"AppDataDir '{appData}' is not under '{baseDir}'");
		}
	}
}
