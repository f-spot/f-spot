/*
 * FSpot.Core.ColorManagement.cs
 *
 * Author(s):
 * 	Vasiliy Kirilichev <vasyok@gmail.com>
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * Copyright (c) 2008-2009 Novell, Inc.
 *
 * This is free software. See COPYING for details.
 *
 */

using System;
using System.IO;
using System.Collections.Generic;

namespace FSpot {
	public static class ColorManagement {
		static string [] search_dir = { "/usr/share/color/icc", "~/.color/icc", "/usr/local/share/color/icc " };

		static Dictionary<string, Cms.Profile> profiles;
		public static IDictionary<string, Cms.Profile> Profiles {
			get {
				if (profiles == null) {
					profiles = new Dictionary<string, Cms.Profile> ();
					Cms.Profile p = Cms.Profile.CreateStandardRgb ();
					profiles.Add (p.ProductName, p);
					p = Cms.Profile.CreateAlternateRgb ();
					profiles.Add (p.ProductName, p);
					foreach (var path in search_dir)
						AddProfiles (path, profiles);
					if (XProfile != null)
						profiles.Add ("_x_profile_", XProfile);
				}
				return profiles;
			}
		}

		static Cms.Profile x_profile;
		public static Cms.Profile XProfile {
			get {
				if (x_profile == null)
					x_profile = Cms.Profile.GetScreenProfile (Gdk.Screen.Default);
				return x_profile;
			}
		}

		private static void AddProfiles (string path, IDictionary<string, Cms.Profile> profs)
		{
			//recursive search, only RGB color profiles would be added
			if (Directory.Exists (path)) {
				string[] IccColorProfilList = System.IO.Directory.GetFiles (path, "*.icc");
				foreach (string ColorProfilePath in IccColorProfilList) {
					Cms.Profile profile = new Cms.Profile (ColorProfilePath);
					if ((Cms.IccColorSpace)profile.ColorSpace == Cms.IccColorSpace.Rgb)
						profs.Add(profile.ProductName, profile);
				}
				string[] IcmColorProfilList = System.IO.Directory.GetFiles (path, "*.icm");
				foreach (string ColorProfilePath in IcmColorProfilList) {
					Cms.Profile profile = new Cms.Profile (ColorProfilePath);
					if ((Cms.IccColorSpace)profile.ColorSpace == Cms.IccColorSpace.Rgb)
						profs.Add(profile.ProductName, profile);
				}
				string[] DirList = System.IO.Directory.GetDirectories (path);
					foreach (string dir in DirList)
						AddProfiles (dir, profs);
			}
		}
		
		public static void ApplyProfile (Gdk.Pixbuf pixbuf, Cms.Profile destination_profile)
		{
			ApplyProfile (pixbuf, Cms.Profile.CreateStandardRgb (), destination_profile);
		}

		public static void ApplyProfile (Gdk.Pixbuf pixbuf, Cms.Profile image_profile, Cms.Profile destination_profile)
		{
			if (pixbuf == null || pixbuf.HasAlpha)
				return;

			Cms.Profile [] list = new Cms.Profile [] { image_profile, destination_profile };
			Cms.Transform transform = new Cms.Transform (list,
								     PixbufUtils.PixbufCmsFormat (pixbuf),
								     PixbufUtils.PixbufCmsFormat (pixbuf),
								     Cms.Intent.Perceptual,
								     0x0000);
			PixbufUtils.ColorAdjust (pixbuf, pixbuf, transform);
		}
	}
}
