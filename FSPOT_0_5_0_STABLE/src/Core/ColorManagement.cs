/*
 * FSpot.Core.ColorManagement.cs
 *
 * Author(s):
 * 	Vasiliy Kirilichev <vasyok@gmail.com>
 *
 * This is free software. See COPYING for details.
 *
 */

using System;
using System.IO;
using System.Collections;

namespace FSpot {
public class ColorManagement {
		public static bool IsEnabled;
		public static bool UseXProfile;
		
		private static Cms.Profile XProfile;		
		private static Cms.Profile display_profile;
		private static Cms.Profile destination_profile;
		private static Cms.Transform standart_transform;
		
		private static FSpot.PhotoImageView photo_image_view;

		private static string [] search_dir = { "/usr/share/color/icc", "~/.color/icc", "/usr/local/share/color/icc " }; //the main directory list to find a profiles
		public static ArrayList Profiles = new ArrayList();
		
		public static FSpot.PhotoImageView PhotoImageView {
			set {
				if (photo_image_view == null)
					photo_image_view = value;
			}
			get { return photo_image_view; }
		}
		
		public static Cms.Profile DisplayProfile {
			set {
				display_profile = value;
				foreach (Cms.Profile profile in Profiles)
					if (profile.ProductName == display_profile.ProductName)
						Preferences.Set (Preferences.COLOR_MANAGEMENT_DISPLAY_PROFILE, profile.ProductName);
			}
			get { return display_profile; }
		}

		public static Cms.Profile DestinationProfile {
			set {
				destination_profile = value;
				foreach (Cms.Profile profile in Profiles)
					if (profile.ProductName == destination_profile.ProductName)
						Preferences.Set (Preferences.COLOR_MANAGEMENT_OUTPUT_PROFILE, profile.ProductName);
			}
			get { return destination_profile; }
		}
		
		private static void GetStandartProfiles ()
		{
			Profiles.Add (Cms.Profile.CreateStandardRgb ());
			Profiles.Add (Cms.Profile.CreateAlternateRgb ());
		}
		
		private static void GetProfiles (string path)
		{
			//recursive search, only RGB color profiles would be added
			if (Directory.Exists (path)) {
				string[] IccColorProfilList = System.IO.Directory.GetFiles (path, "*.icc");
				foreach (string ColorProfilePath in IccColorProfilList) {
					Cms.Profile profile = new Cms.Profile (ColorProfilePath);
					if ((Cms.IccColorSpace)profile.ColorSpace == Cms.IccColorSpace.Rgb)
						Profiles.Add(profile);
				}
				string[] IcmColorProfilList = System.IO.Directory.GetFiles (path, "*.icm");
				foreach (string ColorProfilePath in IcmColorProfilList) {
					Cms.Profile profile = new Cms.Profile (ColorProfilePath);
					if ((Cms.IccColorSpace)profile.ColorSpace == Cms.IccColorSpace.Rgb)
						Profiles.Add(profile);
				}
				string[] DirList = System.IO.Directory.GetDirectories (path);
					foreach (string dir in DirList)
						GetProfiles (dir);
			}
		}
		
		private static void GetXProfile ()
		{
			Gdk.Screen screen = Gdk.Screen.Default;
			XProfile = Cms.Profile.GetScreenProfile (screen);
			
			if (XProfile != null && (Cms.IccColorSpace)XProfile.ColorSpace == Cms.IccColorSpace.Rgb) {
				int a = 0;
				foreach (Cms.Profile profile in Profiles) {
					if (profile.ProductName == XProfile.ProductName)
						break;
					a++;
				}
				if (a == Profiles.Count)
					Profiles.Add (XProfile);
			}	
		}
		
		private static void GetSettings ()
		{	
			//Display
			if (UseXProfile && XProfile != null  && (Cms.IccColorSpace)XProfile.ColorSpace == Cms.IccColorSpace.Rgb)
				display_profile = XProfile;
			else {
				foreach (Cms.Profile profile in Profiles)
					if (profile.ProductName == Preferences.Get<string> (Preferences.COLOR_MANAGEMENT_DISPLAY_PROFILE))
						display_profile = profile;
				if (display_profile == null)
					display_profile = Cms.Profile.CreateStandardRgb ();
			}

			//Output
			foreach (Cms.Profile profile in Profiles)
				if (profile.ProductName == Preferences.Get<string> (Preferences.COLOR_MANAGEMENT_OUTPUT_PROFILE))
					destination_profile = profile;
			if (destination_profile == null)
				destination_profile = Cms.Profile.CreateStandardRgb ();
		}
		
		public static void LoadSettings ()
		{
			Profiles.Clear ();
			GetStandartProfiles ();
			foreach (string dir in search_dir)
				GetProfiles (dir);
			GetXProfile ();
			GetSettings ();
			CreateStandartTransform ();
		}
		
		public static void ReloadSettings()
		{
			GetXProfile ();
			GetSettings ();
			CreateStandartTransform ();
			PhotoImageView.Reload ();
		}
		
		private static void CreateStandartTransform ()
		{
			Cms.Profile [] list = new Cms.Profile [] { Cms.Profile.CreateStandardRgb (), display_profile };
			
			standart_transform = new Cms.Transform (list,
			                                        Cms.Format.Rgb8,
			                                        Cms.Format.Rgb8,
			                                        Cms.Intent.Perceptual, 0x0000);
		}
		
		//it returns the cached transformation
		public static Cms.Transform StandartTransform ()
		{
			if (IsEnabled)
				return standart_transform;
			else
				return null;
		}
		
		//this method create the Cms.Transform using image_profile and current screen profile
		public static Cms.Transform CreateTransform (Gdk.Pixbuf pixbuf, Cms.Profile image_profile)
		{
			if (IsEnabled && pixbuf != null) {
				if (image_profile == null)
					image_profile = Cms.Profile.CreateStandardRgb ();
		
				Cms.Profile [] list = new Cms.Profile [] { image_profile, display_profile };
				
				Cms.Transform transform = new Cms.Transform (list,
				                                             PixbufUtils.PixbufCmsFormat (pixbuf),
				                                             PixbufUtils.PixbufCmsFormat (pixbuf),
				                                             Cms.Intent.Perceptual, 0x0000);
				return transform;
			}
			else
				return null;
		}
		
		//the main apply color profile method. it use the _standart_ image profile and current display profile
		public static void ApplyScreenProfile (Gdk.Pixbuf pixbuf)
		{
			if (IsEnabled && pixbuf != null && !pixbuf.HasAlpha)
				PixbufUtils.ColorAdjust (pixbuf, pixbuf, standart_transform);
		}
		
		//it works also but it uses the image_profile too
		public static void ApplyScreenProfile (Gdk.Pixbuf pixbuf, Cms.Profile image_profile)
		{
			if (IsEnabled && pixbuf != null && !pixbuf.HasAlpha) {
				if (image_profile == null)
					ApplyScreenProfile (pixbuf);
				else {
					Cms.Profile [] list = new Cms.Profile [] { image_profile, display_profile };
					Cms.Transform transform = new Cms.Transform (list,
					                                             PixbufUtils.PixbufCmsFormat (pixbuf),
					                                             PixbufUtils.PixbufCmsFormat (pixbuf),
					                                             Cms.Intent.Perceptual, 0x0000);
					PixbufUtils.ColorAdjust (pixbuf, pixbuf, transform);
				}
			}
		}
		
//		public static void ApplyScreenProfile (Gdk.Pixbuf src, Gdk.Pixbuf dest)
//		{
//			PixbufUtils.ColorAdjust (src, dest, standart_transform);
//		}
		
		public static void ApplyPrinterProfile (Gdk.Pixbuf pixbuf, Cms.Profile image_profile)
		{
			if (IsEnabled && pixbuf != null && !pixbuf.HasAlpha) {
				if (image_profile == null)
					image_profile = Cms.Profile.CreateStandardRgb ();
				
				Cms.Profile [] list = new Cms.Profile [] { image_profile, destination_profile };
				
				Cms.Transform transform = new Cms.Transform (list,
				                                             PixbufUtils.PixbufCmsFormat (pixbuf),
				                                             PixbufUtils.PixbufCmsFormat (pixbuf),
				                                             Cms.Intent.Perceptual, 0x0000);
				PixbufUtils.ColorAdjust (pixbuf, pixbuf, transform);
			}
		}
	}
}
