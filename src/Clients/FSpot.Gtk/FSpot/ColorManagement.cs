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
		static readonly string[] windowsProfileDirectories = { @"C:\Windows\System32\spool\drivers\color" };
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

		static Dictionary<string, Profile> profiles;
		public static IDictionary<string, Profile> Profiles {
			get {
				if (profiles == null)
					BuildProfiles ();

				return profiles;
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
				} catch (CmsException e) {
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
			else if (Utils.Platform.IsWindows)
				return windowsProfileDirectories;
			else
				return Array.Empty<string> ();
		}

		static void BuildProfiles ()
		{
			profiles = new Dictionary<string, Profile> ();
			if (Utils.Platform.IsWindows) // FIXME, add lcms support
				return;

			var p = Profile.CreateStandardRgb ();
			if (!profiles.ContainsKey (p.ProductDescription))
				profiles.Add (p.ProductDescription, p);

			p = Profile.CreateAlternateRgb ();
			if (!profiles.ContainsKey (p.ProductDescription))
				profiles.Add (p.ProductDescription, p);

			foreach (var path in ColorSearchPath ())
				if (!profiles.ContainsKey (path))
					AddProfiles (path);

			if (XProfile != null)
				if (!profiles.ContainsKey ("_x_profile_"))
					profiles.Add ("_x_profile_", XProfile);
		}

		static void AddProfiles (string path)
		{
			//recursive search, only RGB color profiles would be added
			if (!Directory.Exists (path))
				return;

			AddProfilesByExtension (path, "*.icc");
			AddProfilesByExtension (path, "*.icm");

			var DirList = Directory.GetDirectories (path);
			foreach (string dir in DirList)
				AddProfiles (dir);
		}

		static void AddProfilesByExtension (string path, string fileExtension)
		{
			var colorProfilList = Directory.GetFiles (path, fileExtension);
			foreach (string ColorProfilePath in colorProfilList) {
				try {
					var profile = new Profile (ColorProfilePath);
					if (profile.ColorSpace == IccColorSpace.Rgb && profile.ProductDescription != null && !profiles.ContainsKey (profile.ProductDescription))
						profiles.Add (profile.ProductDescription, profile);
				} catch (CmsException CmsEx) {
					Console.WriteLine (CmsEx);
				}
			}
		}

		public static void ApplyProfile (Gdk.Pixbuf pixbuf, Profile destinationProfile)
		{
			ApplyProfile (pixbuf, Profile.CreateStandardRgb (), destinationProfile);
		}

		public static void ApplyProfile (Gdk.Pixbuf pixbuf, Profile imageProfile, Profile destinationProfile)
		{
			if (pixbuf == null || pixbuf.HasAlpha)
				return;

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
		}
	}
}
