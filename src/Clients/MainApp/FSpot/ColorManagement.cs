//
// ColorManagement.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008-2010 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Collections.Generic;

using FSpot.Core;

namespace FSpot {
	public static class ColorManagement {
		static string [] search_dir = { "/usr/share/color/icc", Path.Combine (Global.HomeDirectory, ".color/icc"), "/usr/local/share/color/icc " };

		static Dictionary<string, Cms.Profile> profiles;
		public static IDictionary<string, Cms.Profile> Profiles {
			get {
				if (profiles == null) {
					profiles = new Dictionary<string, Cms.Profile> ();
					Cms.Profile p = Cms.Profile.CreateStandardRgb ();
					if (!profiles.ContainsKey (p.ProductDescription))
						profiles.Add (p.ProductDescription, p);

					p = Cms.Profile.CreateAlternateRgb ();
					if (!profiles.ContainsKey (p.ProductDescription))
						profiles.Add (p.ProductDescription, p);

					foreach (var path in search_dir)
						if (!profiles.ContainsKey (path))
							AddProfiles (path, profiles);

					if (XProfile != null)
						if (!profiles.ContainsKey ("_x_profile_"))
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
					if ((Cms.IccColorSpace)profile.ColorSpace == Cms.IccColorSpace.Rgb && profile.ProductDescription != null && !profs.ContainsKey (profile.ProductDescription))
						profs.Add(profile.ProductDescription, profile);
				}
				string[] IcmColorProfilList = System.IO.Directory.GetFiles (path, "*.icm");
				foreach (string ColorProfilePath in IcmColorProfilList) {
					Cms.Profile profile = new Cms.Profile (ColorProfilePath);
					if ((Cms.IccColorSpace)profile.ColorSpace == Cms.IccColorSpace.Rgb && profile.ProductDescription != null && !profs.ContainsKey (profile.ProductDescription))
						profs.Add(profile.ProductDescription, profile);
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

			image_profile = image_profile ?? Cms.Profile.CreateStandardRgb ();

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
