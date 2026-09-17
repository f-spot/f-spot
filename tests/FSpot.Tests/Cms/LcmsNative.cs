// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Cms.Tests
{
	// Production .NET clients register LcmsNativeLibrary so lcmsNET's "lcms2"
	// import finds Homebrew / the versioned SONAME. Tests still probe here so a
	// machine with no library reports Inconclusive instead of a wall of failures.
	static class LcmsNative
	{
		static readonly object gate = new object ();
		static string loadFailure;

		public static void Require ()
		{
			lock (gate) {
				LcmsNativeLibrary.Register ();

				if (loadFailure == null) {
					try {
						_ = lcmsNET.Cms.EncodedCMMVersion;
						loadFailure = string.Empty;
					} catch (Exception e) when (IsMissingLibrary (e)) {
						loadFailure = e.Message;
					}
				}
			}

			if (loadFailure.Length == 0)
				return;

			string message = $"liblcms2 is not available on this machine: {loadFailure}";
			if (string.IsNullOrEmpty (Environment.GetEnvironmentVariable ("FSPOT_REQUIRE_LCMS2")))
				Assert.Inconclusive (message);

			Assert.Fail ($"FSPOT_REQUIRE_LCMS2 is set but {message}");
		}

		internal static bool IsMissingLibrary (Exception e)
			=> ProfileCatalog.IsNativeLibraryMissing (e);
	}
}
