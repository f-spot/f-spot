//
// LcmsNative.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Cms.Tests
{
	// The tests run on .NET, where lcmsNET's bare "lcms2" import is probed only in the
	// application directory and the loader's default search path. That misses the
	// usual homes of a system liblcms2 -- Homebrew's /opt/homebrew/lib above all -- so
	// the tests register a resolver for the assembly that declares the import. Under
	// Mono the shipped lcmsNET.dll.config does this job instead.
	static class LcmsNative
	{
		static readonly object gate = new object ();
		static bool registered;
		static string loadFailure;

		static readonly string[] candidates = {
			Environment.GetEnvironmentVariable ("FSPOT_LCMS2_LIBRARY"),
			"liblcms2.2.dylib",
			"/opt/homebrew/lib/liblcms2.2.dylib",
			"/usr/local/lib/liblcms2.2.dylib",
			"liblcms2.so.2",
			"/usr/lib/x86_64-linux-gnu/liblcms2.so.2",
			"/usr/lib/aarch64-linux-gnu/liblcms2.so.2",
			"/usr/lib64/liblcms2.so.2",
			"lcms2.dll",
			"liblcms2-2.dll",
		};

		// Call at the top of any test that reaches liblcms2. Skips (Inconclusive) rather
		// than failing on a box with no liblcms2, and says why; passing silently without
		// the library is what this must never do. Set FSPOT_REQUIRE_LCMS2 where the
		// library is supposed to be installed -- CI does -- and a missing one fails the
		// run instead of skipping quietly.
		public static void Require ()
		{
			lock (gate) {
				if (!registered) {
					NativeLibrary.SetDllImportResolver (typeof (lcmsNET.Profile).Assembly, Resolve);
					registered = true;
				}

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

		// A missing native library reaches managed code in more shapes than one: too old
		// a liblcms2 has no cmsGetEncodedCMMversion, an architecture mismatch is a bad
		// image, and any of them raised from a static constructor arrives wrapped.
		internal static bool IsMissingLibrary (Exception e)
		{
			if (e is TypeInitializationException && e.InnerException != null)
				return IsMissingLibrary (e.InnerException);

			return e is DllNotFoundException || e is EntryPointNotFoundException || e is BadImageFormatException;
		}

		static IntPtr Resolve (string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
		{
			if (libraryName != "lcms2")
				return IntPtr.Zero;

			foreach (string candidate in Candidates ())
				if (NativeLibrary.TryLoad (candidate, out var handle))
					return handle;

			return IntPtr.Zero;
		}

		static IEnumerable<string> Candidates ()
		{
			foreach (string candidate in candidates)
				if (!string.IsNullOrEmpty (candidate))
					yield return candidate;
		}
	}
}
