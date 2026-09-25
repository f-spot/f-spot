// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Platform;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	// Core tests used to read and write ~/.config/f-spot/f-spot-settings.json.
	// A developer preference then changed assertions that assume the built-in defaults.
	[TestClass]
	public static class CoreTestHost
	{
		static string preferencesFile;

		[AssemblyInitialize]
		public static void IsolatePreferences (TestContext context)
		{
			preferencesFile = Path.Combine (Path.GetTempPath (), "f-spot-tests-" + Guid.NewGuid ().ToString ("n") + ".json");
			PreferenceJsonBackend.PreferenceLocationOverride = preferencesFile;
			PreferenceJsonBackend.Unload ();
			Environment.SetEnvironmentVariable (FSpotSentry.KillSwitchVariable, "0");
		}

		[AssemblyCleanup]
		public static void RemoveIsolatedPreferences ()
		{
			Environment.SetEnvironmentVariable (FSpotSentry.KillSwitchVariable, null);
			PreferenceJsonBackend.PreferenceLocationOverride = null;
			PreferenceJsonBackend.Unload ();
			if (preferencesFile != null && File.Exists (preferencesFile))
				File.Delete (preferencesFile);
		}
	}
}
