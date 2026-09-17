//
// ColorManagement.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2014 Stephen Shaw
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008-2010 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using FSpot.Cms;
using FSpot.Gui;
using FSpot.Settings;

namespace FSpot
{
	public static class ColorManagement
	{
		static readonly string[] linuxProfileDirectories = { "/usr/share/color/icc", Path.Combine (FSpotConfiguration.HomeDirectory, ".color/icc"), "/usr/local/share/color/icc" };
		// Every ICC profile shipped with macOS lives in the first of these; it was not
		// searched at all. "/User/x/..." was a placeholder rather than a path, and the
		// two remaining entries only resolved because APFS is case-insensitive by
		// default -- the directories are ColorSync and Application Support.
		static readonly string[] macProfileDirectories = {
			"/System/Library/ColorSync/Profiles",
			"/Library/ColorSync/Profiles",
			Path.Combine (FSpotConfiguration.HomeDirectory, "Library/ColorSync/Profiles"),
			"/Library/Application Support/Adobe/Color/Profiles"
		};

		static ProfileCatalog catalog;
		public static IReadOnlyDictionary<string, Profile> Profiles {
			get {
				if (catalog == null)
					BuildProfiles ();

				return catalog.Profiles;
			}
		}

		static Profile x_profile;

		// The profile an X11 colour manager publishes on the root window's _ICC_PROFILE
		// atom; null wherever there is none, which includes every non-X11 backend.
		public static Profile XProfile {
			get {
				if (x_profile != null)
					return x_profile;
				var screen = Gdk.Screen.Default;
				if (screen == null)
					return null;
				try {
					return x_profile = CompositeUtils.GetScreenProfile (screen);
				} catch (Exception e) when (e is CmsException || e is ArgumentException
						|| ProfileCatalog.IsNativeLibraryMissing (e)) {
					// Read from BuildProfiles, so an escape here surfaces in whichever draw
					// handler touched Profiles first and leaves the catalogue half built.
					Logger.Log.Warning ($"Ignoring the unreadable _ICC_PROFILE published for the screen: {e.Message}");
					return null;
				}
			}
		}

		static string[] ColorSearchPath ()
		{
			if (Utils.Platform.IsLinux)
				return linuxProfileDirectories;
			else if (Utils.Platform.IsMac)
				return macProfileDirectories;
			else
				return Array.Empty<string> ();
		}

		static void BuildProfiles ()
		{
			// Published only once complete: a failure part-way through used to leave a
			// half-built dictionary in place, which every later lookup then trusted.
			// Missing liblcms2 (Windows, or x86_64 Mono with an arm64 Homebrew dylib)
			// becomes an empty catalogue rather than escaping into a GTK draw handler.
			catalog = ProfileCatalog.Build (built => {
				built.Add (Profile.CreateStandardRgb ());
				built.Add (Profile.CreateAlternateRgb ());

				foreach (var path in ColorSearchPath ())
					built.AddFrom (path);

				if (XProfile != null)
					built.AddShared ("_x_profile_", XProfile);
			});
		}

		public static void ApplyProfile (Gdk.Pixbuf pixbuf, Profile destinationProfile)
		{
			ApplyProfile (pixbuf, null, destinationProfile);
		}

		public static void ApplyProfile (Gdk.Pixbuf pixbuf, Profile imageProfile, Profile destinationProfile)
		{
			if (pixbuf == null || pixbuf.HasAlpha)
				return;

			try {
				imageProfile = imageProfile ?? Profile.CreateStandardRgb ();

				Profile[] list = { imageProfile, destinationProfile };
				// CollectionCellGridView calls this from its draw handler, so an undisposed
				// transform here leaks a native handle per thumbnail per repaint.
				using var transform = new Transform (list,
									 PixbufUtils.PixbufCmsFormat (pixbuf),
									 PixbufUtils.PixbufCmsFormat (pixbuf),
									 Intent.Perceptual,
									 0x0000);
				PixbufUtils.ColorAdjust (pixbuf, pixbuf, transform);
			} catch (Exception e) when (e is CmsException || ProfileCatalog.IsNativeLibraryMissing (e)) {
				// Every caller but PrintOperation is a bare call in a draw or render
				// handler, where gtk-sharp turns an escaping exception into an abort. A
				// transform lcms declines to build has to degrade to unmanaged colour.
				Logger.Log.Warning ($"Leaving the image unmanaged: {e.Message}");
			}
		}
	}
}
