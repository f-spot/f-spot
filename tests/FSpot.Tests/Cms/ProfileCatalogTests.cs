//
// ProfileCatalogTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using FSpot.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Cms.Tests
{
	[TestClass]
	public class ProfileCatalogTests
	{
		readonly TemporaryDirectories temporaryDirectories = new TemporaryDirectories ();

		[TestInitialize]
		public void RequireLcms ()
		{
			LcmsNative.Require ();
		}

		[TestCleanup]
		public void RemoveTemporaryDirectories ()
		{
			temporaryDirectories.DeleteAll ();
		}

		[TestMethod]
		public void InstalledDirectories_FollowThePlatformColorPaths ()
		{
			string[] paths = ProfileCatalog.InstalledDirectories ().ToArray ();
			if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
				Assert.Contains ("/System/Library/ColorSync/Profiles", paths);
			else if (RuntimeInformation.IsOSPlatform (OSPlatform.Linux))
				Assert.Contains ("/usr/share/color/icc", paths);
			else {
				string root = Environment.GetEnvironmentVariable ("SystemRoot");
				if (string.IsNullOrEmpty (root))
					root = @"C:\Windows";
				Assert.Contains (Path.Combine (root, "System32", "spool", "drivers", "color"), paths);
			}
		}

		[TestMethod]
		public void Add_RegistersAnRgbProfileUnderItsDescription ()
		{
			var catalog = new ProfileCatalog ();
			var profile = Profile.CreateAlternateRgb ();

			Assert.IsTrue (catalog.Add (profile));
			Assert.AreSame (profile, catalog.Profiles[profile.ProductDescription]);
		}

		// The empty string is the "colour management off" sentinel: the preference
		// defaults to it, the preference dialog writes it for "None", and every render
		// path looks the preference up with TryGetValue. A profile registered under it
		// switches colour management on for a user who never enabled it.
		[TestMethod]
		public void Add_DoesNotRegisterAProfileWithNoDescription ()
		{
			var catalog = new ProfileCatalog ();
			var profile = new Profile (MinimalIccProfile.WithColorSpace ("RGB "));

			Assert.AreEqual (string.Empty, profile.ProductDescription, "the fixture should have no description");
			Assert.IsFalse (catalog.Add (profile));
			Assert.IsEmpty (catalog.Profiles);
		}

		[TestMethod]
		public void Add_DoesNotRegisterANonRgbProfile ()
		{
			var catalog = new ProfileCatalog ();
			var profile = new Profile (MinimalIccProfile.WithColorSpace ("GRAY"));

			Assert.IsFalse (catalog.Add (profile));
			Assert.IsEmpty (catalog.Profiles);
		}

		// The print dropdown asks for Output profiles. A CMYK printer profile is the
		// usual one, and the display list never asks for Output, so it stays RGB.
		[TestMethod]
		public void Add_RegistersACmykPrinterProfileUnderItsDescription ()
		{
			var catalog = new ProfileCatalog ();
			var profile = new Profile (DescribedProfile.Output ("CMYK", "Generic CMYK"));

			Assert.AreEqual ("Generic CMYK", profile.ProductDescription);
			Assert.AreEqual (IccProfileClass.Output, profile.DeviceClass);
			Assert.AreNotEqual (IccColorSpace.Rgb, profile.ColorSpace);
			Assert.IsTrue (catalog.Add (profile));
			Assert.AreSame (profile, catalog.Profiles["Generic CMYK"]);
		}

		[TestMethod]
		public void Add_StillRefusesGrayAndACmykProfileThatIsNotAPrinter ()
		{
			var catalog = new ProfileCatalog ();
			var gray = new Profile (DescribedProfile.Display ("GRAY", "Gray display"));
			var displayCmyk = new Profile (DescribedProfile.Display ("CMYK", "Display CMYK"));
			var unnamed = new Profile (MinimalIccProfile.WithColorSpace ("CMYK"));

			Assert.AreEqual ("Gray display", gray.ProductDescription);
			Assert.IsFalse (catalog.Add (gray));
			Assert.IsFalse (catalog.Add (displayCmyk));
			Assert.IsFalse (catalog.Add (unnamed));
			Assert.IsEmpty (catalog.Profiles);
			Assert.IsFalse (catalog.Profiles.ContainsKey (string.Empty));
		}

		// A directory scan has nowhere to put a profile the catalogue declines, so the
		// catalogue takes ownership and closes it rather than leaving it to the finalizer.
		[TestMethod]
		public void Add_DisposesTheProfileItRejects ()
		{
			var catalog = new ProfileCatalog ();
			var profile = new Profile (MinimalIccProfile.WithColorSpace ("GRAY"));

			catalog.Add (profile);

			Assert.ThrowsExactly<System.ObjectDisposedException> (() => _ = profile.ProductDescription);
		}

		[TestMethod]
		public void Add_KeepsTheProfileAlreadyRegisteredUnderThatDescription ()
		{
			var catalog = new ProfileCatalog ();
			var first = Profile.CreateAlternateRgb ();
			var duplicate = Profile.CreateAlternateRgb ();
			catalog.Add (first);

			Assert.IsFalse (catalog.Add (duplicate));
			Assert.AreSame (first, catalog.Profiles[first.ProductDescription]);
		}

		// Disposing the rejected duplicate must not reach the one already registered:
		// they are separate lcms handles, and the registered one is still published.
		[TestMethod]
		public void Add_LeavesTheRegisteredProfileUsableAfterRejectingADuplicate ()
		{
			var catalog = new ProfileCatalog ();
			var first = Profile.CreateAlternateRgb ();
			catalog.Add (first);

			catalog.Add (Profile.CreateAlternateRgb ());

			Assert.AreEqual ("RGB built-in", first.ProductDescription);
		}

		[TestMethod]
		public void AddFrom_RegistersTheProfilesItFindsBelowTheDirectory ()
		{
			string root = TemporaryDirectory ();
			string nested = Directory.CreateDirectory (Path.Combine (root, "nested")).FullName;
			File.WriteAllBytes (Path.Combine (nested, "srgb.icc"), CmsFacadeTests.SaveStandardRgb ());
			var catalog = new ProfileCatalog ();

			catalog.AddFrom (root);

			Assert.Contains ("sRGB built-in", catalog.Profiles.Keys);
		}

		// The scan walks user-writable trees -- ~/Library/ColorSync/Profiles and
		// ~/.color/icc among them -- so one directory the user cannot read must not
		// abandon the rest. Nothing is cached until the walk completes, so an escape here
		// would repeat on every later access to the profile list.
		[TestMethod]
		[OSCondition (ConditionMode.Exclude, OperatingSystems.Windows)]
		public void AddFrom_SkipsADirectoryItCannotRead ()
		{
			string root = TemporaryDirectory ();
			File.WriteAllBytes (Path.Combine (root, "srgb.icc"), CmsFacadeTests.SaveStandardRgb ());
			string locked = Directory.CreateDirectory (Path.Combine (root, "locked")).FullName;
			Directory.CreateDirectory (Path.Combine (locked, "deeper"));
			MakeUnreadable (locked);

			try {
				Directory.GetFiles (locked);
				Assert.Inconclusive ("this user can read a mode-000 directory, so there is nothing to skip; do not run as root");
			} catch (UnauthorizedAccessException) {
			}

			var catalog = new ProfileCatalog ();

			catalog.AddFrom (root);

			Assert.Contains ("sRGB built-in", catalog.Profiles.Keys);
		}

		string TemporaryDirectory ()
		{
			return temporaryDirectories.Create ("catalog");
		}

		// Guarded rather than suppressing CA1416: Roslyn recognises
		// OperatingSystem.IsWindows () as a platform gate but cannot see the
		// [OSCondition] that already keeps the caller off Windows.
		static void MakeUnreadable (string directory)
		{
			if (OperatingSystem.IsWindows ())
				return;

			File.SetUnixFileMode (directory, UnixFileMode.None);
		}

		// XProfile is published by ColorManagement.XProfile and cached there, so the
		// catalogue borrows it under a fixed key instead of owning it.
		[TestMethod]
		public void AddShared_RegistersUnderTheGivenKeyWithoutTakingOwnership ()
		{
			var catalog = new ProfileCatalog ();
			var profile = new Profile (MinimalIccProfile.WithColorSpace ("RGB "));

			Assert.IsTrue (catalog.AddShared ("_x_profile_", profile));
			Assert.AreSame (profile, catalog.Profiles["_x_profile_"]);
			Assert.AreEqual (string.Empty, profile.ProductDescription, "the borrowed profile should not be disposed");
		}
	}
}
