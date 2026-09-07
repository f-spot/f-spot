//
// ProfileCatalogTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Cms.UnitTest
{
	[TestClass]
	public class ProfileCatalogTests
	{
		[TestInitialize]
		public void RequireLcms ()
		{
			LcmsNative.Require ();
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
