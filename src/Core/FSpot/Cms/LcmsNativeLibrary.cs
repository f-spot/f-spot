// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FSpot.Cms
{
	// lcmsNET P/Invokes the bare name "lcms2". Mono maps that through
	// lcmsNET.dll.config. .NET does not read dllmaps, so the loader only looks
	// next to the process and on the default path -- missing Homebrew and the
	// versioned SONAME. One resolver for every .NET client (Avalonia, tests).
	public static class LcmsNativeLibrary
	{
		const string DllName = "lcms2";

		public static void Register ()
		{
			lock (gate) {
				if (!registered) {
					NativeLibrary.SetDllImportResolver (typeof (lcmsNET.Profile).Assembly, Resolve);
					registered = true;
				}
			}

			// After the resolver: this is the first P/Invoke, and it has to see lcms2.
			CmsDiagnostics.Install ();
		}

		public static IReadOnlyList<string> Candidates ()
		{
			string dir = AppContext.BaseDirectory;
			var found = new List<string> ();
			foreach (string candidate in new[] {
				Environment.GetEnvironmentVariable ("FSPOT_LCMS2_LIBRARY"),
				Path.Combine (dir, "liblcms2.2.dylib"),
				Path.Combine (dir, "liblcms2.dylib"),
				Path.Combine (dir, "liblcms2.so.2"),
				Path.Combine (dir, "lcms2.dll"),
				"liblcms2.2.dylib",
				"/opt/homebrew/lib/liblcms2.2.dylib",
				"/usr/local/lib/liblcms2.2.dylib",
				"liblcms2.so.2",
				"/usr/lib/x86_64-linux-gnu/liblcms2.so.2",
				"/usr/lib/aarch64-linux-gnu/liblcms2.so.2",
				"/usr/lib64/liblcms2.so.2",
				"lcms2.dll",
				"liblcms2-2.dll",
			}) {
				if (!string.IsNullOrEmpty (candidate))
					found.Add (candidate);
			}

			return found;
		}

		static readonly object gate = new object ();
		static bool registered;

		static IntPtr Resolve (string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
		{
			if (libraryName != DllName)
				return IntPtr.Zero;

			foreach (string candidate in Candidates ()) {
				if (string.IsNullOrEmpty (candidate))
					continue;
				if (NativeLibrary.TryLoad (candidate, out var handle))
					return handle;
			}

			return IntPtr.Zero;
		}
	}
}
