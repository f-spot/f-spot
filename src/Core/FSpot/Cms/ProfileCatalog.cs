//
// ProfileCatalog.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using FSpot.Settings;

using Hyena;

namespace FSpot.Cms
{
	// The set of display profiles the UI offers, keyed by profile description because
	// that is what the preferences store. Two of the rules here are load-bearing and
	// neither is local to any one call site, which is why they live next to Profile
	// rather than in the directory scan that feeds them.
	public sealed class ProfileCatalog
	{
		readonly Dictionary<string, Profile> profiles = new Dictionary<string, Profile> (StringComparer.Ordinal);

		// Read-only on purpose: the two rules below are the reason this class exists, and
		// handing out the live dictionary would let a caller step around them.
		public IReadOnlyDictionary<string, Profile> Profiles => profiles;

		// ColorManagement.Profiles is read from draw handlers and dialog constructors.
		// gtk-sharp turns an escaping exception there into either an abort or a menu
		// click that does nothing, so a missing liblcms2 has to become an empty
		// catalogue rather than a DllNotFoundException.
		public static ProfileCatalog Build (Action<ProfileCatalog> populate)
		{
			if (populate == null)
				throw new ArgumentNullException (nameof (populate));

			var built = new ProfileCatalog ();
			try {
				populate (built);
				return built;
			} catch (Exception e) when (IsNativeLibraryMissing (e)) {
				Logger.Log.Warning ($"Colour management disabled: native Little CMS is not available ({e.Message})");
				return new ProfileCatalog ();
			}
		}

		// A missing native library reaches managed code in more shapes than one: too old
		// a liblcms2 has no cmsGetEncodedCMMversion, an architecture mismatch is a bad
		// image, and any of them raised from a static constructor arrives wrapped.
		public static bool IsNativeLibraryMissing (Exception e)
		{
			if (e == null)
				throw new ArgumentNullException (nameof (e));

			if (e is TypeInitializationException && e.InnerException != null)
				return IsNativeLibraryMissing (e.InnerException);

			return e is DllNotFoundException || e is EntryPointNotFoundException || e is BadImageFormatException;
		}

		// Takes ownership: a profile the catalogue declines is closed here, because the
		// directory scan that produced it has nowhere else to put it.
		public bool Add (Profile profile)
		{
			if (profile == null)
				throw new ArgumentNullException (nameof (profile));

			// The empty description is the "colour management off" sentinel -- the
			// preference defaults to it and the dialog writes it for "None" -- so a
			// profile with no readable description must never become that key, or it
			// switches colour management on for a user who never enabled it.
			string description = profile.ProductDescription;
			if (string.IsNullOrEmpty (description)
					|| !Admitted (profile)
					|| profiles.ContainsKey (description)) {
				profile.Dispose ();
				return false;
			}

			profiles.Add (description, profile);
			return true;
		}

		// Display profiles are RGB. A printer profile is often CMYK, and the print
		// list is the only one that asks for Output, so that is the only non-RGB
		// space admitted. Gray and a CMYK profile tagged as a display stay out.
		static bool Admitted (Profile profile)
		{
			if (profile.ColorSpace == IccColorSpace.Rgb)
				return true;

			return profile.DeviceClass == IccProfileClass.Output
				&& profile.ColorSpace == IccColorSpace.Cmyk;
		}

		// Adds every profile below a directory, recursively. A directory or a file it
		// cannot read is skipped rather than abandoning the walk: the search paths
		// include user-writable trees, and the catalogue is published only once the walk
		// completes, so an escape here would repeat on every later lookup.
		public static IReadOnlyList<string> Directories (CmsPlatform platform, string home, string systemRoot)
		{
			if (platform == CmsPlatform.Linux) {
				return new[] {
					"/usr/share/color/icc",
					Path.Combine (home ?? string.Empty, ".color/icc"),
					"/usr/local/share/color/icc"
				};
			}

			if (platform == CmsPlatform.Mac) {
				return new[] {
					"/System/Library/ColorSync/Profiles",
					"/Library/ColorSync/Profiles",
					Path.Combine (home ?? string.Empty, "Library/ColorSync/Profiles"),
					"/Library/Application Support/Adobe/Color/Profiles"
				};
			}

			// GetColorDirectory returns %SystemRoot%\System32\spool\drivers\color.
			// A per-user choice is a registry association to a file in that directory,
			// not a second tree of profiles.
			if (platform == CmsPlatform.Windows) {
				string root = string.IsNullOrEmpty (systemRoot) ? @"C:\Windows" : systemRoot;
				return new[] { Path.Combine (root, "System32", "spool", "drivers", "color") };
			}

			return Array.Empty<string> ();
		}

		public static IReadOnlyList<string> InstalledDirectories ()
		{
			CmsPlatform platform = CmsPlatform.Linux;
			if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
				platform = CmsPlatform.Mac;
			else if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows))
				platform = CmsPlatform.Windows;
			else if (!RuntimeInformation.IsOSPlatform (OSPlatform.Linux))
				return Array.Empty<string> ();

			return Directories (platform, FSpotConfiguration.HomeDirectory, Environment.GetEnvironmentVariable ("SystemRoot"));
		}

		public void AddInstalled ()
		{
			Add (Profile.CreateStandardRgb ());
			Add (Profile.CreateAlternateRgb ());
			foreach (string path in InstalledDirectories ())
				AddFrom (path);
		}

		public void AddFrom (string directory)
		{
			if (directory == null)
				throw new ArgumentNullException (nameof (directory));

			if (!Directory.Exists (directory))
				return;

			AddFrom (directory, "*.icc");
			AddFrom (directory, "*.icm");

			foreach (string child in Enumerate (directory, () => Directory.GetDirectories (directory)))
				AddFrom (child);
		}

		void AddFrom (string directory, string pattern)
		{
			foreach (string path in Enumerate (directory, () => Directory.GetFiles (directory, pattern))) {
				try {
					// The catalogue closes what it declines; on macOS that is most of the
					// system profiles, since only RGB ones are offered.
					Add (new Profile (path));
				} catch (CmsException e) {
					Logger.Log.Warning ($"Ignoring the unreadable ICC profile {path}: {e.Message}");
				}
			}
		}

		static string[] Enumerate (string directory, Func<string[]> list)
		{
			try {
				return list ();
			} catch (Exception e) when (e is IOException || e is UnauthorizedAccessException
					|| e is System.Security.SecurityException) {
				Logger.Log.Warning ($"Skipping the unreadable profile directory {directory}: {e.Message}");
				return Array.Empty<string> ();
			}
		}

		// Borrows: the caller keeps ownership, which is what ColorManagement.XProfile
		// needs since it caches and republishes the same instance.
		public bool AddShared (string key, Profile profile)
		{
			if (key == null)
				throw new ArgumentNullException (nameof (key));
			if (profile == null)
				throw new ArgumentNullException (nameof (profile));

			if (profiles.ContainsKey (key))
				return false;

			profiles.Add (key, profile);
			return true;
		}
	}
}
